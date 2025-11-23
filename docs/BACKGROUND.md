# Background Jobs - Fire and Forget

## What Is It

Enqueues requests/notifications for **asynchronous in-process** execution.

```csharp
await backgroundMediator.Enqueue(new SendEmailCommand { To = "user@example.com" });
// Returns immediately. Email is sent in background.
```

## ⚠️ CRITICAL LIMITATIONS

### 1. NOT DURABLE
```
❌ If process dies, unprocessed jobs are LOST
❌ No automatic retry
❌ Does not persist to database/queue
```

**When to use:** Operations that can be lost without harm (logs, non-critical notifications)

**When NOT to use:** Payments, critical confirmations, external system integrations

### 2. IN-PROCESS ONLY
Jobs run **in the same process** as your application. If you do:
```csharp
await backgroundMediator.Enqueue(new HeavyCpuTask());
```

You're **consuming your API's CPU**. Use sparingly.

### 3. NO PRIORITIZATION
Jobs are processed **FIFO** (First In, First Out). There's no concept of priority.

## Installation

```bash
dotnet add package Coordix.Background
```

```csharp
// AddCoordixBackground automatically registers core Coordix services
builder.Services.AddCoordixBackground(); // Adds IBackgroundMediator + core services
```

> **Note**: There is no need to call `AddCoordix()` explicitly. `AddCoordixBackground()` registers all required core services automatically.

## Usage

### Enqueue Request
```csharp
public class MyController : ControllerBase
{
    private readonly IBackgroundMediator _bg;

    public MyController(IBackgroundMediator bg) => _bg = bg;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand cmd)
    {
        // Save user (synchronous, critical)
        var user = await _mediator.Send(cmd);

        // Send welcome email (asynchronous, non-critical)
        await _bg.Enqueue(new SendWelcomeEmail { UserId = user.Id });

        return Ok(user);
    }
}
```

### Enqueue Notification
```csharp
// Publish event in background to multiple handlers
await _bg.Enqueue(new UserCreatedEvent { UserId = userId });
```

## How It Works

```
HTTP Request → Enqueue() → Channel<BackgroundJob> → BackgroundWorker
                                                            ↓
                                            ProcessJobAsync (new scope)
                                                            ↓
                                            IHandlerExecutor.Execute()
```

### Scoped Services

Each job creates a **new scope:**

```csharp
public class SendEmailHandler : IRequestHandler<SendEmail>
{
    private readonly AppDbContext _db; // NEW scope per job

    public async Task Handle(SendEmail request, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        // Send email...
    }
}
```

**Guarantee:** `DbContext` is unique per job and correctly disposed.

## Exception Handling

```csharp
public class FailingHandler : IRequestHandler<SendEmail>
{
    public Task Handle(SendEmail request, CancellationToken ct)
    {
        throw new Exception("SMTP failed!");
    }
}
```

**Behavior:**
- Exception is **logged** (LogLevel.Error)
- Worker **continues** processing next jobs
- Failed job is **discarded** (no retry)

**Log:**
```
[Error] Error processing background job: SendEmail
System.Exception: SMTP failed!
```

## Performance

Background worker is optimized for **zero reflection**:

```csharp
// BEFORE (reflection at runtime):
MethodInfo method = GetMethod(...);
method.MakeGenericMethod(...).Invoke(...);

// NOW (delegation to executor):
await executor.ExecuteRequestHandlerDynamic(request, responseType, ct);
```

**Result:** Job processing as fast as direct calls.

## Graceful Shutdown

When application is terminated:

```csharp
public override async Task StopAsync(CancellationToken ct)
{
    _logger.LogInformation("Background worker stopping...");
    // Worker tries to finish current job (respecting cancellation token)
}
```

**Warning:** Jobs still in queue are **lost**.

## Comparison with Alternatives

| Feature | Coordix.Background | Hangfire | MassTransit |
|---------|-------------------|----------|-------------|
| Setup | 1 line | Heavy | Very heavy |
| Durability | ❌ No | ✅ Yes | ✅ Yes |
| Retry | ❌ No | ✅ Yes | ✅ Yes |
| Distributed | ❌ No | ✅ Yes | ✅ Yes |
| Overhead | Zero | Medium | High |

**Use Coordix.Background when:**
- Quick setup for MVPs
- Non-critical jobs (logs, notifications)
- You accept losing jobs on crash

**Use Hangfire/MassTransit when:**
- Critical jobs that cannot be lost
- Need retry/scheduling
- Distributed application

---

**Golden rule:** Coordix background jobs are **convenient but not reliable**. Use for operations that can fail without business impact.
