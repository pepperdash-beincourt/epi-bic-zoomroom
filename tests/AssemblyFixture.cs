using System.Reflection;
using System.Text.Json;

namespace PepperDash.Essentials.Plugins.Zoom.Room.Tests;

public static class AssemblyFixture
{
    private static readonly Lazy<MetadataLoadContext> LazyContext = new(CreateContext);
    private static readonly Lazy<Assembly> LazyAssembly = new(LoadPluginAssembly);

    /// <summary>
    /// Derives Debug/Release from the test output path: tests/bin/{Configuration}/net8.0/
    /// so that `dotnet test -c Release` works without hard-coding.
    /// </summary>
    private static string Configuration
    {
        get
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var parts = baseDir.Split(Path.DirectorySeparatorChar);
            // net8.0 is the last segment, Configuration is second-to-last
            return parts[^2];
        }
    }

    /// <summary>
    /// Plugin csproj is in src/ (not src/4Series/), and the Crestron TFM folder is "net8" (not "net8.0").
    /// </summary>
    private static string PluginDllPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "src", "bin", Configuration, "net8",
            "PepperDash.Essentials.Plugins.bic.zoomroom.dll"));

    private static string PluginOutputDir => Path.GetDirectoryName(PluginDllPath)!;

    public static MetadataLoadContext Context => LazyContext.Value;
    public static Assembly PluginAssembly => LazyAssembly.Value;

    public static string SourceDirectory =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "src"));

    private static MetadataLoadContext CreateContext()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var dllByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Priority 1: plugin output dir wins (correct versions for Crestron/PepperDash assemblies)
        foreach (var dll in Directory.GetFiles(PluginOutputDir, "*.dll"))
            dllByName[Path.GetFileName(dll)] = dll;

        // Priority 2: .NET runtime
        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            dllByName.TryAdd(Path.GetFileName(dll), dll);

        // Priority 3: the plugin's NuGet packages, transitive ones included. project.assets.json lists
        // every restored package; deps.json can leave out packages referenced with
        // ExcludeAssets="runtime" (PepperDashEssentials and its dependencies), depending on the SDK.
        var depsJsonPath = Path.ChangeExtension(PluginDllPath, ".deps.json");
        foreach (var listPath in new[] { AssetsJsonPath, depsJsonPath }.Where(File.Exists))
        {
            foreach (var path in ResolvePackageAssemblies(listPath))
                dllByName.TryAdd(Path.GetFileName(path), path);
        }

        return new MetadataLoadContext(new PathAssemblyResolver(dllByName.Values));
    }

    /// <summary>
    /// Where the plugin's packages were restored: the package folders recorded in its
    /// project.assets.json, then NUGET_PACKAGES, then the default ~/.nuget/packages. CI runners
    /// often keep the package cache somewhere other than the user profile.
    /// </summary>
    private static IEnumerable<string> PackageFolders()
    {
        if (File.Exists(AssetsJsonPath))
        {
            using var stream = File.OpenRead(AssetsJsonPath);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("packageFolders", out var folders))
            {
                foreach (var folder in folders.EnumerateObject())
                    yield return folder.Name;
            }
        }

        var fromEnvironment = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(fromEnvironment))
            yield return fromEnvironment;

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");
    }

    /// <summary>The plugin's restore output, which lists every package it depends on.</summary>
    private static string AssetsJsonPath => Path.Combine(SourceDirectory, "obj", "project.assets.json");

    /// <summary>
    /// Assemblies of every package in a "libraries" list. project.assets.json and deps.json share the
    /// shape: each entry has a "type" and, for packages, a "path" relative to a package folder.
    /// </summary>
    private static IEnumerable<string> ResolvePackageAssemblies(string libraryListPath)
    {
        var packageFolders = PackageFolders().Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        using var stream = File.OpenRead(libraryListPath);
        using var doc = JsonDocument.Parse(stream);

        if (!doc.RootElement.TryGetProperty("libraries", out var libraries))
            yield break;

        foreach (var lib in libraries.EnumerateObject())
        {
            if (!lib.Value.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "package")
                continue;
            if (!lib.Value.TryGetProperty("path", out var pathProp))
                continue;

            var packagePath = packageFolders
                .Select(folder => Path.Combine(folder, pathProp.GetString()!))
                .FirstOrDefault(Directory.Exists);
            if (packagePath == null) continue;

            // NuGet packages ship net8.0 libs even when plugin TFM is "net8"
            var libDir = Path.Combine(packagePath, "lib", "net8.0");
            if (!Directory.Exists(libDir))
                libDir = Path.Combine(packagePath, "lib", "netstandard2.0");
            if (!Directory.Exists(libDir)) continue;

            foreach (var dll in Directory.GetFiles(libDir, "*.dll"))
                yield return dll;
        }
    }

    private static Assembly LoadPluginAssembly()
    {
        if (!File.Exists(PluginDllPath))
            throw new FileNotFoundException(
                $"Plugin DLL not found at '{PluginDllPath}'. Build the plugin first (dotnet build epi-zoom-room.4Series.sln).");
        return Context.LoadFromAssemblyPath(PluginDllPath);
    }

    public static List<Type> FindFactoryTypes()
    {
        return PluginAssembly.GetTypes()
            .Where(t => !t.IsAbstract
                && t.BaseType is { IsGenericType: true }
                && t.BaseType.GetGenericTypeDefinition().Name.StartsWith("EssentialsPluginDeviceFactory"))
            .ToList();
    }
}
