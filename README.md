<h1 align="center"><br>
<a href="https://github.com/gabriel-sisjr/coordix">
<img src="assets/logo.png" width="300px">
</a>
</h1>
<h4 align="center">Lightweight mediator for .NET with zero reflection overhead (optional)</h4>
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
</p>

---

## 🚀 Quickstart

### 1. Install

```bash
dotnet add package Coordix
```

### 2. Register

```csharp
// Program.cs
builder.Services.AddCoordix();
```

### 3. Use

```csharp
// Request with response
public class GetUser : IRequest<UserDto> { public int Id { get; set; } }

public class GetUserHandler : IRequestHandler<GetUser, UserDto>
{
    public async Task<UserDto> Handle(GetUser request, CancellationToken ct)
        => await _repository.GetByIdAsync(request.Id);
}

// Usage
var user = await _mediator.Send(new GetUser { Id = 1 });
```

```csharp
// Command (no response)
public class CreateUser : IRequest { public string Name { get; set; } }

public class CreateUserHandler : IRequestHandler<CreateUser>
{
    public async Task Handle(CreateUser request, CancellationToken ct)
        => await _repository.AddAsync(new User { Name = request.Name });
}

// Usage
await _mediator.Send(new CreateUser { Name = "John" });
```

```csharp
// Notification (event, multiple handlers)
public class UserCreated : INotification { public int UserId { get; set; } }

public class SendEmailHandler : INotificationHandler<UserCreated>
{
    public async Task Handle(UserCreated notification, CancellationToken ct)
        => await _emailService.SendWelcomeEmail(notification.UserId);
}

public class LogHandler : INotificationHandler<UserCreated>
{
    public Task Handle(UserCreated notification, CancellationToken ct)
    {
        _logger.LogInformation("User {Id} created", notification.UserId);
        return Task.CompletedTask;
    }
}

// Usage
await _mediator.Publish(new UserCreated { UserId = userId });
```

**Done.** That's all you need.

---

## ⚙️ Choose Your Execution Mode

### Reflection Mode (Default)

**Zero dependencies. Optimized with cached delegates.**

```csharp
builder.Services.AddCoordix(); // That's it
```

| Metric     | Value             |
| ---------- | ----------------- |
| Latency    | ~500ns            |
| Throughput | ~2M ops/s         |
| Memory     | 192B/op           |
| Startup    | 15ms (first call) |

**When to use:** You don't have performance problems.

### CodeGen Mode (Zero Reflection)

**Compile-time code generation. Direct method calls.**

```bash
dotnet add package Coordix.CodeGen
```

```csharp
builder.Services.AddCoordix(options => {
    options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
});
```

| Metric     | Value     | vs Reflection |
| ---------- | --------- | ------------- |
| Latency    | ~200ns    | **-61%**      |
| Throughput | ~5M ops/s | **+160%**     |
| Memory     | 96B/op    | **-50%**      |
| Startup    | 0ms       | **instant**   |

**When to use:** Performance matters or you want faster startup.

### Benchmark Proof

```bash
cd tests/Benchmarks
dotnet run -c Release
```

**Request/Response:**
| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| Coordix CodeGen | 51.84 ns | 352 B | **1.00x** |
| MediatR | 89.91 ns | 496 B | 1.73x |
| Coordix Reflection | 181.72 ns | 600 B | 3.50x |
| Wolverine | 270.03 ns | 944 B | 5.21x |

**Notifications (10 handlers):**
| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| Coordix CodeGen | 245.8 ns | 424 B | **1.00x** |
| MediatR | 546.6 ns | 3,176 B | 2.22x |
| Coordix Reflection | 914.1 ns | 3,152 B | 3.72x |
| Wolverine | 3,107.3 ns | 1,576 B | 12.64x |

**Coordix CodeGen vs MediatR:** 1.7-2.2x faster, 29-87% less memory.  
**Coordix CodeGen vs Wolverine:** 5.2-12.6x faster, 63-73% less memory.

_Note: Wolverine benchmarks use Dynamic mode (reflection-based). True CodeGen mode would require pre-generated code._

_Tested on .NET 8.0.22, Apple M4, BenchmarkDotNet v0.13.12_

---

## 🏁 Background Jobs (Optional)

Fire-and-forget in-process job execution.

```bash
dotnet add package Coordix.Background
```

```csharp
builder.Services.AddCoordix();
builder.Services.AddCoordixBackground();
```

```csharp
public class MyController
{
    private readonly IBackgroundMediator _bg;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand cmd)
    {
        // Critical: save user (sync)
        var user = await _mediator.Send(cmd);

        // Non-critical: send email (async)
        await _bg.Enqueue(new SendWelcomeEmail { UserId = user.Id });

        return Ok(user);
    }
}
```

### ⚠️ Critical Limitations

