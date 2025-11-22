# Troubleshooting - Common Errors

## Handler Not Found

### Error
```
System.InvalidOperationException: Handler not found for GetUserQuery
```

### Causes
1. **Handler not registered in DI**
2. **Assembly not scanned by `AddCoordix()`**
3. **Handler is not public**

### Solutions

#### 1. Verify registration
```csharp
// ✅ Automatic (recommended)
builder.Services.AddCoordix(); // Scans current assembly

// ✅ Manual
builder.Services.AddScoped<IRequestHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();
```

#### 2. Verify handler is public
```csharp
// ❌ WRONG
internal class GetUserQueryHandler : IRequestHandler<...> { }

// ✅ CORRECT
public class GetUserQueryHandler : IRequestHandler<...> { }
```

#### 3. Verify assemblies
```csharp
// If handlers are in another assembly:
builder.Services.AddCoordix(
    typeof(GetUserQueryHandler).Assembly, // Assembly with handlers
    typeof(Program).Assembly
);
```

---

## CodeGen Not Generating Code

### Error
```
[Warning] CodeGenPreferred configured but falling back to Reflection mode
```

### Causes
1. **`Coordix.CodeGen` package not installed**
2. **Build cache corrupted**
3. **Source generator not loaded**

### Solutions

#### 1. Install package
```bash
dotnet add package Coordix.CodeGen
```

#### 2. Clean and rebuild
```bash
dotnet clean
dotnet build
```

#### 3. Verify reference
```xml
<!-- Your .csproj should have: -->
<ItemGroup>
  <PackageReference Include="Coordix" Version="0.2.0" />
  <PackageReference Include="Coordix.CodeGen" Version="0.1.0" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

#### 4. Verify generation
```bash
# Should exist:
ls obj/Debug/net8.0/Coordix.CodeGen/Coordix.CodeGen.CoordixSourceGenerator/GeneratedHandlerExecutor.g.cs
```

---

## Background Jobs Not Executing

### Symptom
Jobs enqueued but never processed.

### Causes
1. **`AddCoordixBackground()` not called**
2. **Background worker not started**
3. **Application shutdown before processing**

### Solutions

#### 1. Register worker
```csharp
builder.Services.AddCoordix();
builder.Services.AddCoordixBackground(); // ← REQUIRED
```

#### 2. Check logs
```
[Info] Background worker started  ← Should appear
```

If it doesn't appear, worker wasn't registered.

#### 3. Graceful shutdown
```csharp
var app = builder.Build();
// ...
await app.RunAsync(); // Use RunAsync, not Run()
```

---

## Scoped Service Error

### Error
```
System.InvalidOperationException: Cannot resolve scoped service 'AppDbContext' from root provider
```

### Cause
Trying to use scoped service in mediator (which is singleton).

### Solution
**DON'T inject scoped services into mediator.** Inject into **handler**:

```csharp
// ❌ WRONG
public class MyController
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db; // Scoped

    public MyController(IMediator mediator, AppDbContext db) { }
}

// ✅ CORRECT
public class GetUserHandler : IRequestHandler<GetUser, UserDto>
{
    private readonly AppDbContext _db; // Scoped

    public GetUserHandler(AppDbContext db) => _db = db;
}
```

**Why?** `IMediator` is singleton, so it can only receive singleton dependencies. Handlers are scoped, so they can receive any lifetime.

---

## Analyzer Warnings

### COORDIX001: CodeGenPreferred without package
```csharp
options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
// ❌ ERROR: Install Coordix.CodeGen or use Reflection
```

**Solution:**
```bash
dotnet add package Coordix.CodeGen
```

### COORDIX002: Handle() not public
```csharp
public class MyHandler : IRequestHandler<MyRequest, string>
{
    private Task<string> Handle(...) { } // ❌
}
```

**Solution:**
```csharp
public async Task<string> Handle(MyRequest request, CancellationToken ct)
{
    // ...
}
```

### COORDIX003: Duplicate handlers
```csharp
public class Handler1 : IRequestHandler<GetUser, UserDto> { }
public class Handler2 : IRequestHandler<GetUser, UserDto> { }
// ⚠️ WARNING
```

**Solution:** Remove one handler or use different types.

### COORDIX005: Non-public handler
```csharp
internal class GetUserHandler : IRequestHandler<...> { }
// ⚠️ WARNING
```

**Solution:** Make it public:
```csharp
public class GetUserHandler : IRequestHandler<...> { }
```

---

## Performance Worse Than Expected

### Symptom
Latency higher than benchmarks indicate.

### Causes
1. **Cold start** (first execution compiles delegates)
2. **Debug mode** instead of Release
3. **Handlers doing heavy operations**

### Diagnosis

#### 1. Check mode
```csharp
// On first request, log should show:
[Debug] First execution of GetUserHandler - compiling delegate
```

Second requests should be **much** faster.

#### 2. Profile with diagnostics
```bash
dotnet run -c Release
# or
dotnet run -c Debug --no-build
```

Debug mode disables optimizations.

#### 3. Use BenchmarkDotNet
```bash
cd tests/Benchmarks
dotnet run -c Release -- --filter *YourScenario*
```

---

## Background Jobs Are Lost

### Symptom
Jobs enqueued but not processed after restart.

### Cause
**Expected behavior.** Coordix background jobs are **in-memory** and **not durable**.

### Solution
If you need durability, use:
- **Hangfire** (persists to SQL)
- **MassTransit** (persists to RabbitMQ/Azure Service Bus)
- **Quartz.NET** (persistent scheduler)

Coordix.Background is for **non-critical** jobs only.

---

## Notification Handler Not Called

### Symptom
`Publish()` doesn't call some handler.

### Causes
1. **Handler not registered**
2. **Exception in previous handler** (notifications stop on first exception)

### Solutions

#### 1. Verify registration
```csharp
builder.Services.AddTransient<INotificationHandler<MyEvent>, Handler1>();
builder.Services.AddTransient<INotificationHandler<MyEvent>, Handler2>();
// Both must be registered
```

#### 2. Check exception logs
```
[Error] Error executing notification handler: Handler1
```

If one handler fails, subsequent ones don't run.

**Solution:** Handle exceptions inside handlers:
```csharp
public async Task Handle(MyEvent notification, CancellationToken ct)
{
    try {
        // Logic that may fail
    } catch (Exception ex) {
        _logger.LogError(ex, "Handler failed but continuing");
        // Don't re-throw
    }
}
```

---

## NuGet Package Not Found

### Error
```
error NU1101: Unable to find package 'Coordix'
```

### Cause
Package not published or incorrect NuGet source.

### Solution
```bash
# Check sources
dotnet nuget list source

# Add nuget.org if needed
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

---

## Questions?

If your error isn't listed here:

1. **Enable detailed logs:**
   ```csharp
   builder.Logging.SetMinimumLevel(LogLevel.Debug);
   ```

2. **Open an issue:** https://github.com/gabriel-sisjr/coordix/issues

3. **Include:**
   - Code that reproduces the problem
   - Full stack trace
   - Coordix and .NET version

---

**Golden rule:** Most errors are DI registration problems. Always check that first.
