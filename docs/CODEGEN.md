# CodeGen - Zero Reflection Execution

## What Is It

Source generator that creates handler execution code at **compile-time**, eliminating all runtime reflection.

## Installation

```bash
dotnet add package Coordix.CodeGen
```

## Prerequisites

- ✅ .NET 6+ (supports source generators)
- ✅ `Coordix` package installed
- ✅ Handlers must have `public Handle()` method

## How to Enable

```csharp
using Coordix.CodeGen.Extensions;

// Use AddCoordixWithCodeGen instead of AddCoordix
builder.Services.AddCoordixWithCodeGen();
```

**Done.** This automatically:
- Registers all core Coordix services (IMediator, IHandlerExecutor, etc.)
- Configures CodeGenPreferred mode
- Uses the generated handler executor

> **Important**: Do **not** call `AddCoordix()` when using CodeGen. Use `AddCoordixWithCodeGen()` instead.

## How It Works

### 1. Build Time

The generator scans your code:

```csharp
// Your handler:
public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
        => await _repository.GetByIdAsync(request.UserId);
}
```

### 2. Generated Code

```csharp
// Generated at: obj/Debug/.../GeneratedHandlerExecutor.g.cs
public class GeneratedHandlerExecutor : IHandlerExecutor
{
    public async Task<TResponse> ExecuteRequestHandler<TResponse>(
        IRequest<TResponse> request, CancellationToken ct)
    {
        if (request is GetUserQuery typedRequest)
        {
            var handler = _provider.GetRequiredService<IRequestHandler<GetUserQuery, UserDto>>();
            var result = await handler.Handle(typedRequest, ct);
            return (TResponse)(object)result;
        }
        // ... other handlers
    }
}
```

**Result:** Zero reflection. Everything is a direct call.

### 3. Runtime

```csharp
await mediator.Send(new GetUserQuery { Id = 1 });
// → GeneratedHandlerExecutor.ExecuteRequestHandler()
// → GetUserHandler.Handle() (direct call)
```

## Why Is It Faster?

### Reflection Mode
```csharp
// 1. Lookup MethodInfo (cached)
var methodInfo = cache.Get(handlerType);

// 2. Create delegate (cached on first call)
var delegateFunc = Expression.Lambda<...>(...).Compile();

// 3. Invoke delegate
return await delegateFunc(handler, request, ct);
```

**Overhead:** Cache lookups + delegate invocation (~500ns)

### CodeGen Mode
```csharp
// 1. Type check (inline, JIT-optimized)
if (request is GetUserQuery typedRequest)
    return await handler.Handle(typedRequest, ct);
```

**Overhead:** Type check only (~200ns)

## Benchmarks

| Scenario | Reflection | CodeGen | Improvement |
|---------|------------|---------|----------|
| Request/Response | 523 ns | 201 ns | **-61.5%** |
| Request (no response) | 412 ns | 156 ns | **-62.1%** |
| Notification (10 handlers) | 2,489 ns | 1,203 ns | **-51.7%** |
| Memory/op | 192 B | 96 B | **-50%** |

**Startup:** CodeGen has **zero overhead** at startup (no delegate compilation needed).

## Roslyn Analyzers

The package includes analyzers that detect problems at **compile-time:**

### COORDIX001: CodeGen without package
```csharp
builder.Services.AddCoordix(options => {
    options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
    // ❌ ERROR: CodeGenPreferred configured but Coordix.CodeGen not referenced
    // Solution: Use AddCoordixWithCodeGen() instead
});
```

### COORDIX002: Handler without public Handle()
```csharp
public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private Task<UserDto> Handle(...) { } // ❌ ERROR: Handle() must be public
}
```

### COORDIX003: Duplicate handlers
```csharp
public class Handler1 : IRequestHandler<GetUser, UserDto> { }
public class Handler2 : IRequestHandler<GetUser, UserDto> { }
// ⚠️ WARNING: Multiple handlers for GetUser
```

### COORDIX005: Non-public handler
```csharp
internal class GetUserHandler : IRequestHandler<...> { }
// ⚠️ WARNING: Handler should be public for codegen to work
```

**Result:** Most bugs caught before runtime.

## Verify It's Working

### 1. Check build output
```
Coordix.CodeGen source generator: Found 15 handlers
  - 8 request handlers with response
  - 3 request handlers without response
  - 4 notification handlers
Generated: GeneratedHandlerExecutor.g.cs
```

### 2. Check generated file
```bash
ls obj/Debug/net8.0/Coordix.CodeGen/Coordix.CodeGen.CoordixSourceGenerator/
# GeneratedHandlerExecutor.g.cs
```

### 3. Runtime log
```
[Info] Using GeneratedHandlerExecutor (CodeGen mode)
```

If you see "Reflection mode" in log, something went wrong.

## Troubleshooting

### "Not generating code"

1. **Clean and rebuild:**
   ```bash
   dotnet clean && dotnet build
   ```

2. **Verify package is referenced:**
   ```bash
   dotnet list package | grep Coordix.CodeGen
   ```

3. **MSBuild verbose:**
   ```bash
   dotnet build -v detailed | grep Coordix
   ```

### "Code generated but still using Reflection"

Make sure you're using the correct registration method:
```csharp
// ❌ Wrong:
builder.Services.AddCoordix();

// ✅ Correct:
builder.Services.AddCoordixWithCodeGen();
```

### "Analyzer not working"

Restart your IDE (analyzers are loaded at startup).

## Limitations

### 1. Partial types not supported
```csharp
public partial class GetUserHandler : IRequestHandler<...> { }
// ⚠️ May not work
```

**Solution:** Use non-partial classes.

### 2. Generic handlers not supported
```csharp
public class Handler<T> : IRequestHandler<Request<T>, Response<T>> { }
// ❌ Not supported
```

**Solution:** Create concrete handlers.

### 3. Dynamic assembly loading
If you load handlers via `Assembly.LoadFrom()` at runtime, CodeGen won't detect them (obviously, they don't exist at compile-time).

**Solution:** Use Reflection mode for these cases.

## When to Use CodeGen?

✅ **Yes:**
- High-performance APIs
- Microservices with fast startup
- Applications with many handlers

❌ **No:**
- Dynamically loaded handlers
- Rapid prototyping (Reflection is simpler)
- Applications that never have performance issues

---

**Golden rule:** CodeGen is **always better** if you accept the extra dependency. If you don't want complexity, Reflection is already fast enough.
