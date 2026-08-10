# Architecture & Technical Strategy

## System Overview

The Beincourt room is built on the **Pepperdash Essentials plugin framework** — a C# library that abstracts control system operations into reusable components. The Beincourt implementation extends the public `epi-zoom-room` library with custom features, bug fixes, and integrations specific to the Beincourt courtroom environment.

```
┌─────────────────────────────────────────────────────────┐
│           Beincourt Room Control System                  │
├─────────────────────────────────────────────────────────┤
│  PD Tools (Remote Management & Provisioning)             │
│  └─ Deploys packages & configuration to hardware         │
├─────────────────────────────────────────────────────────┤
│  epi-bic-zoomroom (Beincourt Customization Layer)        │
│  └─ Fork of PepperDash/epi-zoom-room                     │
│  ├─ StartSharingOnlyMeeting API fixes                    │
│  ├─ Presentation content routing                         │
│  └─ NDI annotation stream support                        │
├─────────────────────────────────────────────────────────┤
│  epi-zoom-room (Upstream Library - PepperDash)           │
│  └─ Core Zoom Room SDK integration                       │
│  ├─ Camera control, meeting state, participants          │
│  ├─ Mobile Control integration                           │
│  └─ Layout management & presets                          │
├─────────────────────────────────────────────────────────┤
│  Zoom Room SDK (Hardware Abstraction)                     │
│  └─ SSH communication with Zoom Room devices             │
└─────────────────────────────────────────────────────────┘
```

## Fork Strategy

### Why Fork?

The Beincourt room requires customizations that are:
1. **Specific to Beincourt** — Not applicable to other organizations
2. **Blocking production** — E.g., presentation content not reaching far-end participants
3. **Pending upstream contribution** — May be merged to PepperDash eventually, or stay custom

Rather than maintaining a patch branch, we maintain a **fork** that:
- Stays in sync with upstream (`main` branch)
- Applies Beincourt-specific changes on development branches (`csv-zoom-sandbox-v2`)
- Publishes versioned releases via PD Tools

### Repository Structure

```
Upstream (PepperDash - Read-Only via Git)
└─ https://github.com/PepperDash/epi-zoom-room
   ├─ main (latest stable)
   └─ feature branches (by PepperDash team)

Fork (Beincourt - Full Write Access)
└─ https://github.com/pepperdash-beincourt/epi-bic-zoomroom
   ├─ main (synced from upstream, no custom commits)
   ├─ csv-zoom-sandbox (old development branch, can be archived)
   └─ csv-zoom-sandbox-v2 (active development branch)
       ├─ Beincourt-specific features
       ├─ Bug fixes (e.g., StartSharingOnlyMeeting)
       └─ Automatic semantic-release versioning
```

### Branch Synchronization

**Goal**: Keep `main` branch in sync with upstream, but apply Beincourt customizations on separate branches.

**Sync Process**:
1. Upstream publishes new release → PepperDash/epi-zoom-room/main updates
2. User fetches upstream changes: `git fetch upstream`
3. Rebase `main` on upstream: `git rebase upstream/main` (or create PR for review)
4. Apply Beincourt customizations on `csv-zoom-sandbox-v2` branch
5. Merge selective fixes back to `csv-zoom-sandbox-v2` from `main` as needed

**Never commit Beincourt-specific changes to `main`** — it must stay synchronized with upstream.

## Semantic Release & Versioning

### Automation Pipeline

```
Developer commits to csv-zoom-sandbox-v2
  ↓
Git push to GitHub
  ↓
GitHub Actions: semantic-release.yml triggers
  ↓
Semantic-release analyzes commits
  • Reads commit messages (build:, fix:, feat:, etc.)
  • Determines version bump (patch, minor, major)
  • Generates CHANGELOG.md
  ↓
Creates npm package & git tag
  ↓
Publishes GitHub Release with .tgz asset
  ↓
PD Tools fetches release and displays in dropdown
  ↓
Users deploy version via PD Tools → CP4N or other hardware
```

### Version Format

**Stable releases** (on `main`):
- Format: `MAJOR.MINOR.PATCH`
- Example: `2.0.0`, `2.0.1`
- Pre-release: ❌ No

**Development releases** (on `csv-zoom-sandbox-v2`):
- Format: `MAJOR.MINOR.PATCH-PRERELEASE_IDENTIFIER.ITERATION`
- Example: `2.0.2-csv-zoom-sandbox-v2.1`
- Pre-release: ✅ Yes (marked in GitHub Releases)

### Commit Message Convention

Semantic-release uses [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): description

body (optional)

footer (optional)
```

**Types that trigger releases**:
- `build:` — Triggers **PATCH** release (for development builds)
- `fix:` — Triggers **PATCH** release (bug fixes)
- `feat:` — Triggers **MINOR** release (new features)

**Types that don't trigger releases**:
- `ci:` — CI/CD configuration changes
- `chore:` — Maintenance, updates, refactoring
- `docs:` — Documentation changes
- `test:` — Test additions/changes

**Example commits**:
```bash
# Triggers patch release
git commit -m "build: trigger csv-zoom-sandbox-v2 release"
git commit -m "fix: correct StartSharingOnlyMeeting API call"
git commit -m "feat: add annotation stream support"

