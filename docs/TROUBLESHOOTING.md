# Troubleshooting Guide

Comprehensive troubleshooting for common issues in the Beincourt room framework across development, CI/CD, and deployment.

## GitHub Actions Build Issues

### Build Job Fails: "dotnet build failed"

**Symptoms**:
```
error: MSB3644: The reference assemblies for framework ".NETFramework,Version=4.7" were not found
```

**Causes**:
- Windows runner doesn't have required .NET Framework installed
- .csproj targeting wrong framework version
- Missing NuGet packages

**Solutions**:

1. **Add .NET Framework setup step** to workflow:
   ```yaml
   - name: Setup .NET Framework
     uses: actions/setup-dotnet@v4
     with:
       dotnet-version: '6.0.x'
   ```

2. **Restore NuGet packages first**:
   ```yaml
   - name: Restore packages
     run: dotnet restore src/epi-zoom-room.4Series.csproj
   ```

3. **Check .csproj target framework**:
   ```bash
   grep -i "TargetFramework" src/epi-zoom-room.4Series.csproj
   # Should show: net47, net48, or net6.0
   ```

**Fix in workflow**:
```yaml
- name: Build plugin
  run: |
    dotnet restore src/epi-zoom-room.4Series.csproj
    dotnet build src/epi-zoom-room.4Series.csproj -c Release
```

---

### Build Job: ".cplz file not found"

**Symptoms**:
```
No .cplz files found in src/
Upload artifacts: 0 files matched
```

**Cause**: 
- .cplz is generated in `bin/Release/` folder, not `src/`
- Build process didn't complete successfully
- .cplz has different name than expected

**Debug Steps**:

1. **List all files in build output**:
   ```yaml
   - name: List build output
     run: |
       echo "=== Looking for .cplz files ==="
       Get-ChildItem -Recurse -Path "${{ github.workspace }}" -Filter "*.cplz" | Select-Object FullName
       
       echo "=== Contents of src/ ==="
       Get-ChildItem -Recurse -Path "${{ github.workspace }}/src" | Select-Object Name, Extension
       
       echo "=== Contents of bin/Release/ ==="
       Get-ChildItem -Recurse -Path "${{ github.workspace }}/src/bin/Release" -ErrorAction SilentlyContinue
   ```

2. **Check Release configuration exists**:
   ```bash
   dotnet build src/epi-zoom-room.4Series.csproj -c Release -v diag
   # Look for "Release" configuration in output
   ```

3. **Verify build succeeded**:
   ```bash
   dotnet build src/epi-zoom-room.4Series.csproj -c Release
   # Exit code should be 0 (success)
   ```

**Fix**:
- Adjust glob pattern in workflow to find actual location
- Look in: `src/bin/Release/`, `src/bin/Release/net47/`, or `bin/Release/`

---

### Build Job: "Tests fail before release"

**Symptoms**:
```
dotnet test exited with code 1
Release cancelled due to test failure
```

**Solutions**:

1. **Run tests locally first**:
   ```bash
   cd C:\Users\ChrisVance\Documents\GitHub\Beincourt\CourtControl\Pv2\epi-zoom-room
   dotnet test tests/ -c Release --verbosity normal
   ```

2. **Skip tests in CI if expected** (not recommended, but option):
   ```yaml
   - name: Run tests
     continue-on-error: true  # Don't fail workflow if tests fail
     run: dotnet test tests/ -c Release --no-build
   ```

3. **Fix specific test failures**:
   - Review test output in GitHub Actions logs
   - Fix failing tests locally
   - Commit fix: `git commit -m "fix(test): resolve failing unit test"`
   - Push and trigger workflow again

---

### Semantic Release Job: "Failed to download artifacts"

**Symptoms**:
```
Error: Artifact not found for name: cplz-plugin
```

**Cause**:
- Build job failed (no artifacts created)
- Artifact upload failed silently
- Artifact expired (retention-days: 1 means it expires after 1 day)

