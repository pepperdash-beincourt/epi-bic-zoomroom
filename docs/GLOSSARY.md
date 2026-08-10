# Glossary & Terminology Reference

A reference guide for terminology, abbreviations, and concepts used in the Beincourt room framework and Pepperdash ecosystem.

## General Concepts

### Beincourt Room
The courtroom control system built on Pepperdash Essentials for Beincourt installations. Integrates Zoom Room technology with courtroom-specific features (presentation sharing, annotation, etc.).

### Pepperdash Essentials
An open-source C# plugin framework for building control system applications. Abstracts hardware and control logic into reusable, modular plugins. Used by many AV integrators and installed on processors from various manufacturers.

**Related**:
- Repository: https://github.com/PepperDash
- Essentials Framework: https://github.com/PepperDash/Essentials
- Documentation: https://github.com/PepperDash/epi-zoom-room/wiki

### Plugin (Essentials Plugin)
A packaged, reusable C# library that adds functionality to the Essentials framework. Each plugin handles a specific device or capability (e.g., Zoom Room control, audio processing, lighting, etc.).

**Examples**:
- `epi-zoom-room` — Zoom Room SDK integration
- `epi-biamp-dsp` — Biamp DSP control
- `epi-lighting` — Lighting control

## Version & Release

### Semantic Versioning (SemVer)
Version format: `MAJOR.MINOR.PATCH`

- **MAJOR** (first number) — Breaking changes (incompatible API changes)
- **MINOR** (second number) — New features (backward compatible)
- **PATCH** (third number) — Bug fixes (backward compatible)

**Example**: `2.0.1`
- 2 = Major version
- 0 = Minor version
- 1 = Patch version

### Pre-release Version
A version marked as not yet stable, usually for testing or development. Includes additional identifiers after the patch number.

**Format**: `MAJOR.MINOR.PATCH-PRERELEASE.ITERATION`

**Example**: `2.0.2-csv-zoom-sandbox-v2.1`
- `2.0.2` = Base version (would be stable version)
- `csv-zoom-sandbox-v2` = Pre-release identifier (branch name)
- `1` = Iteration number on this pre-release branch

**Related**: See [SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md)

### Git Tag
A named reference to a specific commit in git history. Used to mark releases and version points.

**Format**: `v` + version number

**Examples**:
- `v2.0.1` — Tag for stable release v2.0.1
- `v2.0.2-csv-zoom-sandbox-v2.1` — Tag for pre-release development version

### GitHub Release
A packaged version published on GitHub, including release notes and downloadable assets (e.g., .tgz package).

**Contents**:
- Release notes (auto-generated from commits)
- .tgz asset (npm package)
- Source code snapshot
- Tag information

**Access**: https://github.com/pepperdash-beincourt/epi-bic-zoomroom/releases

### Changelog
A file documenting changes between versions. Generated automatically by semantic-release from commit messages.

**File**: `CHANGELOG.md` in repository root

**Format**:
```markdown
## [2.0.2] (2026-08-10)

### Bug Fixes
- fix: resolve annotation stream initialization
- fix: prevent null reference in meeting state

### Features
- feat: add custom layout support
```

## Zoom Room & Integration

### Zoom Room
A turnkey video conferencing endpoint manufactured by Zoom. Includes built-in camera, microphone, speaker, and processing. Controlled via SSH command interface (Zoom Room SDK).

**Models**:
- Zoom Room for Cisco (CE70, CP4N, etc.)
- Zoom Room for Polycom
- Zoom Room for Crestron

### Zoom Room SDK
The SSH-based command interface for controlling Zoom Room devices. Allows remote control of join/leave, camera, layouts, recording, sharing, etc.

**Command Examples**:
- `zCommand join "Meeting 123"` — Join meeting
- `zCommand shareView start` — Start presentation sharing
- `zCommand camera pan left` — Pan camera left

**Related**: [ZrcSdkController.cs](../src/Controller/ZrcSdkController.cs)

### StartSharingOnlyMeeting
A Zoom Room SDK command that ensures presentation content is visible to **far-end participants** (not just locally). This is the key fix in `csv-zoom-sandbox-v2` branch.

**Old behavior** (broken):
```bash
zCommand shareRemote start  # Only local display sees content
```

**New behavior** (fixed):
```bash
zCommand shareView start    # Far-end participants see content too
```

**Related**: [Beincourt Fix](../BEINCOURT.md)

### NDI (Network Device Interface)
Streaming protocol for sending video and audio over standard network. Used for annotation and presentation routing.

**Stream 3** (in Beincourt context): Reserved for annotation feed

**Device**: StreamSync (receives NDI streams and outputs to displays)

### Annotation
Drawing/marking capability during Zoom meetings. Presenter can draw on shared content. Routed via NDI stream 3 to annotation decoder.

**Support**: Enabled when annotation mode is toggled on in control interface.

