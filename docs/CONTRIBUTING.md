# Contributing Guidelines

Welcome to the Beincourt room framework! This guide explains how to contribute code, report issues, and help improve the project.

## Code of Conduct

All contributors must follow these principles:
- **Respect** — Treat colleagues and maintainers with respect
- **Collaboration** — Work together to solve problems
- **Transparency** — Communicate clearly about changes and decisions
- **Quality** — Strive for code that's maintainable, tested, and well-documented

## Getting Started

1. Read [ARCHITECTURE.md](./ARCHITECTURE.md) to understand the fork strategy and system design
2. Follow [DEVELOPMENT.md](./DEVELOPMENT.md) for setup and development workflow
3. Review this guide for contribution standards

## Contribution Types

### 1. Bug Reports & Issue Tracking

**Before creating an issue:**
- Search existing issues to avoid duplicates
- Reproduce the bug consistently
- Gather diagnostic information (logs, version, hardware)

**When creating an issue:**
```markdown
## Bug: [Brief description]

**Severity**: Critical / High / Medium / Low

**Environment**:
- Plugin Version: 2.0.2-csv-zoom-sandbox-v2.1
- Hardware: CP4N at 192.168.100.155
- Zoom Room OS: [version if known]

**Reproduction Steps**:
1. Step one
2. Step two
3. Step three

**Expected Behavior**:
[What should happen]

**Actual Behavior**:
[What actually happens]

**Logs/Screenshots**:
[Attach diagnostic logs or error messages]
```

### 2. Code Changes (Features & Fixes)

Follow the workflow in [DEVELOPMENT.md](./DEVELOPMENT.md):

```bash
# 1. Create feature branch from csv-zoom-sandbox-v2
git checkout csv-zoom-sandbox-v2
git checkout -b fix/bug-name  # or feature/feature-name

# 2. Make changes, test locally
# Edit files, run tests
dotnet test tests/

# 3. Commit with semantic message (CRITICAL!)
git commit -m "type(scope): description"

# 4. Push and create pull request (or merge directly)
git push fork fix/bug-name
# Open PR on GitHub for team review
```

### 3. Documentation & Guides

Documentation lives in `docs/` folder. Contribute by:

- Updating existing `.md` files for clarity and completeness
- Adding new guides for complex procedures
- Fixing typos and grammatical errors
- Adding examples and code snippets
- Updating version numbers and dates

**Documentation commit format**:
```bash
git commit -m "docs: clarify deployment process for new developers"
```

### 4. Testing & Quality Assurance

Help improve test coverage:

- Write unit tests for new functionality
- Report gaps in test coverage
- Perform manual testing and report results
- Suggest test scenarios for edge cases

**Test commit format**:
```bash
git commit -m "test: add unit tests for camera PTZ operations"
```

## Commit Message Standards

**Every commit must use Conventional Commits format** or it will not trigger semantic versioning:

### Format
```
type(scope): description

optional body explaining what and why

optional footer (e.g., Closes #123)
```

### Types & Examples

| Type | Purpose | Example | Release? |
|------|---------|---------|----------|
| `build:` | Development builds, release triggers | `build: trigger v2.0.2 release` | ✅ Patch |
| `fix:` | Bug fixes | `fix: resolve annotation stream init` | ✅ Patch |
| `feat:` | New features | `feat: add custom layout support` | ✅ Minor |
| `ci:` | CI/CD changes | `ci: add csv-zoom-sandbox-v2 to releases` | ❌ No |
| `docs:` | Documentation | `docs: update deployment guide` | ❌ No |
| `test:` | Tests | `test: add camera control tests` | ❌ No |
| `chore:` | Maintenance | `chore: update dependencies` | ❌ No |
| `refactor:` | Code restructuring | `refactor: simplify SDK controller` | ❌ No |

### Commit Message Examples

**Good examples** (will trigger releases):
```bash
git commit -m "fix: prevent null reference in meeting state handling"
git commit -m "fix: correct StartSharingOnlyMeeting API call"
git commit -m "feat: add support for presenter notes"
```

