# Development Guide

## Environment Setup

### Prerequisites
- **Git** (version control)
- **.NET SDK 7.0+** or **.NET Framework 4.7+** (for building C# projects)
- **Visual Studio 2022** (Community or Pro) or **VS Code** with C# extensions
- **Node.js 18+** (for semantic-release tooling)
- **npm** (Node package manager, comes with Node.js)
- **GitHub CLI** (optional, for easier git operations)

### Initial Setup

1. **Clone the fork** (not upstream):
   ```bash
   git clone https://github.com/pepperdash-beincourt/epi-bic-zoomroom.git
   cd epi-bic-zoomroom
   ```

2. **Add upstream as a remote** (for syncing):
   ```bash
   git remote add upstream https://github.com/PepperDash/epi-zoom-room.git
   git fetch upstream
   ```

3. **Install Node.js dependencies** (for semantic-release):
   ```bash
   npm install
   ```

4. **Build the project**:
   ```bash
   # Using .NET CLI
   dotnet build src/epi-zoom-room.4Series.csproj
   
   # Or using Visual Studio
   # Open epi-zoom-room.4Series.sln and hit Ctrl+Shift+B
   ```

5. **Run tests**:
   ```bash
   dotnet test tests/
   ```

## Branch Strategy & Workflow

### Branch Naming Conventions

```
main                          (Upstream-synced, stable, read-only)
│
├─ csv-zoom-sandbox           (Old development branch, archived)
│
└─ csv-zoom-sandbox-v2        (Active development, write-enabled)
   ├─ feature/presentation-routing
   ├─ fix/annotation-stream-3
   └─ ... (temporary feature branches)
```

### Creating a Feature Branch

**Always branch from `csv-zoom-sandbox-v2`** (never `main`):

```bash
# Ensure you're on csv-zoom-sandbox-v2
git checkout csv-zoom-sandbox-v2
git pull fork csv-zoom-sandbox-v2

# Create feature branch
git checkout -b feature/your-feature-name

# Make changes, commit, push
git add .
git commit -m "feat: add new feature"
git push fork feature/your-feature-name
```

### Merging Changes Back

**Option 1: Local merge** (recommended for small changes)
```bash
git checkout csv-zoom-sandbox-v2
git pull fork csv-zoom-sandbox-v2
git merge --squash feature/your-feature-name
git commit -m "feat: add new feature"  # Semantic commit message!
git push fork csv-zoom-sandbox-v2
```

**Option 2: Pull Request** (recommended for reviews)
```bash
# Push feature branch and open PR via GitHub UI
# Title: "feat: add new feature"  (semantic message)
# Let team review, then merge to csv-zoom-sandbox-v2
```

### Branch Protection Rules

- **`main`**: Protected, upstream-only syncing
  - No direct commits allowed
  - PRs must pass CI/CD
  - Requires admin approval for upstream sync

- **`csv-zoom-sandbox-v2`**: Open for development
  - Direct commits allowed
  - Each commit automatically versioned
  - Force push allowed (for rebasing)

## Commit Message Guidelines

Commit messages **must** follow [Conventional Commits](https://www.conventionalcommits.org/) format to trigger semantic versioning.

### Format
```
type(scope): description

optional body

optional footer
```

### Types & Release Impact

| Type | Release Type | Example |
|------|-------------|---------|
| `build:` | Patch | `build: trigger development release` |
| `fix:` | Patch | `fix: correct StartSharingOnlyMeeting API call` |
| `feat:` | Minor | `feat: add annotation stream support` |
| `BREAKING CHANGE` | Major | `feat!: refactor controller interface` |
| `ci:` | None | `ci: update semantic-release config` |
| `docs:` | None | `docs: update README` |
| `test:` | None | `test: add unit tests for camera control` |
| `chore:` | None | `chore: bump dependencies` |

### Real Examples

```bash
# Patches (2.0.1 → 2.0.2)
git commit -m "fix: resolve annotation stream initialization"
git commit -m "fix: prevent null reference in meeting state"

# Minor version (2.0.x → 2.1.0)
git commit -m "feat: add support for custom layouts"

# Major version (2.x.x → 3.0.0)
git commit -m "feat!: redesign controller API"  # Note the !

# No release
git commit -m "test: add unit tests for camera"
git commit -m "docs: clarify deployment process"
```

## Code Structure

### Directory Overview

```
src/
├── Controller/
│   ├── IZoomRoomController.cs      # Interface for control operations
│   ├── ZrcSdkController.cs         # Main controller implementation
│   └── SdkConnectionMonitor.cs     # Connection state monitoring
├── ZoomRoom.cs                     # Core plugin class
├── ZoomRoomCamera.cs               # Camera-specific operations
├── zCommand.cs                     # Command definitions
├── zStatus.cs                      # Status/state definitions
├── ZoomRoomJoinMap.cs              # Mobile control messenger mappings
└── MobileControlMessenger/         # Mobile control UI bindings
    ├── IHasZoomRoomLayoutsMessenger.cs
    ├── IHasCameraAutoModeMessenger.cs
    ├── IHasMeetingRecordingWithPromptMessenger.cs
    └── ... (other messengers)
```

### Key Classes

**ZoomRoom.cs** (Main Plugin)
- Initializes the plugin
- Manages device connections
- Exposes join map for control system
- Handles Zoom SDK events

**ZrcSdkController.cs** (Core Controller)
- Communicates with Zoom Room SDK via SSH
- Sends commands (join, share, camera control, etc.)
- Receives and parses status updates
- Most likely location for API fixes

**Mobile Control Messengers**
- Translate control system state to mobile app UI
- Handle user interactions from mobile app
- Update room state based on user input

### Modifying Presentation Sharing

The StartSharingOnlyMeeting fix is in **ZrcSdkController.cs**:

```csharp
// OLD (not working for far-end participants)
SendCommand("zCommand shareRemote start");

// NEW (fixed)
SendCommand("zCommand shareView start");
```

For annotation support, check:
- **StreamSync device configuration** — NDI stream 3 binding
- **zCommand.cs** — Command definitions for annotation mode
- **ZoomRoomJoinMap.cs** — Mobile UI mapping for annotation toggle

## Testing

### Unit Tests

Run all tests:
```bash
dotnet test tests/
```

Run specific test class:
```bash
dotnet test tests/ --filter "ControllerAbstractionTests"
```

Test files are in `tests/` folder. Key test files:
- `ControllerAbstractionTests.cs` — ZrcSdkController behavior
- `ConfigDeserializationTests.cs` — Configuration parsing
- `FactoryDiscoveryTests.cs` — Plugin factory patterns

### Manual Integration Testing

**On CP4N hardware** (192.168.100.155):

1. Deploy version via PD Tools
2. SSH into CP4N:
   ```bash
   ssh admin@192.168.100.155
   ```
3. Check plugin status:
   ```bash
   # In CP4N shell
   PluginStatus
   ```
4. Test scenarios:
   - Join Zoom meeting
   - Start presentation (View All mode)
   - Enable annotation
   - Control camera
   - Switch layouts
   - Record meeting

### Debugging

**Local debugging** (not on hardware):
- Open `epi-zoom-room.4Series.sln` in Visual Studio
- Set breakpoints in ZrcSdkController or ZoomRoom
- Run tests with debugger attached

**Hardware debugging**:
- Enable debug logging in ZoomRoom (if implemented)
- SSH into CP4N and tail plugin logs
- Review Zoom Room SDK responses

## Release & Versioning

### Automatic Releases

Every commit to `csv-zoom-sandbox-v2` automatically triggers a release:

1. **Commit pushed** → GitHub webhook fires
2. **semantic-release.yml** runs on GitHub Actions
3. **Semantic-release analyzes** commit message
4. **Version calculated** (patch, minor, major)
5. **Tag created** (e.g., `v2.0.2-csv-zoom-sandbox-v2.1`)
6. **GitHub Release published** with .tgz asset
7. **PD Tools updated** within 1-2 minutes

### Manual Release (Advanced)

For testing or forcing a release locally:

```bash
npm install -D semantic-release @semantic-release/github @semantic-release/git
npx semantic-release --dry-run  # Test without committing
npx semantic-release            # Actual release
```

See [SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md) for detailed configuration.

## Common Development Tasks

### Task: Fix a Bug

```bash
# 1. Create branch from csv-zoom-sandbox-v2
git checkout csv-zoom-sandbox-v2
git checkout -b fix/bug-description

# 2. Make changes
# Edit files, run tests locally
dotnet test tests/

# 3. Commit with semantic message
git commit -m "fix: describe the bug and solution"

# 4. Push and merge
git push fork fix/bug-description
# Merge via PR or local merge to csv-zoom-sandbox-v2
git checkout csv-zoom-sandbox-v2
git merge fix/bug-description

# 5. Semantic-release automatically creates v2.0.X
# Version appears in PD Tools within 1-2 minutes
```

### Task: Add a Feature

```bash
# 1. Create feature branch
git checkout csv-zoom-sandbox-v2
git checkout -b feature/new-feature

# 2. Implement feature
# Add code, update tests, etc.
dotnet build src/
dotnet test tests/

# 3. Commit with semantic message (feat: or build:)
git commit -m "feat: add new feature for X"

# 4. Merge to csv-zoom-sandbox-v2
git push fork feature/new-feature
# Merge via PR to csv-zoom-sandbox-v2

# 5. Semantic-release creates v2.1.0 (minor version bump)
# Version appears in PD Tools for deployment
```

### Task: Sync Upstream Changes

```bash
# 1. Fetch latest from PepperDash
git fetch upstream

# 2. Check for new commits on main
git log main...upstream/main

# 3. Merge or rebase main (if allowed)
git checkout main
git merge upstream/main  # or git rebase upstream/main

# 4. Merge main into csv-zoom-sandbox-v2
git checkout csv-zoom-sandbox-v2
git merge main

# 5. Resolve conflicts if any
# Make sure all tests pass
dotnet test tests/

# 6. Commit and push
git commit -m "chore: sync upstream updates"
git push fork csv-zoom-sandbox-v2
```

### Task: Deploy to Hardware

```bash
# 1. Ensure version is available in PD Tools
# Wait 1-2 minutes after push for semantic-release to complete

# 2. Open PD Tools UI
# Select Beincourt room
# Choose version (e.g., 2.0.2-csv-zoom-sandbox-v2.1)
# Click "Sync" or "Deploy"

# 3. Monitor CP4N provisioning
# Log into CP4N via SSH if needed
# Verify plugin loaded: PluginStatus

# 4. Test functionality
# Join Zoom meeting, share screen, etc.
```

## Performance & Optimization

### Profiling Commands

Check plugin memory and CPU usage on CP4N:
```bash
ssh admin@192.168.100.155
# In CP4N shell
ProcStats  # Show process statistics
```

### Common Performance Issues

| Issue | Symptom | Solution |
|-------|---------|----------|
| Slow camera response | Camera pan/tilt is sluggish | Check SSH connection speed to Zoom Room |
| Meeting state delays | Status not updating in real-time | Verify polling interval in zStatus.cs |
| Memory leak | CP4N gets slower over time | Review event subscription cleanup in dispose methods |
| Layout switching slow | Gallery/speaker switch takes 2+ seconds | Check layout command queueing in ZrcSdkController |

## Troubleshooting Development Issues

### Build Fails

**Error**: `The type or namespace name 'Newtonsoft' could not be found`

**Solution**: Restore NuGet packages
```bash
dotnet restore src/epi-zoom-room.4Series.csproj
```

**Error**: `.csproj file not found`

**Solution**: Ensure you're in the repository root and path is correct
```bash
ls src/epi-zoom-room.4Series.csproj  # Should show the file
```

### Tests Fail

**Error**: `Unable to load SDK x.x.x`

**Solution**: Install required test framework
```bash
dotnet test tests/ --restore
```

### Semantic-Release Error

**Error**: `Not Found - list-commits-on-a-pull-request`

**Solution**: This is a known issue with PR-less releases on forks. The release still completes successfully; only the success notification fails. No action needed.

---

**Last Updated**: August 10, 2026