### View All Layout
Zoom Room layout showing presenter + all participants + shared content simultaneously. Used for high-motion courtroom presentations.

## Hardware

### CP4N
**Cisco Collaboration Endpoint (Cisco Processor for 4 Series)**

A control processor running embedded Linux with Essentials plugin support. Primary hardware for Beincourt courtroom.

**Specifications**:
- Network: Ethernet (IP address: 192.168.100.155)
- Access: SSH (admin login)
- Runtime: Cisco OS + Essentials runtime
- Plugins: Hosted and managed via PD Tools

**Related**: [DEPLOYMENT.md](./DEPLOYMENT.md)

### StreamSync
A hardware device that processes NDI streams. In Beincourt, it receives annotation feed on NDI stream 3 and outputs to displays.

**Role**: Bridges network-based NDI streams to physical displays

### Zoom Room Device
The actual Zoom Room endpoint (physical device). Separate from CP4N; communicates via SSH SDK interface.

**Note**: CP4N **controls** the Zoom Room device via SDK commands.

## Development & Git

### Fork (Repository Fork)
A complete copy of a repository under different ownership. Allows independent development while maintaining ability to sync with upstream.

**Beincourt Setup**:
- **Upstream** (read-only): https://github.com/PepperDash/epi-zoom-room (original, by PepperDash)
- **Fork** (write access): https://github.com/pepperdash-beincourt/epi-bic-zoomroom (Beincourt customizations)

**Workflow**: Changes pushed to fork → Versions created → Deployable via PD Tools

### Branch
A parallel line of development in git. Allows multiple versions to be worked on simultaneously.

**Branch Types in Beincourt**:

| Branch | Purpose | Write Access | Releases |
|--------|---------|--------------|----------|
| main | Stable, upstream-synced | Admin only | ✅ Stable versions |
| csv-zoom-sandbox | Old development | ❌ Archived | ❌ None |
| csv-zoom-sandbox-v2 | Active development | ✅ Developers | ✅ Pre-release versions |

**Workflow**: Develop on feature branch → Merge to `csv-zoom-sandbox-v2` → Automatic release

### Commit
A snapshot of code changes. Committed to a branch with a message explaining the changes.

**Commit Message Format** (required for releases):
```
type(scope): description

optional body

optional footer
```

**Example**:
```
fix(camera): resolve PTZ timeout issue

When camera was switching layouts too rapidly, PTZ commands
would timeout after 5 seconds. Increased timeout to 10 seconds
and added exponential backoff retry logic.

Fixes #42
```

### Pull Request (PR)
A request to merge changes from one branch to another. Allows code review before merging.

**Workflow**:
1. Create feature branch
2. Make changes, commit
3. Push to remote
4. Create PR in GitHub UI
5. Team reviews and approves
6. Merge to target branch (e.g., `csv-zoom-sandbox-v2`)

**Note**: PRs to upstream (`PepperDash/epi-zoom-room`) require special coordination; see [CONTRIBUTING.md](./CONTRIBUTING.md)

### Merge
Combining changes from one branch into another. Can be:
- **Standard merge**: Creates a merge commit
- **Squash merge**: Combines all commits into one
- **Rebase merge**: Replays commits on top of target branch

**In Beincourt**: Typically use squash merge for clean history on `csv-zoom-sandbox-v2`

### Remote
A reference to a repository hosted on a server (typically GitHub).

**Beincourt Remotes**:
- `fork` — https://github.com/pepperdash-beincourt/epi-bic-zoomroom (write access)
- `upstream` — https://github.com/PepperDash/epi-zoom-room (read-only, syncing)
- `origin` — Usually same as `fork` (default remote)

**Commands**:
```bash
git remote -v           # List all remotes
git fetch upstream      # Fetch latest from upstream
git push fork branch    # Push to fork remote
```

## CI/CD & Automation

### GitHub Actions
GitHub's built-in continuous integration system. Runs automated tasks (build, test, release) when code is pushed.

**In Beincourt**: `.github/workflows/semantic-release.yml` runs on every push to configured branches.

### Workflow
A YAML-defined sequence of automated tasks in GitHub Actions. Stored in `.github/workflows/` folder.

**Beincourt Workflow**:
- Triggers on: `push` to main, csv-zoom-sandbox, csv-zoom-sandbox-v2
- Steps:
  1. Checkout code
  2. Setup Node.js
  3. Install dependencies
  4. Run semantic-release

### Semantic Release
An npm tool that automates versioning, changelog generation, and GitHub Release publishing based on commit messages.

**Process**:
1. Analyze commits since last release
2. Determine version bump (patch/minor/major)
3. Generate release notes from commits
4. Update CHANGELOG.md
5. Create git tag
6. Publish GitHub Release
7. Update package.json
8. Output version info for downstream systems

**Config Files**:
- `.releaserc.json` — Main configuration
- `.github/workflows/semantic-release.yml` — Trigger definition

