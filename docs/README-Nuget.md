# Coordix

A lightweight and straightforward mediator implementation for .NET applications with minimal setup.

[![Coordix NuGet Version](https://img.shields.io/nuget/vpre/Coordix.svg)](https://www.nuget.org/packages/coordix)  
[![Coordix NuGet Downloads](https://img.shields.io/nuget/dt/Coordix.svg)](https://codecov.io/gh/gabriel-sisjr/coordix)

## Key Features

- **Built to allow maximum compatibility** - Built with .NET Standard 2.1 bringing the max compatibility
- **Zero external dependencies** - Completely standalone with no third-party dependencies
- **High-performance design** - Optimized for performance with cached delegates and minimal reflection overhead
- **DDD-friendly design** - Support for plain domain events without library dependencies, keeping your domain model clean
- **Dependency Injection Native** - Created from scratch to be used with Microsoft Dependency Injection
- **Comprehensive messaging types**:

  - `IRequest` / `IRequest<TResponse>` - For state-changing and retrieval operations
  - `INotification` - For notifications and event-driven architecture

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
- **First invocation**: Creates and caches the delegate (one-time overhead)
- **Subsequent invocations**: Direct delegate calls with near-native performance
- **Scalability**: Performance improvements become more significant as handler invocation frequency increases

These optimizations ensure that Coordix maintains excellent performance even in high-throughput scenarios while remaining lightweight and easy to use.

## Getting Started

### Installation

You can install the Coordix package via NuGet Package Manager or the .NET CLI:

```bash
dotnet add package Coordix
```

### Simple Usage: Request

This example demonstrates how to use a `Request` (command/query) in a real-world use case.

#### 1. Define the Request

```csharp
public class YourExampleCommand : IRequest<string>
{
    public Guid GuidId { get; set; }
}
```

#### 2. Implement the Handlers

```csharp
public class YourExampleHandler : IRequestHandler<YourExampleCommand, string>
{
    private readonly IMediator _mediator;

    public YourExampleHandler(IMediator mediator) => _mediator = mediator;

    public async Task<string> Handle(YourExampleCommand request, CancellationToken cancellationToken)
    {
        // Do all verifications, persistences and etc.
        // ...

        return $"The request with ID: '{request.GuidId}' was processed successfully.";
    }
}
```

---

### Advanced Usage: Request + Notification

This example demonstrates how to combine a `Request` (command/query) and a `Notification` (event) in a real-world use case.

> #### ✅ Using the previous example.

#### 1. Define the request

```csharp
public class YourExampleCommand : IRequest<string>
{
    public Guid GuidId { get; set; }
}

public class YourExampleEvent : INotification
{
    public Guid ExampleId { get; }

    public YourExampleEvent(Guid ExampleId)
    {
        ExampleId = ExampleId;
    }
}
```

#### 2. Implement the Handlers

```csharp
public class YourExampleHandler : IRequestHandler<YourExampleCommand, string>
{
    private readonly IMediator _mediator;

    public YourExampleHandler(IMediator mediator) => _mediator = mediator;

    public async Task<string> Handle(YourExampleCommand request, CancellationToken cancellationToken)
    {
        var idRequest = request.GuidId;
        // Do all verifications, persistences and etc.
        // ...

        // Publish the Event
        await _mediator.Publish(new YourExampleEvent(idRequest), cancellationToken);

        return $"The request with ID: '{idRequest}' was processed successfully.";
    }
}

public class ExampleEmailHandler : INotificationHandler<YourExampleEvent>
{
    public Task Handle(YourExampleEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending email to ID: {notification.ExampleId}");
        return Task.CompletedTask;
    }
}
```

### P.S. After select your approach, you will need to register the Handlers (Dependency Injection)

You can register everything manually:

```csharp
services.AddSingleton<IMediator, Mediator>();

services.AddScoped<IRequestHandler<YourExampleCommand, Guid>, YourExampleHandler>(); // or Transient.
services.AddTransient<INotificationHandler<YourExampleEvent>, ExampleEmailHandler>(); // or Scoped
```

Or with:

```csharp
services.AddCoordix();
```

#### _**Note: If you are already a user of `Mediator`, you just will need to replace their lib for our, `Coordix` provides the following register:**_

```csharp
services.AddMediator();
```

---

### After all steps before, now is time to execute the Flow

```csharp
public class AppService
{
    private readonly IMediator _mediator;

    public AppService(IMediator mediator) => _mediator = mediator;

    public async Task<string> YourExample()
        => await _mediator.Send(new YourExampleCommand { GuidId = Guid.NewGuid() });
}
```

## Documentation

For comprehensive documentation, including detailed explanations, advanced features, and best practices, please visit the [Wiki](#) - _(WIP)_.

## Give a Star! ⭐

If this project made your life easier, a star would mean a lot to us!

## Examples

Check out the [`/examples`](./examples) folder for more projects that illustrate how to use Coordix.

These include:

- ✅ Basic and Advanced usage with `Send` and `Publish`
- ✅ Manual and automatic registration of handlers

Don't hesitate to experiment — run the examples to see the mediator in action.

## About

Coordix was developed by [Gabriel Santana](https://https://www.linkedin.com/in/gabriel-sisjr/) under the MIT license.
