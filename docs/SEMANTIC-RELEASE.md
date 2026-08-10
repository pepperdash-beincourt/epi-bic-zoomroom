# Semantic Release Configuration & Automation

## Overview

Semantic-release is an automated versioning and release tool that:
- Analyzes commit messages to determine version bumps
- Generates CHANGELOG.md automatically
- Creates git tags and GitHub Releases
- Publishes npm packages
- Integrates with PD Tools for immediate version availability

This eliminates manual version management and ensures consistent, predictable releases.

## How It Works

### Commit Analysis → Version Bump

Semantic-release reads commit messages to determine the type of change:

```
fix: bug fix                 →  PATCH release (v2.0.0 → v2.0.1)
feat: new feature           →  MINOR release (v2.0.0 → v2.1.0)
feat!: breaking change      →  MAJOR release (v2.0.0 → v3.0.0)
build: development build    →  PATCH release (v2.0.1 → v2.0.2)
ci: config change           →  NO release
docs: documentation change  →  NO release
```

### Automated Pipeline

```
Developer commits with semantic message
  ↓
git push to csv-zoom-sandbox-v2
  ↓
GitHub Actions workflow triggered (.github/workflows/semantic-release.yml)
  ↓
semantic-release runs:
  • Loads .releaserc.json config
  • Fetches commits since last release
  • Analyzes commit types
  • Calculates next version
  • Generates CHANGELOG
  • Creates git tag
  • Creates GitHub Release
  • Publishes npm package
  • Updates PD Tools
  ↓
Version appears in PD Tools within 1-2 minutes
  ↓
Users can deploy via PD Tools to hardware
```

## Configuration Files

### .releaserc.json

Located in repository root, this file configures semantic-release behavior:

```json
{
  "plugins": [
    // Plugin configurations (see below)
  ],
  "branches": [
    // Branch configurations (see below)
  ]
}
```

### .github/workflows/semantic-release.yml

Located in `.github/workflows/`, this GitHub Actions workflow triggers semantic-release:

```yaml
name: Semantic Release
on:
  push:
    branches:
      - main
      - csv-zoom-sandbox
      - csv-zoom-sandbox-v2    # Triggers on this branch

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: "22"
      
      - run: npm ci  # Install semantic-release plugins
      - run: npx semantic-release  # Run release
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          NPM_TOKEN: ${{ secrets.NPM_TOKEN }}
```

## Plugin Configuration

Semantic-release uses plugins for each step of the release process:

### 1. Commit Analyzer

**Purpose**: Read commit messages and determine release type

```json
[
  "@semantic-release/commit-analyzer",
  {
    "releaseRules": [
      { "type": "build", "release": "patch" },
      { "scope": "force-patch", "release": "patch" },
      { "scope": "no-release", "release": false }
    ]
  }
]
```

**Key Rules**:
- `build:` commits → PATCH release
- `fix:` commits → PATCH release (default)
- `feat:` commits → MINOR release (default)
- `scope: force-patch` → Force PATCH even if type would suggest MINOR
- `scope: no-release` → Prevent release even with `fix:` or `feat:`

**Example**:
```bash
git commit -m "build: trigger release"  # Patch release
git commit -m "feat: add new feature"   # Minor release
```

### 2. Changelog Generator

**Purpose**: Generate release notes from commit messages

```json
[
  "@semantic-release/release-notes-generator",
  {
    // Default conventional commits format
    // Generates sections: Features, Bug Fixes, Breaking Changes
  }
]
```

**Output** (`CHANGELOG.md`):
```markdown
## [2.0.2] (2026-08-10)

### Bug Fixes
- fix: resolve annotation stream initialization ([#42](issue))
- fix: prevent null reference in meeting state ([#41](issue))

### Features
- feat: add custom layout support ([#45](issue))
```

### 3. Changelog Plugin

**Purpose**: Update CHANGELOG.md file

```json
[
  "@semantic-release/changelog",
  {
    "changelogFile": "CHANGELOG.md"
  }
]
```

### 4. NPM Plugin

**Purpose**: Create npm package (.tgz) and update package.json version

```json
[
  "@semantic-release/npm",
  {
    "npmPublish": true,
    "tarballDir": "dist"
  }
]
```

**Output**: `epi-bic-zoomroom-2.0.2-csv-zoom-sandbox-v2.1.tgz`

