# Background Jobs with Coordix.Background

Coordix.Background is a separate NuGet package that extends Coordix with fire-and-forget background job processing capabilities. It allows you to enqueue requests and notifications to be processed asynchronously outside the original request context.

## Overview

Coordix.Background provides:
- **Fire-and-forget processing**: Enqueue jobs that are processed asynchronously
- **In-process queue**: Uses `System.Threading.Channels` for efficient in-memory queuing
- **Automatic processing**: Background worker processes jobs automatically
- **Error isolation**: Exceptions in one job don't stop processing of other jobs
- **Same mediator**: Uses the same `IMediator` interface and handlers as Coordix core

## Installation

```bash
dotnet add package Coordix.Background
```

> **Important**: `Coordix.Background` requires `Coordix` to be installed. The core package will be automatically installed as a dependency.

## Configuration

### Basic Setup

```csharp
using Coordix.Extensions;
using Coordix.Background.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Coordix core (required)
builder.Services.AddCoordix();

// 2. Register Coordix.Background
builder.Services.AddCoordixBackground();

// 3. Register your handlers (same as Coordix core)
builder.Services.AddScoped<IRequestHandler<SendEmailRequest>, SendEmailHandler>();
```

### How It Works

1. `AddCoordix()` - Registers the core `IMediator` and discovers handlers
2. `AddCoordixBackground()` - Registers:
   - `IBackgroundMediator` - For enqueueing jobs
   - `BackgroundWorker` - A hosted service that processes jobs from the queue
   - Internal channel for job queuing

## Usage

### Enqueueing Requests

#### Request Without Response

```csharp
using Coordix.Background.Interfaces;
using Coordix.Interfaces;

public class SendEmailRequest : IRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class EmailController : ControllerBase
{
    private readonly IBackgroundMediator _backgroundMediator;

    public EmailController(IBackgroundMediator backgroundMediator)
    {
        _backgroundMediator = backgroundMediator;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail(SendEmailRequest request)
    {
        // Enqueue for background processing (fire-and-forget)
        await _backgroundMediator.Enqueue(request);
        
        return Ok(new { Message = "Email queued for sending" });
    }
}
```

#### Request With Response

```csharp
public class ProcessPaymentRequest : IRequest<string>
{
    public decimal Amount { get; set; }
    public string CustomerId { get; set; } = string.Empty;
}

// Enqueue (response is ignored in background processing)
await _backgroundMediator.Enqueue<string>(new ProcessPaymentRequest 
{ 
    Amount = 99.99m, 
    CustomerId = "CUST-123" 
});
```

### Enqueueing Notifications

```csharp
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

## When to Use Background Jobs

### Use Background Jobs For:

- ✅ **Email sending** - Don't block the HTTP response
- ✅ **Logging/Auditing** - Non-critical operations
- ✅ **Notifications** - User notifications, push notifications
- ✅ **Data processing** - Heavy computations
- ✅ **External API calls** - Third-party integrations
- ✅ **Report generation** - Long-running tasks

### Don't Use Background Jobs For:

- ❌ **Critical operations** - Where you need immediate feedback
- ❌ **Operations requiring response** - Background jobs are fire-and-forget
- ❌ **Time-sensitive operations** - No guarantee of immediate processing
- ❌ **Operations requiring transaction context** - Background jobs run outside the original request context

## Error Handling

By default, exceptions in background job handlers are logged but don't stop processing of other jobs:

```csharp
// If this handler throws an exception, it's logged but other jobs continue processing
public class SendEmailHandler : IRequestHandler<SendEmailRequest>
{
    public Task Handle(SendEmailRequest request, CancellationToken cancellationToken)
    {
        // If this throws, the error is logged and processing continues
        throw new InvalidOperationException("Email service unavailable");
    }
}
```

The background worker logs errors but continues processing:

```
[Error] Error processing background job: SendEmailRequest
System.InvalidOperationException: Email service unavailable
   at SendEmailHandler.Handle(...)
