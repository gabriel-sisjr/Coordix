# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Remove auto-merge from Dependabot workflow to require manual approval for all dependency updates
- Skip commitlint validation for Dependabot PRs to prevent workflow failures

### Fixed

- Group Dependabot updates into single PR instead of multiple PRs per dependency

## [0.1.0] - 2025-11-15

### Added

- Concurrent dictionaries to cache handlers for improved performance and thread-safety
- Dotnet format check to pre-commit hook to prevent invalid code formatting commits
- Code formatting verification in CI/CD pipeline
- Dependabot configuration with ignore rules for major version updates that may break compatibility
- GitHub Actions workflows:
  - CI/CD pipeline with build and test automation
  - Code quality checks (formatting, commit linting)
  - Security scanning
  - NuGet publishing workflow with support for pre-release and production packages
- Comprehensive documentation in `docs/` folder:
  - Installation guide
  - Getting started tutorial
  - Usage guide with advanced patterns
  - Performance documentation and benchmarks
  - Complete API reference
  - Best practices guide
  - FAQ section
  - Migration guide from other mediator libraries (MediatR, etc.)
- Repository reorganization with structured folders (`src/`, `tests/`, `samples/`)
- Code quality and commit linting configuration (Conventional Commits)
- Pre-commit hooks for automatic code formatting
- Solution file (`Coordix.sln`) with organized project references

### Changed

- Repository structure reorganized to `src/Coordix/`, `tests/`, and `samples/`
- Code formatting standardized to use tabs instead of spaces (per `.editorconfig`)
- Updated README.md with comprehensive documentation, examples, and comparison table
- Improved Dependabot configuration to prevent breaking updates and group updates into single PR
- NuGet package metadata updated with proper repository information and icons

### Fixed

- Code formatting issues (whitespace, encoding, import ordering)
- Husky hooks deprecated initialization lines removed

## [0.0.4] - 2025-04-30

### Added

- Test project setup with xUnit, Moq, and coverlet.collector
- Example projects (SimpleSample and AdvancedSample)
- Logo and updated README with better documentation
- MIT License

### Changed

- Updated folder structure
- Added readme and summary to INotification interface

## [0.0.1] - 2025-04-21

### Added

- Initial release
- Basic mediator implementation
- Request/Response pattern support
- Notification pattern support
- Dependency Injection integration
- Logo and README documentation
- MIT License

[Unreleased]: https://github.com/gabriel-sisjr/Coordix/compare/v0.1.0...develop
[0.1.0]: https://github.com/gabriel-sisjr/Coordix/compare/v0.0.4...v0.1.0
[0.0.4]: https://github.com/gabriel-sisjr/Coordix/compare/v0.0.1...v0.0.4
[0.0.1]: https://github.com/gabriel-sisjr/Coordix/releases/tag/v0.0.1