**Updates**: `package.json` version field

### 5. Git Plugin

**Purpose**: Create git tag and commit release changes

```json
[
  "@semantic-release/git",
  {
    "assets": ["CHANGELOG.md", "package.json", "package-lock.json"],
    "message": "chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}"
  }
]
```

**Creates**:
- Git tag: `v2.0.2-csv-zoom-sandbox-v2.1`
- Release commit with CHANGELOG + package.json changes

### 6. GitHub Plugin

**Purpose**: Publish GitHub Release with asset

```json
[
  "@semantic-release/github",
  {
    "assets": [
      {
        "path": "dist/*.tgz",
        "label": "NPM Package"
      }
    ]
  }
]
```

**Creates**: GitHub Release on repo with .tgz attachment

### 7. Exec Plugin

**Purpose**: Run custom scripts, update external systems

```json
[
  "@semantic-release/exec",
  {
    "verifyReleaseCmd": "echo 'newVersion=true' >> $GITHUB_OUTPUT",
    "publishCmd": "echo 'version=${nextRelease.version}' >> $GITHUB_OUTPUT"
  }
]
```

**Output**: Stores version info in GitHub Actions environment for PD Tools integration

## Branch Configuration

Different branches have different release strategies:

### main Branch (Stable)

```json
{
  "name": "main",
  "prerelease": false
}
```

**Behavior**:
- Releases are stable (not marked as pre-release on GitHub)
- Version format: `MAJOR.MINOR.PATCH` (e.g., `2.0.1`)
- Intended for production deployments

### csv-zoom-sandbox-v2 Branch (Development)

```json
{
  "name": "csv-zoom-sandbox-v2",
  "prerelease": "csv-zoom-sandbox-v2",
  "channel": "csv-zoom-sandbox-v2"
}
```

**Behavior**:
- Releases are pre-releases (marked as "Pre-release" on GitHub)
- Version format: `MAJOR.MINOR.PATCH-PRERELEASE.ITERATION` (e.g., `2.0.2-csv-zoom-sandbox-v2.1`)
- Branch identifier visible in version number
- Intended for development and testing

**How it works**:
1. First release on branch: `2.0.2-csv-zoom-sandbox-v2.0`
2. Second release: `2.0.2-csv-zoom-sandbox-v2.1`
3. Third release: `2.0.2-csv-zoom-sandbox-v2.2`
4. Patch increment from main: `2.0.3-csv-zoom-sandbox-v2.0` (resets iteration counter)

## Analyzing What Triggers Releases

### Commit Types That Trigger Releases

```
✅ build:  - build commits trigger patch
✅ fix:    - bug fixes trigger patch
✅ feat:   - features trigger minor
✅ feat!:  - breaking features trigger major
```

### Commit Types That Don't Trigger Releases

```
❌ ci:     - CI/CD changes
❌ docs:   - Documentation changes
❌ test:   - Test changes
❌ chore:  - Maintenance changes
❌ refactor: - Code refactoring
```

### Examples

```bash
# ✅ Will trigger patch release
git commit -m "fix: resolve camera timeout"
git commit -m "build: trigger release"

# ✅ Will trigger minor release
git commit -m "feat: add annotation support"

# ❌ Won't trigger release
git commit -m "docs: update deployment guide"
git commit -m "test: add unit tests"
git commit -m "ci: update workflow config"
```

## Manual Release (Advanced)

### Running Locally

To test or manually trigger a release:

```bash
# 1. Install semantic-release tools
npm install -D semantic-release @semantic-release/github @semantic-release/git

# 2. Dry run (analyze without publishing)
npx semantic-release --dry-run

# 3. Actual release (with auth tokens)
GITHUB_TOKEN=xxx NPM_TOKEN=yyy npx semantic-release
```

### Dry Run Output

```
[semantic-release] › ℹ  Start step "analyzeCommits"
[semantic-release] › ℹ  Found 2 commits since last release
[semantic-release] [@semantic-release/commit-analyzer] › ℹ  Analyzing commit: fix: resolve timeout
[semantic-release] › ℹ  The release type for the commit is patch
[semantic-release] › ℹ  The next release version is 2.0.2-csv-zoom-sandbox-v2.1
[semantic-release] › ℹ  Generated release notes...
```

## Troubleshooting Release Issues

