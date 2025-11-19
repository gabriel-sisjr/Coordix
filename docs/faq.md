# Frequently Asked Questions (FAQ)

Common questions and answers about Coordix.

## General Questions

### What is Coordix?

Coordix is a lightweight, high-performance mediator pattern implementation for .NET applications. It helps decouple your application components by implementing the mediator pattern.

### Why should I use Coordix?

- **Zero external dependencies** (except Microsoft.Extensions.DependencyInjection)
- **High performance** with cached delegates and minimal reflection
- **Easy to use** with automatic handler discovery
- **DDD-friendly** design that keeps your domain model clean
- **Built for .NET Standard 2.1** for maximum compatibility

### What's the difference between Coordix and MediatR?

Coordix is similar to MediatR but with some key differences:
- **Zero dependencies**: Coordix has no external dependencies (MediatR has dependencies)
- **Performance**: Coordix uses compiled delegates for better performance
- **Automatic registration**: Coordix can automatically discover and register handlers
- **Lightweight**: Smaller footprint and simpler implementation

### Is Coordix production-ready?

Yes! Coordix is designed for production use with:
- Thread-safe operations
- Performance optimizations
- Comprehensive error handling
- Full test coverage

## Installation and Setup

### How do I install Coordix?

```bash
dotnet add package Coordix
```

### How do I register Coordix?

```csharp
using Coordix.Extensions;

services.AddCoordix();
```

### Can I use Coordix in a console application?

Yes! Coordix works in any .NET application that supports dependency injection:

```csharp
var services = new ServiceCollection();
services.AddCoordix();
var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
```

### Can I use Coordix in a class library?

Yes! You can use Coordix in class libraries. Just make sure to register it in the consuming application.

## Usage Questions

### How do I send a request?

```csharp
var response = await mediator.Send(new MyRequest { /* properties */ });
```

### How do I send a command (no response)?

```csharp
await mediator.Send(new MyCommand { /* properties */ });
```

### How do I publish an event?

```csharp
await mediator.Publish(new MyEvent { /* properties */ });
```

### Can I have multiple handlers for the same request?

No. Requests can only have one handler. If you need multiple handlers, use notifications (events).

### Can I have multiple handlers for the same notification?

Yes! All registered handlers for a notification will be called:

```csharp
// All three handlers will be called
await mediator.Publish(new UserCreatedEvent(userId, email));
```

### How do I handle errors in handlers?

Throw exceptions from your handlers:

```csharp
public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(request.UserId, ct);
    if (user == null)
    {
        throw new NotFoundException($"User {request.UserId} not found");
    }
    return MapToDto(user);
}
```

### How do I pass cancellation tokens?

Always pass cancellation tokens through the chain:

```csharp
// In controller
var result = await mediator.Send(request, cancellationToken);

// In handler
public async Task<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
{
    await _repository.GetAsync(cancellationToken);
}
```

## Handler Registration

### How does automatic registration work?

`AddCoordix()` scans all assemblies in the current AppDomain for classes that implement:
- `IRequestHandler<TRequest, TResponse>`
- `IRequestHandler<TRequest>`
- `INotificationHandler<TNotification>`

It then registers them as transient services.

### Can I filter which assemblies are scanned?

Yes! You can specify assemblies or namespace prefixes:

```csharp
// Scan specific assembly
services.AddCoordix(typeof(MyHandler).Assembly);

// Scan by namespace prefix
services.AddCoordix("MyApp");
```

### Can I register handlers manually?

Yes! You can register handlers manually:

```csharp
services.AddSingleton<IMediator, Mediator>();
services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyHandler>();
```

### What's the default service lifetime for handlers?

Handlers are registered as **Transient** by default. You can override this by registering manually.

### Can I use different service lifetimes for different handlers?

Yes! Register them manually with the desired lifetime:

```csharp
services.AddSingleton<IRequestHandler<MyRequest, MyResponse>, MyHandler>();
services.AddScoped<INotificationHandler<MyEvent>, MyEventHandler>();
```

## Performance Questions

### Is Coordix fast?

Yes! Coordix is optimized for performance:
- First call: ~1-5ms (one-time reflection and compilation)
- Subsequent calls: <0.01ms (near-native performance)

### How does Coordix achieve good performance?

- **Cached MethodInfo**: Reflection is performed only once per handler type
- **Compiled Delegates**: Uses Expression Trees to create strongly-typed delegates
- **Thread-Safe Caching**: All caches use `ConcurrentDictionary`

### Should I warm up handlers?

It's not necessary, but you can if you want to avoid the first-call overhead:

```csharp
// At startup
await mediator.Send(new MyRequest { /* dummy data */ });
```

### Does Coordix support async/await?

Yes! All methods return `Task` or `Task<T>` and support async/await.

## Advanced Questions

### Can I chain requests?

Yes! You can send requests from within handlers:

```csharp
public async Task<MyResponse> Handle(MyRequest request, CancellationToken ct)
{
    var user = await _mediator.Send(new GetUserQuery { UserId = request.UserId }, ct);
    // Use user...
}
```

### Can I publish events from handlers?

Yes! This is a common pattern:

```csharp
public async Task<int> Handle(CreateUserCommand request, CancellationToken ct)
{
    var userId = await _repository.AddAsync(user, ct);
    await _mediator.Publish(new UserCreatedEvent(userId, user.Email), ct);
    return userId;
}
```

### Can I use Coordix with CQRS?

Yes! Coordix is perfect for CQRS:
- Commands: Use `IRequest` (no response)
- Queries: Use `IRequest<TResponse>`
- Events: Use `INotification`

### Can I use Coordix with Domain-Driven Design (DDD)?

Yes! Coordix is DDD-friendly:
- Domain events can be plain classes implementing `INotification`
- No framework dependencies in your domain layer
- Clean separation of concerns

## Troubleshooting

### I get "Handler not found" exception

Make sure:
1. The handler is registered (either automatically or manually)
2. The handler implements the correct interface
3. The request/response types match

### Handlers aren't being discovered automatically

Check:
1. The assembly containing handlers is loaded
2. Handlers are in the correct namespace (if using namespace filtering)
3. Handlers implement the correct interface

### Performance is slow

Check:
1. Are you using async/await correctly?
2. Are handlers doing blocking I/O?
3. Are dependencies slow?
4. Consider using appropriate service lifetimes

### I'm getting dependency injection errors

Make sure:
1. All handler dependencies are registered
2. Service lifetimes are compatible (e.g., don't inject scoped services into singleton handlers)

## Migration Questions

### Can I migrate from MediatR?

Yes! Coordix provides an `AddMediator()` alias for compatibility. See the [Migration Guide](./migration.md) for details.

### Will my existing code work with Coordix?

Most code should work with minimal changes. The main differences are:
- Registration method (`AddCoordix()` instead of `AddMediatR()`)
- Some advanced features might not be available

## Support

### Where can I get help?

- GitHub Issues: [https://github.com/gabriel-sisjr/coordix/issues](https://github.com/gabriel-sisjr/coordix/issues)
- Documentation: Check the [docs](./) folder

### How do I report a bug?

Open an issue on GitHub with:
- Description of the problem
- Steps to reproduce
- Expected behavior
- Actual behavior
- Code sample (if possible)

### Can I contribute?

Yes! Contributions are welcome. Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Next Steps

- Read the [Getting Started Guide](./getting-started.md)
- Check the [Usage Guide](./usage.md) for examples
- Explore the [Examples](../samples) folder

