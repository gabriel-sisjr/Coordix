<h1 align="center"><br>
<a href="https://github.com/gabriel-sisjr/coordix">
<img src="assets/logo.png" width="300px">
</a>
</h1>
<h4 align="center">A lightweight and straightforward mediator implementation for .NET applications with minimal setup.</h4>
<p align="center">
<a href="https://www.nuget.org/packages/coordix">
<img src="https://img.shields.io/nuget/vpre/Coordix.svg" alt="Coordix Nuget Version" />
</a>
<a href="https://www.nuget.org/packages/coordix">
<img src="https://img.shields.io/nuget/dt/Coordix.svg" alt="Coordix Nuget Downloads" />
</a>
<a href="https://github.com/gabriel-sisjr/coordix/blob/main/LICENSE">
<img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License: MIT" />
</a>
<a href="https://github.com/gabriel-sisjr/coordix">
<img src="https://img.shields.io/github/stars/gabriel-sisjr/coordix?style=social" alt="GitHub Stars" />
</a>
</p>

## 📋 Table of Contents

- [What is Coordix?](#what-is-coordix)
- [Key Features](#key-features)
- [Why Coordix?](#why-coordix)
- [Getting Started](#getting-started)
- [Quick Examples](#quick-examples)
- [Performance](#performance)
- [Documentation](#documentation)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## What is Coordix?

Coordix is a lightweight, high-performance mediator pattern implementation for .NET applications. It provides a simple way to decouple your application components by implementing the mediator pattern, allowing you to send requests and notifications through a single mediator interface.

### Core Concepts

- **Request/Response**: Send a request to a single handler and receive a response
- **Commands**: Send a command (request without response) to execute an action
- **Queries**: Send a query (request with response) to retrieve data
- **Notifications**: Publish events that can be handled by multiple handlers

## Key Features

- ✅ **Built for maximum compatibility** - Built with .NET Standard 2.1 for maximum compatibility across .NET platforms
- ✅ **Zero external dependencies** - Completely standalone with no third-party dependencies (except Microsoft.Extensions.DependencyInjection)
- ✅ **High-performance design** - Optimized with cached delegates and minimal reflection overhead
- ✅ **DDD-friendly** - Support for plain domain events without library dependencies, keeping your domain model clean
- ✅ **Dependency Injection Native** - Built from scratch to work seamlessly with Microsoft Dependency Injection
- ✅ **Comprehensive messaging types**:
  - `IRequest<TResponse>` - For queries and commands that return a value
  - `IRequest` - For commands that don't return a value
  - `INotification` - For events and notifications
- ✅ **Automatic handler discovery** - Automatically registers all handlers in your assemblies
- ✅ **Manual registration support** - Full control when you need it
- ✅ **Thread-safe** - All operations are thread-safe and optimized for concurrent access

## Why Coordix?

### Compared to Other Mediator Libraries

| Feature                | Coordix | MediatR | Others |
| ---------------------- | ------- | ------- | ------ |
| Zero Dependencies      | ✅      | ❌      | Varies |
| .NET Standard 2.1      | ✅      | ✅      | Varies |
| Performance Optimized  | ✅      | ⚠️      | Varies |
| Automatic Registration | ✅      | ⚠️      | Varies |
| Lightweight            | ✅      | ⚠️      | Varies |

### Use Cases

- **CQRS (Command Query Responsibility Segregation)**: Separate commands and queries
- **Event-Driven Architecture**: Publish and handle domain events
- **Clean Architecture**: Decouple layers and maintain boundaries
- **Microservices**: Communicate between services using messages
- **Domain-Driven Design**: Implement domain events without framework dependencies

## Packages

Coordix is distributed as separate NuGet packages:

### Core Package

**Coordix** - The core mediator implementation

```bash
dotnet add package Coordix
```

### Extension Packages

**Coordix.Background** - Background job processing (fire-and-forget in-process jobs)

```bash
dotnet add package Coordix.Background
```

**Coordix.CodeGen** - Source generator for compile-time handler execution (zero reflection overhead)

```bash
dotnet add package Coordix.CodeGen
```

> **Performance Boost**: The CodeGen package eliminates all runtime reflection by generating handler execution code at compile-time. This results in faster startup times and better runtime performance.

> **Note**: `Coordix.Background` requires `Coordix` to be installed. It will be automatically installed as a dependency.

## Getting Started

### Installation

Install the Coordix core package via NuGet Package Manager:

```bash
dotnet add package Coordix
```

Or via Package Manager Console:

```powershell
Install-Package Coordix
```

For background job processing, also install:

```bash
dotnet add package Coordix.Background
```

### Basic Setup

#### 1. Register Coordix Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Coordix and automatically discover all handlers
builder.Services.AddCoordix();

// Or register manually
builder.Services.AddSingleton<IMediator, Mediator>();
builder.Services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyRequestHandler>();
```

#### 2. Use the Mediator

```csharp
public class MyController : ControllerBase
{
    private readonly IMediator _mediator;

    public MyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<MyResponse>> Create([FromBody] CreateUserCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}
```

## Quick Examples

### Example 1: Simple Request/Response

```csharp
// Define the request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

// Implement the handler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.UserId);
        return new UserDto { Id = user.Id, Name = user.Name };
    }
}

// Use it
var user = await _mediator.Send(new GetUserQuery { UserId = 123 });
```

### Example 2: Command (No Response)

```csharp
// Define the command
public class CreateUserCommand : IRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Implement the handler
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Name = request.Name, Email = request.Email };
        await _repository.AddAsync(user);
    }
}

// Use it
await _mediator.Send(new CreateUserCommand { Name = "John", Email = "john@example.com" });
```

### Example 3: Notifications (Events)

```csharp
// Define the notification
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

// Implement multiple handlers
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Send welcome email
        await SendEmailAsync(notification.Email, "Welcome!");
    }
}

public class LogUserCreatedHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<LogUserCreatedHandler> _logger;

    public LogUserCreatedHandler(ILogger<LogUserCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} created with email {Email}",
            notification.UserId, notification.Email);
        return Task.CompletedTask;
    }
}

