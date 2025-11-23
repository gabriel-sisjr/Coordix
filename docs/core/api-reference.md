# API Reference

Complete API documentation for Coordix.

## Namespaces

- `Coordix.Interfaces` - Core interfaces
- `Coordix.Implementation` - Mediator and handler executor implementations
- `Coordix.Extensions` - Extension methods for dependency injection
- `Coordix` - Configuration options and enums

## Core Interfaces

### IMediator

The main interface for sending requests and publishing notifications.

#### Methods

##### Send<TResponse>(IRequest<TResponse>, CancellationToken)

Sends a request to a single handler and returns its response.

```csharp
Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
```

**Parameters:**
- `request`: The request message to send
- `cancellationToken`: Optional cancellation token

**Returns:** Task containing the handler's response

**Exceptions:**
- `InvalidOperationException`: Thrown if no handler is found for the request type

**Example:**
```csharp
var response = await mediator.Send(new GetUserQuery { UserId = 123 });
```

##### Send(IRequest, CancellationToken)

Sends a request to a single handler without expecting a response.

```csharp
Task Send(IRequest request, CancellationToken cancellationToken = default);
```

**Parameters:**
- `request`: The request message to send
- `cancellationToken`: Optional cancellation token

**Returns:** Task representing the asynchronous operation

**Exceptions:**
- `InvalidOperationException`: Thrown if no handler is found for the request type

**Example:**
```csharp
await mediator.Send(new CreateUserCommand { Name = "John" });
```

##### Publish<TNotification>(TNotification, CancellationToken)

Publishes a notification to all registered handlers.

```csharp
Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
    where TNotification : INotification;
```

**Parameters:**
- `notification`: The notification message to publish
- `cancellationToken`: Optional cancellation token

**Returns:** Task representing the asynchronous publish operation

**Example:**
```csharp
await mediator.Publish(new UserCreatedEvent(userId, email));
```

### IRequest<TResponse>

Marker interface for requests that return a response.

```csharp
public interface IRequest<TResponse> { }
```

**Example:**
```csharp
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}
```

### IRequest

Marker interface for requests that don't return a response.

```csharp
public interface IRequest { }
```

**Example:**
```csharp
public class CreateUserCommand : IRequest
{
    public string Name { get; set; }
}
```

### INotification

Marker interface for notifications (events).

```csharp
public interface INotification { }
```

**Example:**
```csharp
public class UserCreatedEvent : INotification
{
    public int UserId { get; }
    public string Email { get; }
}
```

### IRequestHandler<TRequest, TResponse>

Interface for handling requests that return a response.

```csharp
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

**Example:**
```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Handler implementation
    }
}
```

### IRequestHandler<TRequest>

Interface for handling requests that don't return a response.

```csharp
public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}
```

**Example:**
```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Handler implementation
    }
}
```

### INotificationHandler<TNotification>

Interface for handling notifications.

```csharp
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

**Example:**
```csharp
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Handler implementation
    }
}
```

### IHandlerExecutor

Centralized handler execution abstraction that handles handler lookup, caching, and invocation. This interface ensures that all handler execution logic is centralized in a single place, eliminating scattered reflection throughout the codebase.

