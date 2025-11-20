# Best Practices

This guide covers recommended patterns and best practices when using Coordix.

## Table of Contents

- [Naming Conventions](#naming-conventions)
- [Project Structure](#project-structure)
- [Handler Design](#handler-design)
- [Request/Command Design](#requestcommand-design)
- [Notification Design](#notification-design)
- [Error Handling](#error-handling)
- [Testing](#testing)
- [Background Jobs](#background-jobs)
- [Performance](#performance)
- [Security](#security)

## Naming Conventions

### Requests (Queries and Commands)

Use descriptive names that indicate the action:

```csharp
// Good: Clear and descriptive
public class GetUserByIdQuery : IRequest<UserDto> { }
public class CreateUserCommand : IRequest<int> { }
public class UpdateUserCommand : IRequest { }
public class DeleteUserCommand : IRequest { }

// Bad: Vague or unclear
public class UserRequest : IRequest<UserDto> { }
public class DoSomething : IRequest { }
```

### Handlers

Name handlers after their corresponding request/notification:

```csharp
// Good: Matches request name
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto> { }
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int> { }
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent> { }

// Bad: Doesn't match
public class UserHandler : IRequestHandler<GetUserByIdQuery, UserDto> { }
```

### Notifications (Events)

Use past tense for events (they represent something that already happened):

```csharp
// Good: Past tense
public class UserCreatedEvent : INotification { }
public class OrderShippedEvent : INotification { }
public class PaymentProcessedEvent : INotification { }

// Bad: Present tense
public class CreateUserEvent : INotification { }
public class ShipOrderEvent : INotification { }
```

## Project Structure

### Feature-Based Organization

Organize by feature rather than by type:

```
Features/
  Users/
    GetUserQuery.cs
    GetUserQueryHandler.cs
    CreateUserCommand.cs
    CreateUserCommandHandler.cs
    UserCreatedEvent.cs
    UserCreatedEventHandler.cs
  Orders/
    CreateOrderCommand.cs
    CreateOrderCommandHandler.cs
    OrderCreatedEvent.cs
    OrderCreatedEventHandler.cs
```

### Alternative: Type-Based Organization

If you prefer organizing by type:

```
Commands/
  CreateUserCommand.cs
  UpdateUserCommand.cs
Queries/
  GetUserQuery.cs
  GetUsersQuery.cs
Handlers/
  CreateUserCommandHandler.cs
  GetUserQueryHandler.cs
Events/
  UserCreatedEvent.cs
Handlers/
  UserCreatedEventHandler.cs
```

## Handler Design

### Keep Handlers Focused

Each handler should do one thing:

```csharp
// Good: Focused handler
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User { Name = request.Name, Email = request.Email };
        return await _repository.AddAsync(user, cancellationToken);
    }
}

// Bad: Handler doing too much
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Validation
        // Business logic
        // Database operations
        // Email sending
        // Logging
        // Cache updates
        // ... too much!
    }
}
```

### Use Dependency Injection

Inject dependencies rather than creating them:

```csharp
// Good: Dependencies injected
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    private readonly IRepository _repository;
    private readonly ILogger<MyHandler> _logger;

    public MyHandler(IRepository repository, ILogger<MyHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// Bad: Creating dependencies
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    public async Task<MyResponse> Handle(MyRequest request, CancellationToken ct)
    {
        var repository = new Repository(); // Bad!
        // ...
    }
}
```

### Handle Errors Appropriately

```csharp
// Good: Proper error handling
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

## Request/Command Design

### Keep Requests Simple

Requests should be simple data containers:

```csharp
// Good: Simple request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

// Bad: Complex logic in request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
    
    public bool IsValid() { /* validation logic */ } // Bad!
    public UserDto ToDto() { /* mapping logic */ } // Bad!
}
```

### Use Immutable Requests When Possible

```csharp
// Good: Immutable request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; }

    public GetUserQuery(int userId)
    {
        UserId = userId;
    }
}

// Also acceptable: Mutable request
public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}
```

### Validate Requests

```csharp
// Option 1: Validate in handler
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required");
        }
        // ...
    }
}

// Option 2: Use FluentValidation or similar
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

## Notification Design

### Make Events Immutable

```csharp
// Good: Immutable event
public class UserCreatedEvent : INotification
{
    public int UserId { get; }
    public string Email { get; }
    public DateTime CreatedAt { get; }

    public UserCreatedEvent(int userId, string email, DateTime createdAt)
    {
        UserId = userId;
        Email = email;
        CreatedAt = createdAt;
    }
}

// Bad: Mutable event
public class UserCreatedEvent : INotification
{
    public int UserId { get; set; } // Bad!
    public string Email { get; set; } // Bad!
}
```

### Include Relevant Data

Include all data that handlers might need:

```csharp
// Good: Includes all relevant data
public class OrderShippedEvent : INotification
{
    public int OrderId { get; }
    public string TrackingNumber { get; }
    public DateTime ShippedAt { get; }
    public Address ShippingAddress { get; }
}

// Bad: Missing data
public class OrderShippedEvent : INotification
{
    public int OrderId { get; } // Handlers might need more info!
}
```

## Error Handling

### Use Specific Exceptions

```csharp
// Good: Specific exception
throw new NotFoundException($"User {userId} not found");
throw new ValidationException("Email is required");
throw new UnauthorizedException("User not authorized");

// Bad: Generic exception
throw new Exception("Error occurred");
```

### Handle Exceptions at the Right Level

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
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, "An error occurred");
}
```

## Testing

### Test Handlers in Isolation

```csharp
[Fact]
public async Task Handle_ShouldReturnUser_WhenUserExists()
{
    // Arrange
    var repository = new Mock<IUserRepository>();
    var handler = new GetUserQueryHandler(repository.Object);
    var query = new GetUserQuery { UserId = 1 };

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    repository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
}
```

### Test Integration Scenarios

```csharp
[Fact]
public async Task Send_ShouldPublishEvent_WhenCommandSucceeds()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddCoordix();
    services.AddScoped<IUserRepository, UserRepository>();
    
    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    await mediator.Send(new CreateUserCommand { Name = "John" });

    // Assert
    // Verify event was published
}
```

## Performance

### Use Appropriate Service Lifetime

```csharp
// Stateless handler - can be singleton
services.AddSingleton<IRequestHandler<MyRequest, MyResponse>, MyHandler>();

// Handler with scoped dependencies - must be scoped
services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyHandler>();
```

### Avoid Blocking Calls

```csharp
// Good: Async all the way
public async Task<MyResponse> Handle(MyRequest request, CancellationToken ct)
{
    var data = await _repository.GetAsync(ct);
    return new MyResponse { Data = data };
}

// Bad: Blocking
public Task<MyResponse> Handle(MyRequest request, CancellationToken ct)
{
    var data = _repository.GetAsync(ct).Result; // Blocks!
    return Task.FromResult(new MyResponse { Data = data });
}
```

## Background Jobs

When using `Coordix.Background` (separate package), follow these best practices:

### When to Use Background Jobs

✅ **Use background jobs for:**
- Email sending
- Logging/Auditing
- User notifications
- Non-critical data processing
- External API calls (third-party integrations)

❌ **Don't use background jobs for:**
- Critical operations requiring immediate feedback
- Operations that need the response value
- Time-sensitive operations
- Operations requiring transaction context

### Registration Order

Always register Coordix core before Coordix.Background:

```csharp
// ✅ Correct order
services.AddCoordix();           // Core mediator
services.AddCoordixBackground(); // Background extension

// ❌ Wrong - Background needs core
services.AddCoordixBackground();
services.AddCoordix();
```

### Error Handling in Background Jobs

Background jobs should handle errors gracefully:

```csharp
public class SendEmailHandler : IRequestHandler<SendEmailRequest>
{
    private readonly ILogger<SendEmailHandler> _logger;
    private readonly IEmailService _emailService;

    public async Task Handle(SendEmailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(request.To, request.Subject, request.Body);
        }
        catch (Exception ex)
        {
            // Log and handle gracefully - don't let it crash the worker
            _logger.LogError(ex, "Failed to send email to {To}", request.To);
            // Consider implementing retry logic or dead-letter queue
        }
    }
}
```

### Service Lifetime Considerations

Background jobs run outside the original request scope. Ensure handlers and their dependencies are registered with appropriate lifetimes:

```csharp
// Handlers are resolved from a new scope for each job
services.AddScoped<IRequestHandler<MyRequest>, MyHandler>();
services.AddScoped<IMyService, MyService>(); // Available in handler
```

### Don't Mix Synchronous and Background Processing

Be clear about when to use `IMediator` vs `IBackgroundMediator`:

```csharp
// ✅ Clear separation
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBackgroundMediator _backgroundMediator;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        // Critical: Process synchronously
        var order = await _mediator.Send(new CreateOrderCommand { ... });

        // Non-critical: Process in background
        await _backgroundMediator.Enqueue(new SendOrderConfirmationEmail 
        { 
            OrderId = order.Id 
        });

        return Ok(order);
    }
}
```

For more information, see the [Background Jobs Guide](../background/background-jobs.md).

## Security

### Validate Input

Always validate input in handlers:

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required");
        }

        // Sanitize input
        var sanitizedEmail = request.Email.Trim().ToLowerInvariant();
        
        // Process command
    }
}
```

### Authorize Actions

Check authorization in handlers:

```csharp
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IAuthorizationService _authorization;

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Check authorization
        var authorized = await _authorization.AuthorizeAsync(
            request.UserId, 
            "DeleteUser");
        
        if (!authorized)
        {
            throw new UnauthorizedException("Not authorized to delete user");
        }

        // Process command
    }
}
```

## Next Steps

- Read the [Usage Guide](./usage.md) for more examples
- Check the [API Reference](./api-reference.md) for complete API documentation
- Explore the [Examples](../../samples) for real-world patterns