**Solutions**:

1. **Check build job succeeded**:
   - Go to GitHub Actions run
   - Click "build" job
   - Scroll to "Upload build artifacts" step
   - Verify it says "✓ Upload complete" with file count > 0

2. **Increase artifact retention**:
   ```yaml
   - name: Upload build artifacts
     uses: actions/upload-artifact@v4
     with:
       name: cplz-plugin
       path: ${{ github.workspace }}/build-output/*.cplz
       retention-days: 7  # Keep for 7 days
   ```

3. **Debug artifact path**:
   ```yaml
   - name: Debug artifact location
     run: |
       Write-Host "Looking for files in:"
       Write-Host "${{ github.workspace }}/build-output/"
       Get-ChildItem -Path "${{ github.workspace }}/build-output/" -Recurse
   ```

---

### Release Job: "Semantic release fails with 404 PR error"

**Symptoms**:
```
RequestError [HttpError]: Not Found - list-commits-on-a-pull-request
Process completed with exit code 1
```

**This is EXPECTED on forks!**

**Explanation**:
- Semantic-release tries to comment on associated PR
- Forks pushing to their own repo have no PR to upstream
- Release is still published successfully; only notification fails

**Status**: This is a known issue and does NOT prevent:
- Version creation ✅
- .cplz asset upload ✅
- GitHub Release publication ✅
- PD Tools availability ✅

**No action needed** — the release is complete despite the error message.

---

## C# Build Issues (Local Development)

### "The type or namespace name 'X' could not be found"

**Cause**: Missing NuGet package or assembly reference

**Solution**:
```bash
cd C:\Users\ChrisVance\Documents\GitHub\Beincourt\CourtControl\Pv2\epi-zoom-room
dotnet restore src/epi-zoom-room.4Series.csproj
dotnet build src/epi-zoom-room.4Series.csproj
```

---

### "Framework not recognized"

**Symptoms**:
```
error : Unable to resolve target framework 'net6.0'
```

**Solution**:
1. Install correct .NET version:
   ```bash
   dotnet --list-sdks  # Check installed versions
   # Install missing version if needed
   ```

2. Check project file:
   ```bash
   grep -i "TargetFramework" src/epi-zoom-room.4Series.csproj
   ```

3. Ensure `.csproj` matches installed framework

---

### "Cannot build on Windows: 'dotnet' not recognized"

**Solution**:
1. Install .NET SDK from https://dotnet.microsoft.com/download
2. Restart PowerShell/terminal
3. Verify: `dotnet --version`

---

## Semantic Release Issues

### "No commits found since last release"

**Symptoms**:
```
No commits found since last release, skipping release
```

**Cause**: All recent commits have types that don't trigger releases (`docs:`, `ci:`, `test:`, etc.)

**Solution**: Make a commit with release-triggering type:
```bash
git commit --allow-empty -m "build: trigger release"
# OR
git commit -m "fix: minor bug fix"
```

See [SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md) for commit type reference.

---

### "Version number too high or too low"

**Symptoms**:
```
Next release version is 3.0.0 (expected 2.0.2)
```

**Causes**:
- Commit has breaking change marker (`!`)
- Previous commit had `feat:` (minor bump)
- Custom releaseRules in `.releaserc.json` not as expected

**Debug**:
```bash
npx semantic-release --dry-run
# Shows what version would be created without publishing
```

**Fix**:
- Check commit message format: `git log --oneline -5`
- Verify `.releaserc.json` releaseRules are correct
- If major version was accidental, manually revert: `git tag -d vX.X.X && git push origin :refs/tags/vX.X.X`

---

### "Asset not found in dist/"

**Symptoms**:
```
[semantic-release/github] › ✘ Glob pattern "dist/*.cplz" matched 0 files
```