### Release Not Triggering

**Issue**: Pushed commit but no release appeared

**Cause**: Likely workflow not configured for that branch

**Solution**:
1. Check `.github/workflows/semantic-release.yml` line 6-7: Is your branch listed?
   ```yaml
   on:
     push:
       branches:
         - main
         - csv-zoom-sandbox-v2  # ← Must include your branch
   ```

2. Check `.releaserc.json` lines 52-62: Is your branch in the branches array?
   ```json
   "branches": [
     "main",
     {
       "name": "csv-zoom-sandbox-v2"  # ← Must include your branch
     }
   ]
   ```

### Version Not Appearing in PD Tools

**Issue**: Release published but PD Tools dropdown doesn't show it

**Cause**: Usually timing (PD Tools caches releases)

**Solution**:
1. Wait 2-3 minutes for PD Tools to refresh
2. Manually refresh PD Tools UI (F5 or refresh button)
3. Check GitHub Releases to confirm it was published
4. Check semantic-release logs in GitHub Actions

### Semantic-Release Error (404 PR Not Found)

**Issue**: Release publishes successfully but fails in "success" step

```
HttpError: Not Found - list-commits-on-a-pull-request
```

**Cause**: Success plugin tries to find PR, but fork doesn't have one

**Solution**: This is a known issue. **The release is published successfully!** Only the notification step fails. No action needed; version is available in PD Tools.

### Version Number Too High or Too Low

**Issue**: Next version doesn't match expected value

**Cause**: Commit analyzer picked up unexpected commit type

**Solution**:
1. Check recent commits: `git log --oneline -5`
2. Verify commit messages use correct format
3. Check `releaseRules` in `.releaserc.json` for custom rules
4. Run dry-run to see what will be analyzed: `npx semantic-release --dry-run`

## Best Practices

### 1. Always Use Semantic Commit Messages

❌ Bad:
```bash
git commit -m "Fixed stuff"
git commit -m "Updates"
git commit -m "WIP"
```

✅ Good:
```bash
git commit -m "fix: resolve camera timeout issue"
git commit -m "feat: add annotation stream support"
git commit -m "docs: update deployment procedures"
```

### 2. One Type Per Commit

❌ Bad:
```bash
git commit -m "feat: add feature + fix: bug + update docs"
```

✅ Good:
```bash
git commit -m "feat: add annotation support"
git commit -m "fix: resolve stream initialization"
git commit -m "docs: add annotation guide"
```

### 3. Use Scopes for Clarity

❌ Bad:
```bash
git commit -m "fix: timeout"
```

✅ Good:
```bash
git commit -m "fix(camera): resolve PTZ command timeout"
git commit -m "fix(meeting): prevent null state reference"
```

### 4. Reference Issues

✅ Best:
```bash
git commit -m "fix: resolve annotation stream
Closes #42
Fixes camera PTZ timeout when switching layouts"
```

## Extending the Release Pipeline

### Adding Custom Release Steps

To run custom scripts during release (e.g., deploy to production):

```json
[
  "@semantic-release/exec",
  {
    "verifyReleaseCmd": "echo 'Deploying version ${nextRelease.version}'",
    "publishCmd": "./scripts/deploy.sh ${nextRelease.version}"
  }
]
```

### Publishing to Additional Registries

Add plugins for different registries:

```json
[
  "@semantic-release/npm",     // npm registry
  "@semantic-release/github",  // GitHub Releases
  "semantic-release-docker",   // Docker Hub (if applicable)
  "@semantic-release/slack"    // Slack notifications (if enabled)
]
```

## Performance Optimization

### Caching Dependencies

GitHub Actions can cache npm dependencies:

```yaml
- uses: actions/setup-node@v4
  with:
    node-version: "22"
    cache: "npm"  # Enables caching
```

### Skipping CI for Release Commits

Prevent infinite loops by skipping CI on release commits:

```json
[
  "@semantic-release/git",
  {
    "message": "chore(release): ${nextRelease.version} [skip ci]"
  }
]
```

The `[skip ci]` flag tells GitHub Actions to skip the workflow for this commit.

---

**Last Updated**: August 10, 2026

**Resources**:
- [Semantic Release Documentation](https://semantic-release.gitbook.io/)
- [Conventional Commits Specification](https://www.conventionalcommits.org/)
