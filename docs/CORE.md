# Core - How It Works

## Execution Flow

```
Request/Command → IMediator → IHandlerExecutor → Handler → Response
                                    ↓
                            (Reflection OR CodeGen)
```

### 1. Request with Response
```csharp
var result = await mediator.Send(new GetUserQuery { Id = 1 });
// IMediator → IHandlerExecutor → GetUserQueryHandler.Handle()
```

### 2. Request without Response (Command)
```csharp
await mediator.Send(new CreateUserCommand { Name = "John" });
// IMediator → IHandlerExecutor → CreateUserCommandHandler.Handle()
```

### 3. Notification (Event)
```csharp
await mediator.Publish(new UserCreatedEvent { UserId = 1 });
// IMediator → IHandlerExecutor → ALL handlers in parallel
```

## DI Interoperability

### Automatic Registration
```csharp
builder.Services.AddCoordix(); // Automatically scans assemblies
```

**What happens:**
1. Finds all `IRequestHandler<,>` and `INotificationHandler<>` in assembly
2. Registers in DI as **Scoped** (default)
3. Registers `IMediator` as **Singleton**
4. Registers `IHandlerExecutor` based on chosen mode

### Manual Registration
```csharp
builder.Services.AddSingleton<IMediator, Mediator>();
builder.Services.AddScoped<IRequestHandler<GetUser, UserDto>, GetUserHandler>();
```

Use when you need fine-grained control over lifetimes.

## Execution Modes

### Reflection (Default)
```csharp
builder.Services.AddCoordix(options => {
    options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
});
```

**How it works:**
- Uses `Expression Trees` to create compiled delegates
- First call: creates and caches delegate (~1ms overhead)
- Subsequent calls: direct invocation via delegate (~500ns)

**When to use:** You don't want to add extra dependencies

### CodeGen (Zero Reflection)
```csharp
using Coordix.CodeGen.Extensions;

// Install: dotnet add package Coordix.CodeGen
builder.Services.AddCoordixWithCodeGen();
```

**How it works:**
- Source generator creates code at **compile-time**
- Runtime: zero reflection, direct calls
- Performance: ~60% faster than Reflection mode

**When to use:** Performance is critical or you want faster startup

> **Important**: Use `AddCoordixWithCodeGen()` instead of `AddCoordix()` when using CodeGen mode. This automatically registers all core services with CodeGen enabled.

## Scoped Services

Handlers can receive scoped dependencies:

```csharp
public class CreateUserHandler : IRequestHandler<CreateUser>
{
    private readonly AppDbContext _db; // Scoped

    public CreateUserHandler(AppDbContext db) => _db = db;

    public async Task Handle(CreateUser request, CancellationToken ct)
    {
        _db.Users.Add(new User { Name = request.Name });
        await _db.SaveChangesAsync(ct);
    }
}
```

**Guarantees:**
- Handlers are resolved within HTTP request scope
- Background jobs create **new scope per job**
- `DbContext` is correctly disposed after completion

## Exception Handling

```csharp
try {
    await mediator.Send(command);
} catch (InvalidOperationException ex) when (ex.Message.Contains("Handler not found")) {
    // Handler not registered
} catch (Exception ex) {
    // Exception from handler itself
}
```

**Behavior:**
- Handler exceptions are **propagated** to caller
- Mediator does **not** swallow exceptions
- Notifications: first exception stops execution (subsequent handlers don't run)

## Performance

| Operation | Reflection | CodeGen | Overhead |
|----------|------------|---------|----------|
| Send<TResponse> | ~500ns | ~200ns | -60% |
| Send (no response) | ~400ns | ~150ns | -62% |
| Publish (10 handlers) | ~2.5μs | ~1.2μs | -52% |

**Conclusion:** CodeGen is always faster, but Reflection is already optimized.

---

**Golden rule:** If you don't have performance problems, use Reflection. If you do, use CodeGen.