```csharp
public interface IHandlerExecutor
{
    Task<TResponse> ExecuteRequestHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task ExecuteRequestHandler(IRequest request, CancellationToken cancellationToken = default);
    Task ExecuteNotificationHandler<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

**Purpose:**
- Centralizes all handler execution logic
- Provides a single point for handler lookup, caching, and invocation
- Allows different implementations (reflection-based, code-generated, etc.)
- Used by both `IMediator` and background job processing

**Note:** This interface is typically not used directly by application code. It's used internally by `IMediator` and `BackgroundWorker`.

## Implementation

### Mediator

The default implementation of `IMediator`. This implementation delegates handler execution to `IHandlerExecutor` (the registry), which centralizes all reflection and caching logic.

```csharp
public class Mediator : IMediator
{
    public Mediator(IHandlerExecutor handlerExecutor);
    
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    public Task Send(IRequest request, CancellationToken cancellationToken = default);
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

**Constructor:**
- `handlerExecutor`: The handler executor (registry) used to execute handlers

**Architecture:**
- `Mediator` discovers the request/notification type and delegates execution to `IHandlerExecutor`
- All handler resolution, caching, and invocation logic is centralized in `IHandlerExecutor`
- This design eliminates scattered reflection and allows different execution strategies (reflection, code generation, etc.)

### HandlerExecutor

The default reflection-based implementation of `IHandlerExecutor`. This class centralizes all handler execution logic including reflection, caching, and invocation.

```csharp
public class HandlerExecutor : IHandlerExecutor
{
    public HandlerExecutor(IServiceProvider provider);
    
    public Task<TResponse> ExecuteRequestHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    public Task ExecuteRequestHandler(IRequest request, CancellationToken cancellationToken = default);
    public Task ExecuteNotificationHandler<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

**Performance Optimizations:**
- Caches `MethodInfo` objects to avoid repeated reflection
- Uses compiled delegates (Expression Trees) for near-native invocation performance
- Thread-safe caching using `ConcurrentDictionary`
- All reflection is performed only once per handler type

## Extension Methods

### ServiceCollectionExtensions

Extension methods for registering Coordix services.

#### AddCoordix(IServiceCollection)

Registers Coordix with default settings (Reflection mode).

```csharp
public static IServiceCollection AddCoordix(this IServiceCollection services);
```

**Example:**
```csharp
services.AddCoordix();
```

#### AddCoordix(IServiceCollection, params object[])

Registers Coordix and scans assemblies for handler implementations.

```csharp
public static IServiceCollection AddCoordix(this IServiceCollection services, params object[] args);
```

**Parameters:**
- `services`: The service collection to add services to
- `args`: Optional parameters:
  - No arguments: Scans all assemblies in the current AppDomain
  - `Assembly[]`: Scans the specified assemblies
  - `string[]`: Scans assemblies whose FullName starts with any of the prefixes

**Returns:** The same service collection instance for chaining

**Example:**
```csharp
// Scan all assemblies
services.AddCoordix();

// Scan specific assembly
services.AddCoordix(typeof(MyHandler).Assembly);

// Scan by namespace prefix
services.AddCoordix("MyApp");
```

#### AddCoordix(IServiceCollection, Action<CoordixOptions>)

Registers Coordix with configuration options.

```csharp
public static IServiceCollection AddCoordix(
    this IServiceCollection services,
    Action<CoordixOptions>? configureOptions);
```

**Parameters:**
- `services`: The service collection to add services to
- `configureOptions`: Optional action to configure `CoordixOptions`

**Returns:** The same service collection instance for chaining

**Example:**
```csharp
// Reflection mode (default)
services.AddCoordix();

// Or explicitly configure
services.AddCoordix(options =>
{
    options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
});
```

> **Note**: For CodeGen mode, use `AddCoordixWithCodeGen()` from `Coordix.CodeGen.Extensions` instead.

#### AddCoordix(IServiceCollection, Action<CoordixOptions>, params object[])

Registers Coordix with configuration options and assembly scanning parameters.

```csharp
public static IServiceCollection AddCoordix(
    this IServiceCollection services,
    Action<CoordixOptions>? configureOptions,
    params object[] args);
```

**Example:**
```csharp
services.AddCoordix(
    options => options.HandlerResolutionMode = HandlerResolutionMode.Reflection,
    typeof(MyHandler).Assembly);
```

#### AddMediator(IServiceCollection)

Alias for `AddCoordix()`. Provided for compatibility with other mediator libraries.

```csharp
public static IServiceCollection AddMediator(this IServiceCollection services);
```

**Example:**
```csharp
services.AddMediator(); // Same as AddCoordix()
```

## Configuration

### CoordixOptions

Configuration options for Coordix mediator services.

```csharp
public class CoordixOptions
{
    public HandlerResolutionMode HandlerResolutionMode { get; set; } = HandlerResolutionMode.Reflection;
}
```

**Properties:**
- `HandlerResolutionMode`: Gets or sets the handler resolution mode. Defaults to `Reflection`.

**Example:**
```csharp
services.AddCoordix(options =>
{
    options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
});
```

### HandlerResolutionMode

Defines the mode used for handler resolution and execution.

```csharp
public enum HandlerResolutionMode
{
    Reflection = 0,        // Uses reflection-based handler resolution (default)
    CodeGenPreferred = 1  // Uses code generation (requires Coordix.CodeGen package)
}
```

**Values:**
- `Reflection`: Uses reflection-based handler resolution and execution. This is the default mode and provides good performance with cached delegates.
- `CodeGenPreferred`: Uses code generation for handler resolution and execution. This mode requires the `Coordix.CodeGen` package to be installed and its registration method to be called.

## Exceptions

### InvalidOperationException

Thrown when:
- No handler is found for a request type
- The Handle method is not found on a handler type
- `HandlerResolutionMode.CodeGenPreferred` is selected without the `Coordix.CodeGen` package

**Example:**
```csharp
try
{
    await mediator.Send(new MyRequest());
}
catch (InvalidOperationException ex)
{
    // Handler not found
    Console.WriteLine(ex.Message);
}

// CodeGen mode - use AddCoordixWithCodeGen instead
using Coordix.CodeGen.Extensions;

// Correct way to use CodeGen mode
services.AddCoordixWithCodeGen();

// Wrong way (will throw exception)
try
{
    services.AddCoordix(options =>
    {
        options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
    });
}
catch (InvalidOperationException ex)
{
    // Exception: Use AddCoordixWithCodeGen() instead
    Console.WriteLine(ex.Message);
}
```

### ArgumentException

Thrown when invalid parameters are passed to `AddCoordix` or `AddMediator`:
- Mixed types (neither all `Assembly` nor all `string`)

**Example:**
```csharp
try
{
    services.AddCoordix(Assembly.GetExecutingAssembly(), "MyApp"); // Mixed types!
}
catch (ArgumentException ex)
{
    // Invalid parameters
}
```

## Type Constraints

### Generic Constraints

- `IRequest<TResponse>`: No constraints
- `IRequest`: No constraints
- `INotification`: No constraints
- `IRequestHandler<TRequest, TResponse>`: `TRequest : IRequest<TResponse>`
- `IRequestHandler<TRequest>`: `TRequest : IRequest`
- `INotificationHandler<TNotification>`: `TNotification : INotification`

## Thread Safety

All public APIs are thread-safe:
- `IMediator` methods can be called concurrently
- Internal caches use `ConcurrentDictionary` for thread-safe access
- Handler instances are resolved per call (unless registered as singleton)

## Next Steps

- Read the [Usage Guide](./usage.md) for examples
- Check [Best Practices](./best-practices.md) for recommended patterns
- Explore the [Examples](../../samples) for complete implementations

