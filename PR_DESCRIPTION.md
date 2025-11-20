# 🚀 Version 0.2.0: Centralized Handler Execution & Configuration Options

## 📋 Summary

This PR introduces a major architectural refactoring that centralizes all handler execution logic into a single registry (`IHandlerExecutor`), eliminates scattered reflection throughout the codebase, and adds configuration options for future extensibility. It also improves background job processing with proper scope management and reduces reflection overhead.

**Version Bump:** `0.1.0` → `0.2.0`

## ✨ Key Changes

### 🎯 Centralized Handler Execution Registry

- **New `IHandlerExecutor` interface**: Central abstraction for handler lookup, caching, and invocation
- **New `HandlerExecutor` implementation**: Reflection-based executor with all performance optimizations
- **Simplified `Mediator`**: Now only discovers request/notification types and delegates to the registry
- **Eliminated scattered reflection**: All reflection logic is now in one place

### ⚙️ Configuration Options

- **New `CoordixOptions` class**: Configuration options for customizing Coordix behavior
- **New `HandlerResolutionMode` enum**:
  - `Reflection` (default) - Uses reflection with cached delegates
  - `CodeGenPreferred` - Placeholder for future code generation support
- **Extended `AddCoordix()` API**: Now accepts optional configuration delegate

### 🔄 Background Processing Improvements

- **Uses `IHandlerExecutor`**: Replaced heavy reflection on `IMediator` with registry usage
- **Proper scope management**: Each background job processed in its own `IServiceScope`
- **Reduced reflection overhead**: Minimal reflection only for generic method invocation
- **Better error handling**: Clear error messages and improved logging

### 📚 Documentation Updates

- Updated all documentation to reflect new architecture
- Added API reference for new interfaces and configuration options
- Enhanced background jobs documentation with architecture details
- Updated FAQ with questions about handler resolution modes

### 🔧 CI/CD Improvements

- Consolidated workflows into single `CI` workflow
- Reduced GitHub Actions pollution
- Simplified maintenance

## 🔍 Technical Details

### Architecture Changes

**Before:**

```
Mediator → Direct reflection + caching
BackgroundWorker → Direct reflection on IMediator
```

**After:**

```
Mediator → IHandlerExecutor (registry)
BackgroundWorker → IHandlerExecutor (registry)
HandlerExecutor → Centralized reflection + caching
```

### New Files

- `src/Core/Interfaces/IHandlerExecutor.cs` - Handler execution interface
- `src/Core/Implementation/HandlerExecutor.cs` - Reflection-based implementation
- `src/Core/CoordixOptions.cs` - Configuration options
- `src/Core/HandlerResolutionMode.cs` - Resolution mode enum

### Modified Files

- `src/Core/Implementation/Mediator.cs` - Simplified to delegate to registry
- `src/Background/Implementation/BackgroundWorker.cs` - Uses registry, improved scope management
- `src/Core/Extensions/ServiceCollectionExtensions.cs` - Added configuration support
- All test files updated to reflect new architecture

## 🔄 Breaking Changes

**None** - All changes are backward compatible:

- Existing `AddCoordix()` calls continue to work
- All APIs remain the same
- New functionality is opt-in via configuration

## ✅ Testing

- ✅ All existing tests updated and passing
- ✅ New tests for `HandlerExecutor` functionality
- ✅ Background worker tests updated for new architecture
- ✅ Mediator tests updated to use mocked `IHandlerExecutor`

## 📖 Usage Examples

### Basic Usage (Unchanged)

```csharp
services.AddCoordix();
```

### With Configuration

```csharp
services.AddCoordix(options =>
{
    options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
});
```

### With Assembly Scanning

```csharp
services.AddCoordix(
    options => options.HandlerResolutionMode = HandlerResolutionMode.Reflection,
    typeof(MyHandler).Assembly);
```

## 🎯 Benefits

1. **Better Architecture**: Centralized handler execution logic
2. **Extensibility**: Ready for code generation support (future)
3. **Performance**: Reduced reflection overhead in background jobs
4. **Maintainability**: Single point of reflection, easier to maintain
5. **Proper Scoping**: Background jobs now properly handle scoped services

## 📊 Statistics

- **Files Changed**: 23
- **Lines Added**: +1,141
- **Lines Removed**: -431
- **Net Change**: +710 lines

## 🔗 Related Issues

- Centralizes handler execution logic
- Prepares infrastructure for code generation
- Improves background job processing

## ✅ Checklist

- [x] Code follows project style guidelines
- [x] Tests added/updated and passing
- [x] Documentation updated
- [x] CHANGELOG.md updated
- [x] Version bumped to 0.2.0
- [x] No breaking changes
- [x] CI workflows consolidated
- [x] All pre-commit checks passing

## 🚦 Ready for Review

This PR is ready for review and merge. All tests pass, documentation is updated, and the changes are backward compatible.