# Does NOT trigger release
git commit -m "docs: update README"
git commit -m "ci: add test branch to semantic-release"
```

## Hardware Integration & Testing

### Target Hardware

**CP4N** (Control Processor 4 Series, 4-port network)
- Device: Cisco CP4N at 192.168.100.155
- SSH Access: admin / [credentials in secure vault]
- Purpose: Zoom Room controller for courtroom

### Deployment Flow

1. **Build Phase**
   - Developer commits to `csv-zoom-sandbox-v2`
   - Semantic-release creates version (e.g., `2.0.2-csv-zoom-sandbox-v2.1`)
   - GitHub Release published with .tgz package

2. **Provision Phase**
   - User opens PD Tools (Pepper Dash provisioning portal)
   - Selects Beincourt room
   - Chooses version from dropdown (e.g., `2.0.2-csv-zoom-sandbox-v2.1`)
   - Clicks "Sync" or "Deploy"

3. **Testing Phase**
   - CP4N downloads and installs plugin
   - Testing team validates Zoom Room functionality
   - Reports results back to development team

### Key Features Being Tested

- **Presentation Sharing** — Content visible to far-end participants (StartSharingOnlyMeeting fix)
- **Annotation Support** — NDI stream 3 carries annotation feed
- **Camera Control** — Pan/tilt/zoom operations
- **Meeting State** — Join, leave, lock/unlock, record, etc.
- **Layout Management** — Gallery, speaker, spotlight, custom layouts
- **Participant Management** — Pin/unpin, mute, spotlight participants

## Key Fixes & Customizations

### StartSharingOnlyMeeting API

**Problem**: When presenting in View All mode on Zoom Room, local participants could see content but far-end participants could not.

**Root Cause**: Older Zoom SDK version used `zCommand shareRemote` instead of `zCommand shareView`.

**Solution**: Updated to call correct API endpoint in [src/ZoomRoom.cs](../src/ZoomRoom.cs).

**Impact**: Presentation content now appears in far-end participant displays.

### Annotation Stream Support

**Feature**: Annotation decoder is NDI output stream 3 (separate from presentation content).

**Implementation**: StreamSync device configured to receive stream 3 when annotation mode is enabled.

**Status**: ✅ Working with v2.0.1+

## Design Principles

### 1. Minimal Divergence from Upstream
- Only customize what's necessary for Beincourt
- Upstream improvements are automatically inherited via `main` branch
- Easy to deprecate custom fixes when upstream provides solutions

### 2. Automated, Reproducible Builds
- Semantic-release removes human error from versioning
- Every commit is versioned and released automatically
- Development team sees new builds in PD Tools within minutes

### 3. Clear Branch Identifiers
- Development versions show their source branch (e.g., `2.0.2-csv-zoom-sandbox-v2.1`)
- Team always knows which development iteration they're testing
- No confusion between stable and development releases

### 4. Transparent Versioning
- CHANGELOG.md updated automatically
- GitHub Releases include commit messages and diffs
- Full audit trail from code to production

## Infrastructure & Dependencies

### Build Stack
- **Language**: C# (.NET Framework 4.7+)
- **Build Tool**: .NET CLI / MSBuild
- **Testing**: NUnit
- **Packaging**: npm (for semantic-release metadata)

### Runtime Stack
- **Runtime**: .NET Runtime on CP4N
- **SDK Dependencies**: Zoom Room SDK (via SSH)
- **Integration**: Pepperdash Essentials plugin framework
- **Networking**: IP-based (SSH for Zoom Room, HTTP/HTTPS for APIs)

### CI/CD Stack
- **VCS**: GitHub (git)
- **CI**: GitHub Actions (semantic-release.yml)
- **Release Manager**: semantic-release (npm package)
- **Registry**: GitHub Releases (acts as package repository)
- **Package Distribution**: PD Tools (Pepper Dash portal)

## Future Considerations

### Scaling to Other Libraries
This fork + semantic-release pattern can be replicated for other Pepperdash libraries:
1. [epi-biamp-dsp](https://github.com/PepperDash/epi-biamp-dsp) — Biamp audio presets (mentioned in project scope)
2. [epi-crestron-device-api](https://github.com/PepperDash/epi-crestron-device-api) — Device control
3. Other essential plugins as needed

**Template**: See `.releaserc.json` and `.github/workflows/semantic-release.yml` as reusable starting points.

### Upstream Contribution Strategy
When Beincourt fixes are broadly applicable:
1. Test thoroughly on `csv-zoom-sandbox-v2`
2. Create PR to upstream (`PepperDash/epi-zoom-room`)
3. PepperDash team reviews and merges
4. `main` branch auto-syncs and inherits the fix
5. Remove custom fix from `csv-zoom-sandbox-v2` (no longer needed)

---

**Last Updated**: August 10, 2026
