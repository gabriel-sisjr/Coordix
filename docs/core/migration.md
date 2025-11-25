# Migration Guide

This guide helps you migrate from other mediator libraries to Coordix.

## Migrating from MediatR

### Step 1: Install Coordix

```bash
dotnet remove package MediatR
dotnet add package Coordix
```

### Step 2: Update Using Statements

Replace:
```csharp
using MediatR;
```

With:
```csharp
using Coordix.Interfaces;
```

### Step 3: Update Registration

Replace:
```csharp
services.AddMediatR(typeof(MyHandler).Assembly);
```

With:
```csharp
using Coordix.Extensions;

services.AddCoordix(typeof(MyHandler).Assembly);

// Or use the alias for compatibility
services.AddMediator(typeof(MyHandler).Assembly);
```

### Step 4: Update Interface Names

The interfaces are the same, but in a different namespace:

| MediatR | Coordix |
|---------|---------|
| `IRequest<TResponse>` | `IRequest<TResponse>` |
| `IRequest` | `IRequest` |
| `INotification` | `INotification` |
| `IRequestHandler<TRequest, TResponse>` | `IRequestHandler<TRequest, TResponse>` |
| `IRequestHandler<TRequest>` | `IRequestHandler<TRequest>` |
| `INotificationHandler<TNotification>` | `INotificationHandler<TNotification>` |

### Step 5: Update Handler Signatures

Handlers have the same signature, so no changes needed:

```csharp
// Works the same in both
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    public async Task<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        // Your code
    }
}
```

### Step 6: Update Mediator Usage

The `IMediator` interface is the same:

```csharp
// Works the same in both
var response = await mediator.Send(new MyRequest());
await mediator.Publish(new MyEvent());
```

### Differences to Be Aware Of

1. **No Pipeline Behaviors**: Coordix doesn't support pipeline behaviors. If you need this, consider:
   - Moving validation to handlers
   - Using middleware in your application
   - Using decorators

2. **No Request Pre/Post Processors**: Handle this in your handlers or use decorators

3. **No Publish Strategies**: All notification handlers run in parallel using `Task.WhenAll`

4. **Service Lifetime**: Handlers are registered as Transient by default (MediatR uses Scoped)

## Migrating from Other Libraries

### General Migration Steps

1. **Install Coordix**
   ```bash
   dotnet add package Coordix
   ```

2. **Update Namespaces**
   ```csharp
   using Coordix.Interfaces;
   using Coordix.Extensions;
   ```

3. **Register Services**
   ```csharp
   services.AddCoordix();
   ```

4. **Update Handler Interfaces**
   - Implement `IRequestHandler<TRequest, TResponse>` for requests with response
   - Implement `IRequestHandler<TRequest>` for requests without response
   - Implement `INotificationHandler<TNotification>` for notifications

5. **Update Mediator Usage**
   ```csharp
   var response = await mediator.Send(new MyRequest());
   await mediator.Publish(new MyEvent());
   ```

## Common Migration Patterns

### Pattern 1: Simple Request/Response

**Before (MediatR):**
```csharp
public class GetUserQuery : IRequest<UserDto> { }
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

**After (Coordix):**
```csharp
// Same code! Just change the namespace
using Coordix.Interfaces;

public class GetUserQuery : IRequest<UserDto> { }
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

### Pattern 2: Commands

**Before:**
```csharp
public class CreateUserCommand : IRequest { }
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

**After:**
```csharp
// Same code! Just change the namespace
using Coordix.Interfaces;

public class CreateUserCommand : IRequest { }
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

### Pattern 3: Notifications

**Before:**
```csharp
public class UserCreatedEvent : INotification { }
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

**After:**
```csharp
// Same code! Just change the namespace
using Coordix.Interfaces;

public class UserCreatedEvent : INotification { }
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Handler logic
    }
}
```

## Handling Advanced Features

### Pipeline Behaviors

If you were using pipeline behaviors in MediatR, you have a few options:

**Option 1: Move to Handlers**
```csharp
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    private readonly IValidator<MyRequest> _validator;
    private readonly ILogger<MyHandler> _logger;

    public async Task<MyResponse> Handle(MyRequest request, CancellationToken ct)
    {
        // Validation (was in pipeline behavior)
        await _validator.ValidateAsync(request, ct);

        // Logging (was in pipeline behavior)
        _logger.LogInformation("Handling request");

        // Handler logic
        return result;
    }
}
```

**Option 2: Use Decorators**
```csharp
public class ValidatingHandlerDecorator<TRequest, TResponse> 
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _handler;
    private readonly IValidator<TRequest> _validator;

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct)
    {
        await _validator.ValidateAsync(request, ct);
        return await _handler.Handle(request, ct);
    }
}
```

### Request Pre/Post Processors

Handle in your handlers or use decorators (see above).

### Publish Strategies

Coordix always runs notification handlers in parallel. If you need sequential execution, you can:

**Option 1: Chain Handlers**
```csharp
public class FirstHandler : INotificationHandler<MyEvent>
{
    private readonly IMediator _mediator;

    public async Task Handle(MyEvent notification, CancellationToken ct)
    {
        // Do work
        await _mediator.Publish(new NextEvent(), ct);
    }
}
```

**Option 2: Use Decorators**
Create a decorator that ensures sequential execution.

## Testing Migration

After migrating, make sure to:

1. **Run All Tests**
   ```bash
   dotnet test
   ```

2. **Test Handler Registration**
   ```csharp
   var mediator = serviceProvider.GetRequiredService<IMediator>();
   var response = await mediator.Send(new MyRequest());
   ```

3. **Test Notifications**
   ```csharp
   await mediator.Publish(new MyEvent());
   ```

## Rollback Plan

If you need to rollback:

1. **Restore Previous Package**
   ```bash
   dotnet remove package Coordix
   dotnet add package MediatR
   ```

2. **Revert Namespace Changes**
   ```csharp
   using MediatR; // Instead of Coordix.Interfaces
   ```

3. **Revert Registration**
   ```csharp
   services.AddMediatR(typeof(MyHandler).Assembly);
   ```

## Getting Help

If you encounter issues during migration:

1. Check the [FAQ](./faq.md)
2. Review the [Usage Guide](./usage.md)
3. Open an issue on GitHub

## Next Steps

- Read the [Getting Started Guide](./getting-started.md)
- Check [Best Practices](./best-practices.md)
- Explore the [Examples](../../samples)

