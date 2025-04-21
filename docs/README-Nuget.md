# Coordix

A lightweight and straightforward mediator implementation for .NET applications with minimal setup.

[![Coordix NuGet Version](https://img.shields.io/nuget/vpre/Coordix.svg)](https://www.nuget.org/packages/coordix)  
[![Coordix NuGet Downloads](https://img.shields.io/nuget/dt/Coordix.svg)](https://codecov.io/gh/gabriel-sisjr/coordix)

## Key Features

- **Built to allow maximum compatibility** - Built with .NET Standard 2.1 briging the max compatibility
- **Zero external dependencies** - Completely standalone with no third-party dependencies
- **Reduced reflection usage** - Optimized for performance with minimal reflection
- **DDD-friendly design** - Support for plain domain events without library dependencies, keeping your domain model clean
- **Dependency Injection Native** - Created from scratch to be used with Microsoft Dependency Injection
- **Comprehensive messaging types**:

  - `IRequest` / `IRequest<TResponse>` - For state-changing and retrieval operations
  - `INotification` - For notifications and event-driven architecture

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
