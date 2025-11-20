# Coordix

A lightweight and straightforward mediator implementation for .NET applications with minimal setup.

[![Coordix NuGet Version](https://img.shields.io/nuget/vpre/Coordix.svg)](https://www.nuget.org/packages/coordix)  
[![Coordix NuGet Downloads](https://img.shields.io/nuget/dt/Coordix.svg)](https://www.nuget.org/packages/coordix)

## Key Features

- **Built for maximum compatibility** - Built with .NET Standard 2.1 for maximum compatibility across .NET platforms
- **Zero external dependencies** - Completely standalone with no third-party dependencies (except Microsoft.Extensions.DependencyInjection)
- **High-performance design** - Optimized for performance with cached delegates and minimal reflection overhead
- **DDD-friendly design** - Support for plain domain events without library dependencies, keeping your domain model clean
- **Dependency Injection Native** - Built from scratch to work seamlessly with Microsoft Dependency Injection
- **Comprehensive messaging types**:
  - `IRequest<TResponse>` - For queries and commands that return a value
  - `IRequest` - For commands that don't return a value
  - `INotification` - For events and notifications
- **Automatic handler discovery** - Automatically registers all handlers in your assemblies
- **Thread-safe** - All operations are thread-safe and optimized for concurrent access

## Performance Optimizations

Coordix is designed with performance in mind, implementing several optimization strategies:

### Cached MethodInfo
- **MethodInfo caching**: The `Handle` method's `MethodInfo` is cached per handler type using a `ConcurrentDictionary`
- **One-time reflection**: `GetMethod("Handle")` is called only once per handler type, eliminating repeated reflection overhead
- **Thread-safe**: All caches use `ConcurrentDictionary` for safe concurrent access

### Compiled Delegates
- **Strongly-typed delegates**: Instead of using `MethodInfo.Invoke`, Coordix uses compiled Expression Trees to create strongly-typed delegates
- **Direct invocation**: Handlers are invoked through pre-compiled delegates, avoiding reflection overhead on every call
- **Type-specific caching**: Separate delegate caches for:
  - Request handlers without return value
  - Request handlers with return value
  - Notification handlers

### Performance Benefits
- **First invocation**: Creates and caches the delegate (one-time overhead ~1-5ms)
- **Subsequent invocations**: Direct delegate calls with near-native performance (<0.01ms)
- **Scalability**: Performance improvements become more significant as handler invocation frequency increases

## Getting Started

### Installation

You can install the Coordix package via NuGet Package Manager or the .NET CLI:

```bash
dotnet add package Coordix
```

### Quick Start

#### 1. Register Coordix

```csharp
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Coordix and automatically discover all handlers
builder.Services.AddCoordix();
```

#### 2. Define a Request

```csharp
using Coordix.Interfaces;

public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

#### 3. Implement a Handler

```csharp
using Coordix.Interfaces;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        return new UserDto { Id = user.Id, Name = user.Name };
    }
}
```

#### 4. Use the Mediator

```csharp
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _mediator.Send(new GetUserQuery { UserId = id });
        return Ok(user);
    }
}
```

## Examples

### Simple Request/Response

```csharp
// Request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

// Handler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Your logic here
        return new UserDto { Id = request.UserId, Name = "John" };
    }
}

// Usage
var user = await _mediator.Send(new GetUserQuery { UserId = 123 });
```

### Command (No Response)

```csharp
// Command
public class CreateUserCommand : IRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Handler
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Your logic here
        await SaveUserAsync(request);
    }
}

// Usage
await _mediator.Send(new CreateUserCommand { Name = "John", Email = "john@example.com" });
```

### Notifications (Events)

```csharp
// Notification
public class UserCreatedEvent : INotification
{
    public int UserId { get; }
    public string Email { get; }

    public UserCreatedEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

// Handler
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Send welcome email
        await SendEmailAsync(notification.Email, "Welcome!");
    }
}

// Usage
await _mediator.Publish(new UserCreatedEvent(userId, email));
```

## Registration Options

### Automatic Registration (Recommended)

```csharp
// Scan all assemblies in the current AppDomain
services.AddCoordix();

// Scan specific assemblies
services.AddCoordix(typeof(MyHandler).Assembly);

// Scan by namespace prefix
services.AddCoordix("MyApp");
```

### Manual Registration

```csharp
services.AddSingleton<IMediator, Mediator>();
services.AddScoped<IRequestHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();
services.AddTransient<INotificationHandler<UserCreatedEvent>, SendWelcomeEmailHandler>();
```

### Compatibility Alias

If you're migrating from MediatR, you can use the `AddMediator` alias:

```csharp
services.AddMediator(); // Same as AddCoordix()
```

## Extension Packages

Coordix has extension packages that add additional functionality:

### Coordix.Background

[![Coordix.Background NuGet Version](https://img.shields.io/nuget/vpre/Coordix.Background.svg)](https://www.nuget.org/packages/Coordix.Background)

Background job processing for fire-and-forget in-process jobs:

```bash
dotnet add package Coordix.Background
```

```csharp
services.AddCoordix();           // Core mediator (required)
services.AddCoordixBackground(); // Background jobs extension
```

See the [Background Jobs Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/background/background-jobs.md) for more information.

## Documentation

For comprehensive documentation, visit the [GitHub repository](https://github.com/gabriel-sisjr/coordix):

### Core Documentation

- 📖 [Installation Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/installation.md)
- 🚀 [Getting Started Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/getting-started.md)
- 📚 [Usage Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/usage.md)
- ⚡ [Performance Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/performance.md)
- 🔧 [API Reference](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/api-reference.md)
- 🎯 [Best Practices](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/best-practices.md)
- 🔄 [Migration Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/migration.md)
- ❓ [FAQ](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/faq.md)

### Extension Packages

- 🔄 [Background Jobs Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/background/background-jobs.md) - Coordix.Background package

## Examples

Check out the [samples folder](https://github.com/gabriel-sisjr/coordix/tree/main/samples) for complete, runnable examples:

- ✅ [Simple Sample](https://github.com/gabriel-sisjr/coordix/tree/main/samples/SimpleSample) - Basic usage with `Send` and `Publish`
- ✅ [Advanced Sample](https://github.com/gabriel-sisjr/coordix/tree/main/samples/AdvancedSample) - Complete application with multiple handlers, events, and patterns

## Requirements

- .NET Standard 2.1 or higher
- .NET Core 2.1+ / .NET 5+ / .NET 6+ / .NET 7+ / .NET 8+
- Microsoft.Extensions.DependencyInjection (included with ASP.NET Core)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/gabriel-sisjr/coordix/blob/main/LICENSE) file for details.

## About

Coordix was developed by [Gabriel Santana](https://www.linkedin.com/in/gabriel-sisjr/) under the MIT license.

## Give a Star! ⭐

If this project made your life easier, a star would mean a lot to us!

---

Made with ❤️ by the Coordix community