| Feature         | Coordix.Background | Hangfire           |
| --------------- | ------------------ | ------------------ |
| **Durable**     | ❌ No (in-memory)  | ✅ Yes (persisted) |
| **Retry**       | ❌ No              | ✅ Yes             |
| **Distributed** | ❌ No              | ✅ Yes             |
| **Setup**       | 1 line             | Heavy              |

**Use Coordix.Background for:** Non-critical jobs (logs, notifications) that you can afford to lose.

**Use Hangfire/MassTransit for:** Critical jobs that must not be lost.

---

## 📚 Documentation

**Core Concepts:**

- [CORE.md](./docs/CORE.md) - How it works, DI integration, execution modes
- [CODEGEN.md](./docs/CODEGEN.md) - Zero-reflection mode, analyzers, benchmarks
- [BACKGROUND.md](./docs/BACKGROUND.md) - Fire-and-forget jobs, limitations
- [PERFORMANCE.md](./docs/PERFORMANCE.md) - Detailed benchmarks with BenchmarkDotNet
- [TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md) - Common errors and solutions

**Legacy Docs:**

- [Installation Guide](./docs/core/installation.md)
- [Getting Started](./docs/core/getting-started.md)
- [Usage Guide](./docs/core/usage.md)
- [API Reference](./docs/core/api-reference.md)
- [Best Practices](./docs/core/best-practices.md)
- [Migration from MediatR](./docs/core/migration.md)
- [FAQ](./docs/core/faq.md)

---

## 🎯 Why Coordix?

### vs MediatR

| Feature                     | Coordix            | MediatR |
| --------------------------- | ------------------ | ------- |
| **Performance (CodeGen)**   | 201 ns             | ~650 ns |
| **Memory (CodeGen)**        | 96 B               | ~240 B  |
| **Zero Reflection**         | ✅ Yes (optional)  | ❌ No   |
| **Compile-time Validation** | ✅ Yes (analyzers) | ❌ No   |
| **Setup**                   | 1 line             | 1 line  |
| **Pipelines/Behaviors**     | ❌ No              | ✅ Yes  |

**Choose Coordix if:** Performance matters or you want compile-time safety.

**Choose MediatR if:** You need pipelines/behaviors.

### Key Features

- ✅ **Two execution modes:** Reflection (fast) or CodeGen (faster)
- ✅ **Zero dependencies** (except Microsoft.Extensions.DependencyInjection)
- ✅ **Compile-time analyzers** detect errors before runtime
- ✅ **Background jobs** for fire-and-forget operations
- ✅ **Thread-safe** with optimized caching
- ✅ **Scoped service support** with proper lifetime management

---

## 🧪 Quality

### Comprehensive Testing

- ✅ Unit tests (100% critical path coverage)
- ✅ Integration tests (DI container scenarios)
- ✅ Robustness tests (exceptions, scopes, edge cases)
- ✅ Background worker tests (concurrent jobs, failures)

### Roslyn Analyzers

Detect problems at **compile-time:**

| Code       | Description                              | Severity |
| ---------- | ---------------------------------------- | -------- |
| COORDIX001 | CodeGenPreferred without CodeGen package | Error    |
| COORDIX002 | Handler missing public Handle()          | Error    |
| COORDIX003 | Duplicate handlers                       | Warning  |
| COORDIX005 | Non-public handler                       | Warning  |

**Result:** Most bugs caught before runtime.

---

## 📦 Packages

| Package                | Purpose              | Install                                 |
| ---------------------- | -------------------- | --------------------------------------- |
| **Coordix**            | Core mediator        | `dotnet add package Coordix`            |
| **Coordix.CodeGen**    | Zero-reflection mode | `dotnet add package Coordix.CodeGen`    |
| **Coordix.Background** | Background jobs      | `dotnet add package Coordix.Background` |

---

## 💡 Examples

Complete, runnable samples in [`/samples`](./samples):

- ✅ [Simple Sample](./samples/SimpleSample) - Basic Send/Publish
- ✅ [Advanced Sample](./samples/AdvancedSample) - Multiple handlers, events, patterns
- ✅ [Background Jobs Sample](./samples/BackgroundJobsSample) - Fire-and-forget processing
- ✅ [CodeGen Sample](./samples/CodeGenSample) - Zero-reflection execution

---

## 🤝 Contributing

PRs welcome! For major changes, open an issue first.

```bash
git checkout -b feature/AmazingFeature
git commit -m 'feat: Add AmazingFeature'
git push origin feature/AmazingFeature
```

Follow [conventional commits](https://github.com/gabriel-sisjr/coordix/blob/main/commitlint.config.js).

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file.

---

## 🌟 Give a Star

If Coordix saved you time, **star the repo!**

<p align="center">Made with ❤️ by the Coordix community</p>