**Cause**:
- Build job created .cplz but didn't upload to artifacts
- Release job downloaded artifacts but they're in wrong location

**Debug**:
1. Check build job upload step — should say "✓ 1 files uploaded"
2. Check release job download step location:
   ```yaml
   - name: List downloaded artifacts
     run: ls -la dist/
   ```

**Fix**:
- Adjust artifact upload glob pattern in build job
- Ensure download-artifact puts files in correct location
- Verify artifact name matches: `name: cplz-plugin`

---

## PD Tools Integration Issues

### "Plugin not showing in PD Tools version dropdown"

**Symptoms**:
- GitHub Release exists
- .cplz file uploaded
- But PD Tools dropdown doesn't show the version

**Causes**:
1. **PD Tools cache not refreshed** (most common)
   - Solution: Wait 2-3 minutes, then refresh UI (F5)

2. **Pre-release not shown** (if filtering)
   - v2.0.2-csv-zoom-sandbox-v2.1 is marked as pre-release
   - Solution: Toggle "Include Pre-release" in PD Tools UI

3. **Restore tool doesn't recognize .cplz**
   - Solution: Check .cplz filename format and location in release

**Verification**:
- Visit GitHub Release directly: https://github.com/pepperdash-beincourt/epi-bic-zoomroom/releases
- Confirm .cplz asset is there and downloadable
- Check asset name matches expected pattern

---

### "Restore tool: Release has no downloadable assets"

**This was the original issue!**

**Root Cause**: No .cplz file in GitHub Release (only .tgz)

**Solution**: 
- Updated `.github/workflows/semantic-release.yml` to build .cplz
- Updated `.releaserc.json` to include .cplz in release assets
- New releases should have .cplz available

**If still seeing this error**:
1. Check latest release has .cplz:
   ```bash
   curl -s https://api.github.com/repos/pepperdash-beincourt/epi-bic-zoomroom/releases/latest | jq '.assets[] | {name, size}'
   ```

2. If no .cplz, check build job logs:
   - GitHub Actions → Latest run → "build" job
   - Look for errors in "Build plugin" or "Upload build artifacts" steps

---

## Deployment & Hardware Issues

### "Plugin won't load on CP4N"

**Check plugin status**:
```bash
ssh admin@192.168.100.155
PluginStatus
# Should show: epi-bic-zoomroom - Running
```

**If Failed**:

1. **Check logs**:
   ```bash
   tail -f /opt/plugins/epi-bic-zoomroom/logs/plugin.log
   grep -i error /opt/plugins/epi-bic-zoomroom/logs/*.log
   ```

2. **Restart plugin**:
   ```bash
   PluginStop epi-bic-zoomroom
   sleep 5
   PluginStart epi-bic-zoomroom
   ```

3. **Check architecture mismatch**:
   - 32-bit vs 64-bit plugin version
   - Compare .cplz architecture with CP4N processor

---

### "Zoom Room SDK connection fails"

**Symptoms**: Camera commands timeout, meeting join fails

**Check connection**:
```bash
ssh admin@192.168.100.155
show status zCommand
# Should show: Connected - Yes
```

**If disconnected**:

1. **Verify Zoom Room device online**:
   - Zoom Room admin panel (on device or web)
   - Check IP connectivity

2. **Restart SDK connection**:
   ```bash
   # On CP4N
   PluginStop epi-bic-zoomroom
   show restart zCommand  # If this command exists
   PluginStart epi-bic-zoomroom
   ```

3. **Check SSH keys** (if required):
   ```bash
   ssh -v admin@192.168.100.155
   # Look for authentication method used
   ```

---

### "Presentation not visible to far-end participants"

**This was the original bug fixed in csv-zoom-sandbox-v2!**

**Check version**:
```bash
ssh admin@192.168.100.155
PluginStatus
# Verify version is 2.0.1 or higher
```

**If using older version**:
- Deploy v2.0.1+ via PD Tools
- The StartSharingOnlyMeeting fix requires v2.0.1+

