# Usage Guide

This guide covers advanced usage patterns and common scenarios with Coordix.

## Table of Contents

- [Request/Response Pattern](#requestresponse-pattern)
- [Commands (No Response)](#commands-no-response)
- [Notifications (Events)](#notifications-events)
- [Handler Registration](#handler-registration)
- [Dependency Injection in Handlers](#dependency-injection-in-handlers)
- [Cancellation Tokens](#cancellation-tokens)
- [Error Handling](#error-handling)
- [Testing](#testing)
- [Background Jobs with Coordix.Background](#background-jobs-with-coordixbackground)
- [Advanced Patterns](#advanced-patterns)

## Request/Response Pattern

### Basic Request with Response

```csharp
// Request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

// Handler
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

// Usage
var user = await _mediator.Send(new GetUserQuery { UserId = 123 });
```

### Complex Response Types

```csharp
public class SearchUsersQuery : IRequest<SearchResult<UserDto>>
{
    public string SearchTerm { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

## Commands (No Response)

Commands are used for operations that change state but don't return a value.

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
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User 
        { 
            Name = request.Name, 
            Email = request.Email 
        };
        await _repository.AddAsync(user, cancellationToken);
    }
}

// Usage
await _mediator.Send(new CreateUserCommand 
{ 
    Name = "John", 
    Email = "john@example.com" 
});
```

## Notifications (Events)

Notifications allow multiple handlers to respond to a single event.

### Single Handler

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
    private readonly IEmailService _emailService;

    public SendWelcomeEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, cancellationToken);
    }
}

// Usage
await _mediator.Publish(new UserCreatedEvent(userId, email));
```

### Multiple Handlers

You can have multiple handlers for the same notification:

```csharp
// Handler 1: Send Email
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Send email
    }
}

// Handler 2: Log Event
public class LogUserCreatedHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<LogUserCreatedHandler> _logger;

    public LogUserCreatedHandler(ILogger<LogUserCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} created", notification.UserId);
        return Task.CompletedTask;
    }
}

// Handler 3: Update Cache
public class UpdateUserCacheHandler : INotificationHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Update cache
    }
}

// All three handlers will be called when you publish:
await _mediator.Publish(new UserCreatedEvent(userId, email));
```

## Handler Registration

### Automatic Registration

```csharp
// Scans all assemblies in the current AppDomain (uses Reflection mode by default)
services.AddCoordix();

// Scan specific assemblies
services.AddCoordix(typeof(MyHandler).Assembly);

// Scan by namespace prefix
services.AddCoordix("MyApp");
```

### Manual Registration

```csharp
// Register Coordix with options
services.AddCoordix(options =>
{
    options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
});

// Register individual handlers
services.AddScoped<IRequestHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();
services.AddTransient<IRequestHandler<CreateUserCommand>, CreateUserCommandHandler>();
services.AddScoped<INotificationHandler<UserCreatedEvent>, SendWelcomeEmailHandler>();
```

> **Note**: When registering manually, you still need to call `AddCoordix()` to register the `IMediator` and `IHandlerExecutor` (registry). The handler registration is separate.

### Custom Service Lifetime

```csharp
// Handlers are registered as Transient by default with AddCoordix()
// You can override by registering manually:

services.AddSingleton<IRequestHandler<MyRequest, MyResponse>, MyHandler>();
services.AddScoped<INotificationHandler<MyEvent>, MyEventHandler>();
```

## Dependency Injection in Handlers

Handlers support full dependency injection:

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IConfiguration _configuration;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<CreateOrderHandler> logger,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for user {UserId}", request.UserId);
        
        var order = new Order { /* ... */ };
        var orderId = await _orderRepository.AddAsync(order, cancellationToken);

        // Publish event
        await _mediator.Publish(new OrderCreatedEvent(orderId), cancellationToken);

        return orderId;
    }
}
```

## Cancellation Tokens

Always pass cancellation tokens through the chain:

```csharp
// In your controller/service
public async Task<ActionResult> GetUser(int id, CancellationToken cancellationToken)
{
    var user = await _mediator.Send(
        new GetUserQuery { UserId = id }, 
        cancellationToken);
    return Ok(user);
}

// In your handler
public async Task<UserDto> Handle(
    GetUserQuery request, 
    CancellationToken cancellationToken)
{
    // Pass cancellationToken to all async operations
    var user = await _repository.GetByIdAsync(
        request.UserId, 
        cancellationToken);
    return MapToDto(user);
}
```

## Error Handling

### Throwing Exceptions

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            throw new NotFoundException($"User with ID {request.UserId} not found");
        }

        return MapToDto(user);
    }
}
```

### Global Exception Handling

```csharp
// In your controller or middleware
try
{
    var result = await _mediator.Send(request);
    return Ok(result);
}
catch (NotFoundException ex)
{
    return NotFound(ex.Message);
}
catch (ValidationException ex)
{
    return BadRequest(ex.Errors);
}
```

## Testing

### Unit Testing Handlers

```csharp
[Fact]
public async Task Handle_ShouldReturnUser_WhenUserExists()
{
    // Arrange
    var repository = new Mock<IUserRepository>();
    var user = new User { Id = 1, Name = "John" };
    repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);

    var handler = new GetUserQueryHandler(repository.Object);
    var query = new GetUserQuery { UserId = 1 };

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("John", result.Name);
}
```

### Integration Testing

```csharp
[Fact]
public async Task Send_ShouldReturnUser_WhenHandlerIsRegistered()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddCoordix();
    services.AddScoped<IUserRepository, UserRepository>();
    
    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new GetUserQuery { UserId = 1 });

    // Assert
    Assert.NotNull(result);
}
```

## Advanced Patterns

### Chaining Requests

```csharp
public class ProcessOrderHandler : IRequestHandler<ProcessOrderCommand>
{
    private readonly IMediator _mediator;

