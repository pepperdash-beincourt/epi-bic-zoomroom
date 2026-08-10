# Beincourt Room Framework Documentation

Welcome to the Beincourt room framework documentation. This folder contains all essential information for managing, developing, implementing, and maintaining the Beincourt room system built on the Pepperdash Essentials stack.

## Quick Navigation

### 📋 Management & Strategy
- **[ARCHITECTURE.md](./ARCHITECTURE.md)** — System design, fork strategy, and high-level technical decisions

### 👨‍💻 Development
- **[DEVELOPMENT.md](./DEVELOPMENT.md)** — Development workflow, branch strategy, and coding standards
- **[SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md)** — Automated versioning and release pipeline configuration

### 🚀 Implementation & Deployment
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** — Deployment instructions, PD Tools integration, and hardware testing
- **[CONTRIBUTING.md](./CONTRIBUTING.md)** — Contribution guidelines and commit conventions

### 🔧 Troubleshooting & Reference
- **[TROUBLESHOOTING.md](./TROUBLESHOOTING.md)** — Common issues, solutions, and debugging tips
- **[GLOSSARY.md](./GLOSSARY.md)** — Terminology and abbreviation reference

## Project Overview

The **Beincourt room** is a customized Zoom Room integration built on the Pepperdash Essentials plugin framework. It extends the public [epi-zoom-room](https://github.com/PepperDash/epi-zoom-room) library with Beincourt-specific features and fixes.

### Key Features
- Enhanced Zoom Room control and automation
- Presentation content sharing to far-end participants
- Annotation support via NDI output streams
- Customizable presets and room configurations
- PD Tools integration for remote management and provisioning

## Repository Structure

```
epi-bic-zoomroom/
├── src/                          # C# source code
├── tests/                         # Unit tests
├── docs/                          # Documentation (this folder)
├── .github/
│   └── workflows/
│       └── semantic-release.yml   # Automated release pipeline
├── .releaserc.json                # Semantic-release configuration
├── package.json                   # Node.js project metadata
└── CHANGELOG.md                   # Version history
```

## Branch Strategy

### Active Branches
- **main** — Stable, production-ready code (read-only, syncs with upstream)
- **csv-zoom-sandbox-v2** — Development branch for v2.x features and fixes

### Branch Protection
- `main` requires pull requests and passing CI/CD
- Development on `csv-zoom-sandbox-v2` is free-form; rebasing and history rewrites are allowed

## Release & Versioning

Releases are **automated** using [semantic-release](https://semantic-release.gitbook.io/):

- **Commit format matters**: Use `build:`, `fix:`, `feat:`, etc. prefixes
- **Patch releases** (`build:` commits) increment the third number: `2.0.1` → `2.0.2`
- **Pre-release versions** on `csv-zoom-sandbox-v2` include branch identifier: `2.0.2-csv-zoom-sandbox-v2.1`
- **Automatic CHANGELOG** generation and GitHub Release publishing
- **PD Tools integration** — New versions appear in dropdown within minutes

See [SEMANTIC-RELEASE.md](./SEMANTIC-RELEASE.md) for detailed configuration.

## Getting Started

### First Time Setup
1. Read [DEVELOPMENT.md](./DEVELOPMENT.md) for environment setup
2. Review [CONTRIBUTING.md](./CONTRIBUTING.md) for contribution guidelines
3. Familiarize yourself with the branch strategy above

### For Deployment & Testing
1. Follow [DEPLOYMENT.md](./DEPLOYMENT.md) for hardware integration
2. Check [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) if issues arise

### For Architecture Questions
1. Review [ARCHITECTURE.md](./ARCHITECTURE.md) for design decisions
2. Consult [GLOSSARY.md](./GLOSSARY.md) for terminology

## Key Contacts & Resources

- **Repository**: https://github.com/pepperdash-beincourt/epi-bic-zoomroom
- **Upstream**: https://github.com/PepperDash/epi-zoom-room
- **PD Tools**: Pepper Dash developer portal for provisioning and management
- **Documentation Generator**: See semantic-release.yml for CI/CD automation

## Recent Major Changes

- ✅ **Fork Strategy Established** — Beincourt-specific fork with upstream sync capability
- ✅ **Semantic-Release Automation** — Automated versioning and PD Tools integration
- ✅ **Branch Identifier Versioning** — Development versions show their source branch
- ✅ **StartSharingOnlyMeeting API Fix** — Content now shares to far-end participants in View All mode

## Contributing

All contributors must follow the guidelines in [CONTRIBUTING.md](./CONTRIBUTING.md). Key points:

- Use semantic commit messages (`build:`, `fix:`, `feat:`, etc.)
- Make changes on feature branches, merge to `csv-zoom-sandbox-v2`
- Every commit triggers automated testing and versioning
- Pull requests to upstream (`PepperDash/epi-zoom-room`) are not recommended; coordinate with leadership first

---

**Last Updated**: August 10, 2026  
**Current Stable Version**: 2.0.1  
**Current Development Version**: 2.0.2-csv-zoom-sandbox-v2.1