**Still not working after update**:
1. Check logs for `shareView` command:
   ```bash
   grep "shareView" /opt/plugins/epi-bic-zoomroom/logs/plugin.log
   ```

2. Verify View All layout active
3. Test with different participant (might be video quality filtering)

---

### "Annotation stream not showing (NDI stream 3)"

**Check stream availability**:
- On NDI-capable device or monitor
- Look for "epi-bic-zoomroom-stream-3" in NDI source list

**If not available**:

1. **Verify annotation mode enabled**:
   ```bash
   ssh admin@192.168.100.155
   tail -f /opt/plugins/epi-bic-zoomroom/logs/plugin.log | grep -i annotation
   ```

2. **Check NDI network connectivity**:
   - Verify network has NDI discovery enabled
   - Check firewall doesn't block NDI ports (5353, 5960, 5961, 6960-6969)

3. **Restart plugin** to reinitialize NDI streams:
   ```bash
   PluginStop epi-bic-zoomroom
   sleep 5
   PluginStart epi-bic-zoomroom
   ```

---

## Debugging Tips

### Enable Debug Logging

**On CP4N**:
```bash
# Edit plugin config (if available)
sudo vi /opt/plugins/epi-bic-zoomroom/config.json
# Add: "debugLogging": true

# Restart
PluginStop epi-bic-zoomroom
PluginStart epi-bic-zoomroom

# Monitor logs
tail -f /opt/plugins/epi-bic-zoomroom/logs/plugin.log
```

### Collect Diagnostic Package

**For support team**:
```bash
ssh admin@192.168.100.155

# Create diagnostic archive
tar -czf /tmp/beincourt-diag.tar.gz \
  /opt/plugins/epi-bic-zoomroom/logs/ \
  /opt/plugins/epi-bic-zoomroom/config.json \
  /opt/essentials/log* 2>/dev/null

# Download to local machine
scp admin@192.168.100.155:/tmp/beincourt-diag.tar.gz ~/Downloads/
```

### GitHub Actions Log Analysis

When workflow fails:
1. **Go to run**: https://github.com/pepperdash-beincourt/epi-bic-zoomroom/actions
2. **Click failed run**
3. **Expand failed job** (usually "build" or "release")
4. **Search for error keyword**:
   - "error:"
   - "failed"
   - "exception"
5. **Look at full context** (lines before error often explain why)
6. **Copy full error output** when reporting issue

---

## Getting Help

### Information to Gather Before Reporting

1. **Workflow issue?**
   - Failed step name
   - Full error message
   - GitHub Actions run URL
   - `.releaserc.json` content

2. **Build issue?**
   - Full compile error message
   - Output of `dotnet --version`
   - Output of `dotnet --list-sdks`
   - Platform (Windows/Linux/Mac)

3. **Deployment issue?**
   - Plugin version installed
   - CP4N IP and OS version
   - Zoom Room model
   - Log excerpt showing error
   - Test scenario attempted

4. **Restore tool issue?**
   - Restore tool name/version
   - Error message from restore tool
   - Configuration files for restore tool

### Reporting Issues

1. **Check [GitHub Issues](https://github.com/pepperdash-beincourt/epi-bic-zoomroom/issues)** for similar problems

2. **Create new issue** with:
   - Descriptive title
   - Detailed symptoms
   - Steps to reproduce
   - Diagnostic information (see above)
   - Screenshots/logs if applicable

3. **Reach out to team** if needed via:
   - Slack #beincourt-dev channel
   - Team standup meetings
   - Direct message to development lead

---

**Last Updated**: August 10, 2026

**Related Documentation**:
- [DEVELOPMENT.md](./DEVELOPMENT.md) — Common development tasks
- [DEPLOYMENT.md](./DEPLOYMENT.md) — Hardware deployment procedures
- [SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md) — Release configuration details