```

## Architecture

```
┌─────────────────┐
│  HTTP Request   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────────────┐
│ IBackground     │─────▶│  Channel (Queue)     │
│ Mediator        │      │  (In-Memory)         │
└─────────────────┘      └──────────┬───────────┘
                                    │
                                    ▼
                          ┌──────────────────────┐
                          │  BackgroundWorker    │
                          │  (Hosted Service)    │
                          │  (Creates scope     │
                          │   per job)          │
                          └──────────┬───────────┘
                                    │
                                    ▼
                          ┌──────────────────────┐
                          │  IHandlerExecutor    │
                          │  (Registry)         │
                          │  (Same as Core)     │
                          └──────────┬───────────┘
                                    │
                                    ▼
                          ┌──────────────────────┐
                          │  Handlers            │
                          │  (Your Code)         │
                          └──────────────────────┘
```

**Key Points:**
- Background jobs use the same `IHandlerExecutor` (registry) as the core mediator
- Each job is processed in its own service scope for proper lifetime management
- The registry handles all handler lookup, caching, and invocation
- No reflection is performed in `BackgroundWorker` - it delegates to the registry

## Best Practices

### 1. Always Register Coordix Core First

```csharp
// ✅ Correct order
services.AddCoordix();           // Core mediator
services.AddCoordixBackground(); // Background extension

// ❌ Wrong - Background needs core
services.AddCoordixBackground();
services.AddCoordix();
```

### 2. Use Background Jobs for Non-Critical Operations

```csharp
// ✅ Good - Non-critical operation
await _backgroundMediator.Enqueue(new SendWelcomeEmail { UserId = userId });

// ❌ Bad - Critical operation that needs immediate feedback
await _backgroundMediator.Enqueue(new ProcessPayment { Amount = 1000 });
```

### 3. Handle Errors in Your Handlers

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
            // Log and handle gracefully
            _logger.LogError(ex, "Failed to send email to {To}", request.To);
            // Consider retry logic or dead-letter queue
        }
    }
}
```

### 4. Use Scoped Services Carefully

Background jobs run outside the original request scope. Each job is processed in its own service scope, which ensures proper lifetime management for scoped services:

```csharp
// Handlers are resolved from a new scope for each job
services.AddScoped<IRequestHandler<MyRequest>, MyHandler>();
services.AddScoped<IMyService, MyService>(); // Available in handler
services.AddScoped<DbContext, MyDbContext>(); // Properly scoped per job
```

The `BackgroundWorker` creates a new `IServiceScope` for each job, resolves the `IHandlerExecutor` from that scope, and disposes the scope when the job completes. This ensures that scoped services (like `DbContext`) work correctly in background jobs.

## Limitations

### In-Process Only

Coordix.Background uses in-process channels. This means:
- Jobs are lost if the application restarts
- Not suitable for distributed scenarios
- No persistence across application restarts

### No Response Handling

Background jobs are fire-and-forget. You cannot:
- Wait for the result
- Get the response value
- Handle exceptions synchronously

### Single Worker

By default, one background worker processes jobs sequentially. For parallel processing, you would need to implement multiple workers (future enhancement).

## Troubleshooting

### Jobs Not Processing

1. **Check if the host is running**: Background worker requires a running host
   ```csharp
   await host.RunAsync(); // Must be running
   ```

2. **Verify handlers are registered**: Same as Coordix core
   ```csharp
   services.AddScoped<IRequestHandler<MyRequest>, MyHandler>();
   ```

3. **Check logs**: Background worker logs when it starts and processes jobs

### Jobs Processing Slowly

- Jobs are processed sequentially by default
- Consider optimizing handler performance
- For high-throughput scenarios, consider external message queues

## Examples

See the [BackgroundJobsSample](../samples/BackgroundJobsSample) for a complete working example.

## Related Documentation

- [Installation Guide](../core/installation.md) - How to install Coordix and Coordix.Background
- [Getting Started Guide](../core/getting-started.md) - Basic Coordix usage
- [Usage Guide](../core/usage.md) - Advanced patterns and examples
- [API Reference](../core/api-reference.md) - Complete API documentation