**Bad examples** (won't trigger releases, but still valid):
```bash
git commit -m "Update controller"  # Missing type
git commit -m "Fix stuff"  # Too vague
git commit -m "WIP: testing camera"  # Incomplete work
```

**Best practices**:
- Use imperative mood: "add feature" not "added feature"
- Be specific: "fix: resolve timeout in SDKConnector" not "fix: timeout"
- Include affected scope: "fix(camera): pan speed too slow" not just "fix: slow"
- Reference issues: "fix: resolve camera lag\n\nCloses #42"

## Pull Request Process

### For Development Team

1. **Create PR from feature branch to `csv-zoom-sandbox-v2`**:
   ```
   Title: feat: add annotation stream support
   Description: Implements NDI stream 3 binding for annotation display
   
   Closes #15
   - [x] Tests passing locally
   - [x] No new warnings
   - [x] Docs updated
   ```

2. **Request reviewers** — Assign to team leads or experts in that area

3. **Address feedback** — Make requested changes and push to same branch (auto-updates PR)

4. **Merge when approved**:
   - Use "Squash and merge" for single logical commit
   - Or "Create a merge commit" if multiple commits tell a story
   - Ensure merge commit message is semantic (e.g., "fix: ...")

### For External Contributors

1. Fork the repository
2. Create branch from `csv-zoom-sandbox-v2`
3. Make changes with proper commit messages
4. Submit pull request to `csv-zoom-sandbox-v2` (not `main`)
5. Wait for team review (may take 1-2 weeks)
6. Team will merge when approved

## Code Style & Standards

### C# Code Style

**Naming Conventions**:
- Classes: PascalCase (`ZoomRoomController`)
- Methods: PascalCase (`JoinMeeting`, `SendCommand`)
- Properties: PascalCase (`IsOnCall`, `CurrentLayout`)
- Fields (private): _camelCase (`_sdkConnection`, `_statusCache`)
- Local variables: camelCase (`meetingId`, `participantCount`)
- Constants: UPPER_SNAKE_CASE (`MAX_RETRY_ATTEMPTS`, `DEFAULT_TIMEOUT_MS`)

**Formatting**:
- Indentation: 4 spaces (no tabs)
- Line length: Aim for < 120 characters
- Braces: Allman style (opening brace on new line for classes/methods)
  ```csharp
  public void JoinMeeting(string meetingUrl)
  {
      // Implementation
  }
  ```
- Use `var` for obvious types, explicit types for clarity

**Documentation**:
- XML doc comments for public methods:
  ```csharp
  /// <summary>Joins a Zoom meeting on the Zoom Room device.</summary>
  /// <param name="meetingUrl">Zoom meeting URL or meeting ID</param>
  /// <returns>True if join was initiated successfully</returns>
  public bool JoinMeeting(string meetingUrl)
  {
      // Implementation
  }
  ```

### Testing Standards

- Every public method should have at least one unit test
- Test names describe the scenario: `JoinMeeting_WithValidUrl_ReturnsTrue`
- Use AAA pattern (Arrange, Act, Assert):
  ```csharp
  [Test]
  public void SendCommand_WithValidCommand_SuccessfullyTransmits()
  {
      // Arrange
      var controller = new ZrcSdkController("192.168.1.100");
      
      // Act
      var result = controller.SendCommand("zCommand join \"Meeting 123\"");
      
      // Assert
      Assert.That(result, Is.True);
  }
  ```

### Documentation Standards

- Every feature should have corresponding documentation
- Update CHANGELOG.md for user-facing changes (auto-generated for releases)
- Add code comments for non-obvious logic:
  ```csharp
  // Zoom Room API requires share mode to be set before calling shareView
  // to ensure content reaches far-end participants
  SendCommand("zCommand shareMode start");
  SendCommand("zCommand shareView start");
  ```

## Review Process

### What Reviewers Look For

- ✅ Commit messages follow Conventional Commits
- ✅ Code follows style guidelines
- ✅ Tests pass locally: `dotnet test tests/`
- ✅ New functionality has corresponding tests
- ✅ No breaking changes to public API (without major version bump)
- ✅ Documentation updated if user-facing change
- ✅ Clear explanation of what and why (in PR description)

### What to Expect

- Team reviews within 24-48 hours
- Feedback typically within 1-2 business days
- May request changes or ask clarifying questions
- Once approved, reviewer will merge

## Releasing

### Automatic Release Process

1. Code merged to `csv-zoom-sandbox-v2` with semantic commit message
2. GitHub hook triggers semantic-release.yml workflow
3. Semantic-release analyzes commits and bumps version
4. Tag created (e.g., `v2.0.2-csv-zoom-sandbox-v2.1`)
5. GitHub Release published with .tgz asset
6. PD Tools updated within 1-2 minutes
7. Version available for deployment

### No Manual Release Steps Required!

The process is fully automated. Just follow proper commit messages and semantic-release handles the rest.

## Reporting Security Issues

**Do not open public issues for security vulnerabilities.**

Instead:
1. Email security concerns to: [security contact - TBD]
2. Include: Description, reproduction steps, potential impact
3. Do not publish exploit details until patch is released

## Getting Help

### Documentation
- [ARCHITECTURE.md](./ARCHITECTURE.md) — System design and fork strategy
- [DEVELOPMENT.md](./DEVELOPMENT.md) — Development setup and workflow
- [DEPLOYMENT.md](./DEPLOYMENT.md) — Hardware deployment procedures
- [SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md) — Release configuration details
- [GLOSSARY.md](./GLOSSARY.md) — Terminology reference

### Team Contact
- **Lead Developer**: [Name/Contact - TBD]
- **Architecture Lead**: [Name/Contact - TBD]
- **Hardware Testing**: [Name/Contact - TBD]

### Discussion Channels
- GitHub Discussions (if enabled)
- Slack channel: #beincourt-dev (if applicable)
- Team standups (Mondays/Wednesdays)

## Contributor Recognition

Contributors are recognized in:
- CHANGELOG.md (for each release)
- GitHub contributors page
- Project documentation acknowledgements

Thank you for contributing! 🎉

---

**Last Updated**: August 10, 2026