### Dry Run
A test run of a process (like semantic-release) without actually making changes. Used to verify behavior before going live.

**Command**:
```bash
npx semantic-release --dry-run  # Analyzes but doesn't publish
```

## PD Tools & Deployment

### PD Tools
Pepper Dash's provisioning and management portal. Web UI for deploying plugins to hardware, monitoring device health, managing configurations.

**Access**: https://pdtools.pepperdash.com (requires account)

**Features**:
- Device management
- Plugin version selection and deployment
- Configuration management
- Remote monitoring and troubleshooting
- Firmware updates

### Provisioning
The process of deploying software and configuration to hardware. In Beincourt context: downloading plugin version from GitHub Release and installing on CP4N.

**Flow**:
1. User selects version in PD Tools
2. PD Tools downloads .tgz from GitHub Release
3. Transfers to CP4N
4. Unpacks and loads plugin
5. Restarts control system if needed
6. Plugin ready for use

### Channel (Release Channel)
A named collection of releases. Allows different user groups to receive different versions.

**Beincourt Channels**:
- (implicit) — Stable releases (main branch)
- `csv-zoom-sandbox-v2` — Development releases (pre-release versions)

**Use**: Users can choose "Stable" or "Development" channel in PD Tools.

## Testing

### Unit Test
Automated test of a single function or method. Runs in isolation, mocking external dependencies.

**Example**:
```csharp
[Test]
public void SendCommand_WithValidCommand_SuccessfullyTransmits()
{
    var controller = new ZrcSdkController("192.168.1.100");
    var result = controller.SendCommand("zCommand test");
    Assert.That(result, Is.True);
}
```

**Command**: `dotnet test tests/`

### Integration Test
Test of multiple components working together. More complex, tests realistic workflows.

**Example**: Test joining Zoom meeting → Starting presentation → Switching layout

### Manual Testing (Hardware Testing)
Testing on actual hardware (CP4N). Validates real-world functionality, integration with Zoom Room SDK, etc.

**Scenarios**: See [DEPLOYMENT.md](./DEPLOYMENT.md) "Test Scenarios"

### Regression
A situation where a previously working feature breaks due to recent changes. Regression testing ensures fixes don't break other functionality.

**Prevention**: Good test coverage + review process

## Architecture & Design

### Controller
The main class managing device control logic. In `epi-bic-zoomroom`, this is `ZrcSdkController` which sends commands to Zoom Room SDK.

**Responsibilities**:
- Send SSH commands to Zoom Room device
- Parse responses
- Maintain connection
- Queue commands if needed

**File**: [src/Controller/ZrcSdkController.cs](../src/Controller/ZrcSdkController.cs)

### Join Map
A mapping of control system input/output points to Zoom Room functionality. Enables "simple" commands like "JoinMeeting" to be mapped to complex SDK sequences.

**File**: [src/ZoomRoomJoinMap.cs](../src/ZoomRoomJoinMap.cs)

**Example**:
```csharp
JoinMap.AddBoolInput("AnnotationEnable", ...);  // UI toggle
JoinMap.Subscribe(() => {
    if (AnnotationEnabled)
        SendCommand("zCommand annotation start");
});
```

### Status/State
The current condition of the Zoom Room device. Maintained by parsing incoming status messages from SDK.

**Examples**:
- Meeting state (idle, joining, in-call, etc.)
- Participant count
- Current layout
- Camera position

**File**: [src/zStatus.cs](../src/zStatus.cs)

### Messenger
A helper class that formats status updates for display in Mobile Control app. Translates internal state to UI-friendly format.

**Files**: [src/MobileControlMessenger/](../src/MobileControlMessenger/)

**Example**:
```csharp
IHasZoomRoomLayoutsMessenger
  ↓ Formats layout data
  ↓ Mobile app displays layout picker
```

## Troubleshooting Terms

### SSH Connection
Secure Shell protocol for remote command execution. `epi-bic-zoomroom` uses SSH to communicate with Zoom Room device.

**Check Connection**:
```bash
ssh admin@192.168.100.155  # If this works, plugin can too
```

### Timeout
When an operation doesn't complete within expected time (usually seconds). Common in network operations.

**Example**: Camera command times out if Zoom Room doesn't respond in 5 seconds.

### Null Reference
Programming error when code tries to use a variable that has no value (is null). Causes plugin crashes.

**Prevention**: Add null checks, use optional types in C#

### Log File
A record of events that occurred during plugin operation. Useful for debugging.

**Location on CP4N**: `/opt/plugins/epi-bic-zoomroom/logs/`

**Viewing**:
```bash
ssh admin@192.168.100.155
tail -f /opt/plugins/epi-bic-zoomroom/logs/plugin.log
```

---

**Last Updated**: August 10, 2026

**Need more terms defined?** Open an issue or PR with your suggestions!
