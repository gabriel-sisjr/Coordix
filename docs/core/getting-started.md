# Getting Started Guide

This guide will walk you through creating your first Coordix application step by step.

## Prerequisites

- .NET SDK 6.0 or higher
- Basic understanding of C# and dependency injection

## Step 1: Create a New Project

```bash
dotnet new webapi -n MyCoordixApp
cd MyCoordixApp
```

## Step 2: Install Coordix

```bash
# Install the core package
dotnet add package Coordix

# Optional: Install Coordix.Background for background jobs
dotnet add package Coordix.Background
```

> **Note**: `Coordix.Background` is a separate package that extends Coordix with background job processing. It requires `Coordix` to be installed first.

## Step 3: Register Coordix

Open `Program.cs` and add Coordix:

```csharp
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Coordix
builder.Services.AddCoordix();

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

## Step 4: Create Your First Request

Create a folder called `Features` and add a `Users` subfolder. Create a request:

```csharp
// Features/Users/GetUserQuery.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

## Step 5: Create Your First Handler

```csharp
// Features/Users/GetUserQueryHandler.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    // In a real app, you'd inject a repository here
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Simulate async operation
        await Task.Delay(100, cancellationToken);

        // Return mock data
        return new UserDto
        {
            Id = request.UserId,
            Name = "John Doe",
            Email = "john.doe@example.com"
        };
    }
}
```

## Step 6: Use the Mediator in a Controller

```csharp
// Controllers/UsersController.cs
using Coordix.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MyCoordixApp.Features.Users;

namespace MyCoordixApp.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var query = new GetUserQuery { UserId = id };
        var user = await _mediator.Send(query);
        return Ok(user);
    }
}
```

## Step 7: Run Your Application

```bash
dotnet run
```

Test the endpoint:

```bash
curl http://localhost:5000/api/users/1
```

## Next: Create a Command

Now let's create a command that doesn't return a value:

```csharp
// Features/Users/CreateUserCommand.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

public class CreateUserCommand : IRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

```csharp
// Features/Users/CreateUserCommandHandler.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // In a real app, you'd save to database here
        await Task.Delay(100, cancellationToken);
        Console.WriteLine($"User created: {request.Name} ({request.Email})");
    }
}
```

Add to the controller:

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
{
    await _mediator.Send(command);
    return Ok(new { message = "User created successfully" });
}
```

## Next: Add a Notification (Event)

Create an event that gets published when a user is created:

```csharp
// Features/Users/UserCreatedEvent.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

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
```

Create a handler for the event:

```csharp
// Features/Users/SendWelcomeEmailHandler.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // In a real app, you'd send an email here
        await Task.Delay(100, cancellationToken);
        Console.WriteLine($"Welcome email sent to: {notification.Email}");
    }
}
```

Update the command handler to publish the event:

```csharp
// Features/Users/CreateUserCommandHandler.cs
using Coordix.Interfaces;

namespace MyCoordixApp.Features.Users;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    private readonly IMediator _mediator;

    public CreateUserCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Save user (mock)
        var userId = new Random().Next(1, 1000);
        await Task.Delay(100, cancellationToken);
        
        Console.WriteLine($"User created: {request.Name} ({request.Email})");

        // Publish event
        await _mediator.Publish(new UserCreatedEvent(userId, request.Email), cancellationToken);
    }
}
```

## Complete Example

Here's a complete working example:

```csharp
// Program.cs
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoordix();
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
```

```csharp
// Controllers/UsersController.cs
using Coordix.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MyCoordixApp.Features.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _mediator.Send(new GetUserQuery { UserId = id });
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "User created successfully" });
    }
}
```

## What's Next?

- Read the [Usage Guide](./usage.md) for advanced patterns
- Check out [Best Practices](./best-practices.md) for recommended patterns
- Explore the [Examples](../../samples) folder for complete applications

