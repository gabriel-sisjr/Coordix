# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [Coordix.CodeGen 0.1.0] - 2025-11-21

### Added

- **Source Generator (`Coordix.CodeGen`)**: New package that generates compile-time handler executors, eliminating reflection overhead entirely
  - `CoordixSourceGenerator`: Roslyn incremental source generator that discovers handlers at compile-time
  - `GeneratedHandlerExecutor`: Auto-generated `IHandlerExecutor` implementation with zero reflection
  - Multi-targeting support (netstandard2.0 for analyzer, netstandard2.1 for runtime extensions)
  - Automatic discovery of `IRequestHandler<TRequest, TResponse>`, `IRequestHandler<TRequest>`, and `INotificationHandler<TNotification>`
  - Generates strongly-typed handler resolution and invocation code
- **DI Integration for CodeGen**:
  - `AddCoordixWithCodeGen()` extension method for seamless integration
  - Automatically replaces reflection-based `HandlerExecutor` with generated version
  - Support for `HandlerResolutionMode.CodeGenPreferred` configuration
  - Runtime discovery of generated executor types
- **CodeGen Sample Application**: Comprehensive sample demonstrating zero-reflection execution
  - Example requests with and without responses
  - Example notification handlers
  - Performance comparison setup
- **CodeGen Unit Tests**: 7 comprehensive tests covering all handler types and scenarios

### Performance

- **Zero Runtime Reflection**: Generated code uses direct type-safe calls instead of `MethodInfo.Invoke()`
- **Compile-Time Optimization**: All handler discovery happens during build, not at runtime
- **Reduced Startup Time**: No reflection scanning or expression tree compilation at startup
- **Better JIT Optimization**: Strongly-typed code allows better inlining and optimization

### Documentation

- CodeGen-specific README with installation and usage instructions
- Sample application demonstrating all features
- Integration guide with existing Coordix applications

## [0.2.0] - 2025-11-20

### Added

- **Handler Execution Registry (`IHandlerExecutor`)**: Centralized handler execution abstraction that eliminates scattered reflection throughout the codebase
  - `IHandlerExecutor` interface for handler lookup, caching, and invocation
  - `HandlerExecutor` implementation with reflection-based execution and performance optimizations
  - All handler execution logic now centralized in a single place
- **Configuration Options (`CoordixOptions`)**: New configuration class for customizing Coordix behavior
  - `HandlerResolutionMode` enum with `Reflection` (default) and `CodeGenPreferred` modes
  - `AddCoordix()` now accepts optional `Action<CoordixOptions>` for configuration
  - Support for mode-dependent registry registration (prepares for future code generation support)
- **Enhanced Background Processing**: Improved background job processing architecture
  - `BackgroundWorker` now uses `IHandlerExecutor` instead of direct `IMediator` reflection
  - Each background job processed in its own service scope for proper lifetime management
  - Reduced reflection overhead in background processing
- **Extended API**: New overloads for `AddCoordix()` method
  - `AddCoordix(IServiceCollection)` - default configuration
  - `AddCoordix(IServiceCollection, Action<CoordixOptions>)` - with configuration
  - `AddCoordix(IServiceCollection, params object[])` - with assembly scanning (backward compatible)
  - `AddCoordix(IServiceCollection, Action<CoordixOptions>, params object[])` - full configuration

### Changed

- **Architecture Refactoring**: `Mediator` now delegates handler execution to `IHandlerExecutor`
  - `Mediator` is simplified to only discover request/notification types
  - All reflection, caching, and invocation logic moved to `HandlerExecutor`
  - Better separation of concerns and extensibility
- **Background Worker Improvements**:
  - Replaced heavy reflection on `IMediator` with `IHandlerExecutor` usage
  - Improved scope management: creates `IServiceScope` per job instead of using root scope
  - Better error messages and logging
- **Service Registration**: Updated registration order and dependencies
  - `IHandlerExecutor` registered as singleton before `IMediator`
  - `CoordixOptions` registered as singleton for dependency injection
  - Mode-dependent registry registration (Reflection vs CodeGenPreferred)

### Fixed

- Background jobs now properly handle scoped services with per-job service scopes
- Eliminated scattered reflection throughout the codebase
- Improved thread safety with centralized caching

### Documentation

- Updated all documentation to reflect new architecture with `IHandlerExecutor`
- Added documentation for `CoordixOptions` and `HandlerResolutionMode`
- Updated API reference with new interfaces and configuration options
- Enhanced background jobs documentation with architecture details
- Updated FAQ with questions about handler resolution modes

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

[Unreleased]: https://github.com/gabriel-sisjr/Coordix/compare/v0.2.0...develop
[0.2.0]: https://github.com/gabriel-sisjr/Coordix/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/gabriel-sisjr/Coordix/compare/v0.0.4...v0.1.0
[0.0.4]: https://github.com/gabriel-sisjr/Coordix/compare/v0.0.1...v0.0.4
[0.0.1]: https://github.com/gabriel-sisjr/Coordix/releases/tag/v0.0.1