// Publish the event
await _mediator.Publish(new UserCreatedEvent(userId, email));
```

### Example 4: Combining Requests and Notifications

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IUserRepository _repository;
    private readonly IMediator _mediator;

    public CreateUserCommandHandler(IUserRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Name = request.Name, Email = request.Email };
        await _repository.AddAsync(user);

        // Publish event after user creation
        await _mediator.Publish(new UserCreatedEvent(user.Id, user.Email), cancellationToken);

        return user.Id;
    }
}
```

## Performance

Coordix is designed with performance in mind. Here's how it achieves high performance:

### Optimization Strategies

1. **Cached MethodInfo**: The `Handle` method's `MethodInfo` is cached per handler type using a `ConcurrentDictionary`
2. **Compiled Delegates**: Uses Expression Trees to create strongly-typed delegates, avoiding reflection overhead
3. **Thread-Safe Caching**: All caches use `ConcurrentDictionary` for safe concurrent access
4. **One-Time Reflection**: Reflection is performed only once per handler type

### Performance Benefits

- **First invocation**: Creates and caches the delegate (one-time overhead)
- **Subsequent invocations**: Direct delegate calls with near-native performance
- **Scalability**: Performance improvements become more significant as handler invocation frequency increases

For detailed performance information, see the [Performance Guide](./docs/performance.md).

### Zero-Reflection CodeGen (Coordix.CodeGen)

For maximum performance, install the `Coordix.CodeGen` package which eliminates **all** runtime reflection by generating handler execution code at compile-time:

```bash
dotnet add package Coordix.CodeGen
```

#### Usage

Replace `AddCoordix()` with `AddCoordixWithCodeGen()`:

```csharp
using Coordix.CodeGen.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Use code-generated executor (zero reflection)
builder.Services.AddCoordixWithCodeGen(typeof(Program).Assembly);

var app = builder.Build();
```

#### How It Works

1. **Compile-Time**: The source generator discovers all handlers during build
2. **Code Generation**: Generates a `GeneratedHandlerExecutor` with direct, strongly-typed calls
3. **Runtime**: Uses the generated executor instead of reflection-based one

#### Performance Impact

- ✅ **Zero runtime reflection** - All handler calls are direct method invocations
- ✅ **Faster startup** - No expression tree compilation at runtime
- ✅ **Better JIT optimization** - Strongly-typed code allows inlining and optimization
- ✅ **Reduced memory** - No cached delegates or MethodInfo instances

#### Example Generated Code

Input (your handler):

```csharp
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken ct)
        => await _repository.GetByIdAsync(request.UserId);
}
```

Generated (by Coordix.CodeGen):

```csharp
public async Task<TResponse> ExecuteRequestHandler<TResponse>(
    IRequest<TResponse> request, CancellationToken ct)
{
    if (request is GetUserRequest typedRequest)
    {
        var handler = _provider.GetRequiredService<IRequestHandler<GetUserRequest, UserDto>>();
        var result = await handler.Handle(typedRequest, ct);
        return (TResponse)(object)result;
    }
    // ... other handlers
}
```

**No reflection, no dynamic invocation - just direct method calls!**

For more details, see the [CodeGen documentation](./samples/CodeGenSample/README.md).

## Documentation

Comprehensive documentation is available in the [`docs`](./docs) folder:

### Core Documentation (Coordix)

- 📖 [Installation Guide](./docs/core/installation.md) - Detailed installation and configuration instructions
- 🚀 [Getting Started Guide](./docs/core/getting-started.md) - Step-by-step tutorial for beginners
- 📚 [Usage Guide](./docs/core/usage.md) - Advanced usage patterns and best practices
- ⚡ [Performance Guide](./docs/core/performance.md) - Performance optimization tips and benchmarks
- 🔧 [API Reference](./docs/core/api-reference.md) - Complete API documentation
- 🎯 [Best Practices](./docs/core/best-practices.md) - Recommended patterns and practices
- 🔄 [Migration Guide](./docs/core/migration.md) - Migrating from other mediator libraries
- ❓ [FAQ](./docs/core/faq.md) - Frequently asked questions

### Extension Packages

#### Coordix.Background

- 🔄 [Background Jobs Guide](./docs/background/background-jobs.md) - Complete guide for fire-and-forget background jobs

## Examples

Check out the [`samples`](./samples) folder for complete, runnable examples:

- ✅ [Simple Sample](./samples/SimpleSample) - Basic usage with `Send` and `Publish`
- ✅ [Advanced Sample](./samples/AdvancedSample) - Complete application with multiple handlers, events, and patterns
- ✅ [Background Jobs Sample](./samples/BackgroundJobsSample) - Fire-and-forget background job processing with `Coordix.Background`

Don't hesitate to experiment — run the examples to see Coordix in action!

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'feat: Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

Please make sure to follow our [commit message conventions](https://github.com/gabriel-sisjr/coordix/blob/main/commitlint.config.js).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## About

Coordix was developed by [Gabriel Santana](https://www.linkedin.com/in/gabriel-sisjr/) under the MIT license.

## Give a Star! ⭐

If this project made your life easier, a star would mean a lot to us!

---

<p align="center">Made with ❤️ by the Coordix community</p>
