# Coordix.Background

Background job processing extension for Coordix mediator - fire-and-forget in-process background jobs.

[![Coordix.Background NuGet Version](https://img.shields.io/nuget/vpre/Coordix.Background.svg)](https://www.nuget.org/packages/Coordix.Background)  
[![Coordix.Background NuGet Downloads](https://img.shields.io/nuget/dt/Coordix.Background.svg)](https://www.nuget.org/packages/Coordix.Background)

## Overview

`Coordix.Background` is a separate NuGet package that extends [Coordix](https://www.nuget.org/packages/Coordix) with fire-and-forget background job processing capabilities. It allows you to enqueue requests and notifications to be processed asynchronously outside the original request context.

## Key Features

- ✅ **Fire-and-forget processing** - Enqueue jobs that are processed asynchronously
- ✅ **In-process queue** - Uses `System.Threading.Channels` for efficient in-memory queuing
- ✅ **Automatic processing** - Background worker processes jobs automatically
- ✅ **Error isolation** - Exceptions in one job don't stop processing of other jobs
- ✅ **Same mediator** - Uses the same `IMediator` interface and handlers as Coordix core
- ✅ **Requires Coordix** - Automatically installs Coordix as a dependency

## Installation

```bash
dotnet add package Coordix.Background
```

> **Important**: `Coordix.Background` requires `Coordix` to be installed. The core package will be automatically installed as a dependency.

## Quick Start

### 1. Install Packages

```bash
dotnet add package Coordix
dotnet add package Coordix.Background
```

### 2. Register Services

```csharp
using Coordix.Extensions;
using Coordix.Background.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Coordix core (required)
builder.Services.AddCoordix();

// Register Coordix.Background
builder.Services.AddCoordixBackground();

// Register your handlers
builder.Services.AddScoped<IRequestHandler<SendEmailRequest>, SendEmailHandler>();
```

### 3. Use Background Mediator

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

## When to Use Background Jobs

Use `Coordix.Background` for:
- ✅ Email sending (don't block HTTP response)
- ✅ Logging/Auditing (non-critical operations)
- ✅ Notifications (user notifications, push notifications)
- ✅ Data processing (heavy computations)
- ✅ External API calls (third-party integrations)

Don't use for:
- ❌ Critical operations requiring immediate feedback
- ❌ Operations that need the response value
- ❌ Time-sensitive operations
- ❌ Operations requiring transaction context

## Example: Enqueueing Requests

```csharp
// Request without response
public class SendEmailRequest : IRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

// Enqueue for background processing
await _backgroundMediator.Enqueue(new SendEmailRequest 
{ 
    To = "user@example.com",
    Subject = "Welcome!",
    Body = "Thank you for joining us!"
});
```

## Example: Enqueueing Notifications

```csharp
// Notification
public class OrderCreatedNotification : INotification
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

// Enqueue notification (all handlers will be called in background)
await _backgroundMediator.Enqueue(new OrderCreatedNotification 
{ 
    OrderId = "ORD-456",
    Total = 199.99m
});
```

## Important Notes

- Background jobs use the **same handlers** registered with `AddCoordix()`
- Jobs are processed **outside the original request context**
- Exceptions in one job **don't stop processing** of other jobs
- Jobs are **in-process only** (lost on application restart)
- Always register `AddCoordix()` before `AddCoordixBackground()`

## Documentation

For comprehensive documentation, visit the [GitHub repository](https://github.com/gabriel-sisjr/coordix):

### Core Documentation

- 📖 [Installation Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/installation.md)
- 🚀 [Getting Started Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/getting-started.md)
- 📚 [Usage Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/usage.md)
- 🎯 [Best Practices](https://github.com/gabriel-sisjr/coordix/blob/main/docs/core/best-practices.md)

### Coordix.Background Documentation

- 🔄 [Background Jobs Guide](https://github.com/gabriel-sisjr/coordix/blob/main/docs/background/background-jobs.md) - Complete guide for Coordix.Background

## Examples

Check out the [BackgroundJobsSample](https://github.com/gabriel-sisjr/coordix/tree/main/samples/BackgroundJobsSample) for a complete working example.

## Requirements

- .NET Standard 2.1 or higher
- .NET Core 2.1+ / .NET 5+ / .NET 6+ / .NET 7+ / .NET 8+
- **Coordix** package (automatically installed as dependency)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Hosting

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/gabriel-sisjr/coordix/blob/main/LICENSE) file for details.

## Related Packages

- [Coordix](https://www.nuget.org/packages/Coordix) - Core mediator implementation (required)