    public ProcessOrderHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(ProcessOrderCommand request, CancellationToken cancellationToken)
    {
        // Send multiple requests
        var user = await _mediator.Send(new GetUserQuery { UserId = request.UserId }, cancellationToken);
        var inventory = await _mediator.Send(new CheckInventoryQuery { ProductId = request.ProductId }, cancellationToken);
        
        // Process order...
    }
}
```

### Conditional Event Publishing

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IMediator mediator, IUserRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { /* ... */ };
        var userId = await _repository.AddAsync(user, cancellationToken);

        // Only publish event if user was created successfully
        if (userId > 0)
        {
            await _mediator.Publish(new UserCreatedEvent(userId, user.Email), cancellationToken);
        }

        return userId;
    }
}
```

### Request Validation

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IValidator<CreateUserCommand> _validator;

    public CreateUserCommandHandler(IValidator<CreateUserCommand> validator)
    {
        _validator = validator;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Process command...
    }
}
```

## Configuration Options

### Handler Resolution Mode

Coordix supports different modes for handler resolution and execution:

**Reflection Mode (Default):**
```csharp
services.AddCoordix(); // Uses Reflection mode by default
```

**CodeGen Mode (Zero Reflection):**
```csharp
using Coordix.CodeGen.Extensions;

// Install: dotnet add package Coordix.CodeGen
services.AddCoordixWithCodeGen(); // Uses code generation for handler resolution
```

**Available Modes:**
- **Reflection** (default): Uses reflection-based handler resolution with cached delegates for optimal performance
- **CodeGen** (zero reflection): Uses compile-time code generation for direct method calls (requires `Coordix.CodeGen` package)

> **Important**: When using CodeGen mode, use `AddCoordixWithCodeGen()` instead of `AddCoordix()`. This automatically registers all core services with CodeGen mode enabled.

## Background Jobs with Coordix.Background

`Coordix.Background` is a separate NuGet package that extends Coordix with fire-and-forget background job processing.

### Installation

```bash
dotnet add package Coordix.Background
```

### Configuration

```csharp
using Coordix.Background.Extensions;

// AddCoordixBackground automatically includes core Coordix services
services.AddCoordixBackground();
```

> **Note**: You do **not** need to call `AddCoordix()` when using `AddCoordixBackground()`. The Background extension registers all core services automatically.

### Enqueueing Background Jobs

#### Enqueue a Request

```csharp
using Coordix.Background.Interfaces;

public class OrderController : ControllerBase
{
    private readonly IBackgroundMediator _backgroundMediator;

    public OrderController(IBackgroundMediator backgroundMediator)
    {
        _backgroundMediator = backgroundMediator;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        // Process synchronously
        var order = await _mediator.Send(new CreateOrderCommand { ... });

        // Enqueue background job (fire-and-forget)
        await _backgroundMediator.Enqueue(new SendOrderConfirmationEmail 
        { 
            OrderId = order.Id 
        });

        return Ok(order);
    }
}
```

#### Enqueue a Notification

```csharp
// Enqueue notification for background processing
await _backgroundMediator.Enqueue(new OrderCreatedNotification 
{ 
    OrderId = "ORD-123",
    CustomerId = "CUST-456"
});
```

### When to Use Background Jobs

Use `IBackgroundMediator` for operations that:
- Don't need immediate feedback
- Can be processed asynchronously
- Shouldn't block the HTTP response
- Are non-critical (logging, emails, notifications)

### Important Notes

- Background jobs use the **same handlers** registered with `AddCoordix()`
- Background jobs use the **same handler execution registry** (`IHandlerExecutor`) as the core mediator
- Each job is processed in its **own service scope** for proper lifetime management
- Jobs are processed **outside the original request context**
- Exceptions in one job **don't stop processing** of other jobs
- Jobs are **in-process only** (lost on application restart)
- Background jobs are **fire-and-forget** - responses from handlers are not available to the caller

For complete documentation, see the [Background Jobs Guide](../background/background-jobs.md).

## Next Steps

- Read [Best Practices](./best-practices.md) for recommended patterns
- Check the [API Reference](./api-reference.md) for complete API documentation
- Explore the [Examples](../../samples) folder for more patterns

