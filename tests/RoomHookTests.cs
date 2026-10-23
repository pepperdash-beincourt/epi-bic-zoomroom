using FluentAssertions;

namespace PepperDash.Essentials.Plugins.Zoom.Room.Tests;

/// <summary>
/// A room controlling this codec finds these public methods by name and parameter types at runtime
/// (it has no compile-time reference to this plugin), so renaming one or changing its parameters
/// silently turns the feature off. Inspects the compiled assembly.
/// </summary>
public class RoomHookTests
{
    private static System.Reflection.TypeInfo ZoomRoomType =>
        AssemblyFixture.PluginAssembly.DefinedTypes.Single(t => t.Name == "ZoomRoom");

    [Theory]
    [InlineData("SetActiveSpeakerCamera", "Int32")]
    [InlineData("SetCameraTileNames", "String[]")]
    [InlineData("SetCameraInputInMultiCamera", "String,Boolean")]
    [InlineData("SetLayoutWhileSharing", "String")]
    public void ZoomRoom_Exposes_Room_Hook(string name, string parameterTypes)
    {
        var expected = parameterTypes.Split(',');
        ZoomRoomType.GetMethods()
            .Should().Contain(m => m.Name == name && m.IsPublic && !m.IsStatic &&
                m.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(expected),
                "the room calls {0}({1}) by reflection", name, parameterTypes);
    }
}
