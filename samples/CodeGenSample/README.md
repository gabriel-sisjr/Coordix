# Coordix CodeGen Sample

This sample demonstrates the use of **Coordix.CodeGen**, which generates optimized handler execution code at compile-time, eliminating reflection overhead.

## What This Sample Shows

1. **Request with Response** (`GetUserRequest` → `GetUserResponse`)
   - Handler returns data strongly typed
   - No reflection, direct method invocation via generated code

2. **Request without Response** (`CreateUserCommand`)
   - Fire-and-forget command pattern
   - Handler executes side effects without returning data

3. **Notification with Multiple Handlers** (`UserCreatedNotification`)
   - Published to multiple handlers simultaneously
   - Both `UserCreatedEmailHandler` and `UserCreatedAuditHandler` execute
   - No reflection lookup, all handlers resolved at compile-time

## How CodeGen Works

### Traditional Approach (Reflection Mode)

```csharp
services.AddCoordix(); // Uses reflection at runtime
```

- Handlers discovered via `GetType()`, `MakeGenericType()`, `MethodInfo.Invoke()`
- Cached delegates improve performance but still have overhead
- Works everywhere, no build-time magic

### CodeGen Approach (CodeGenPreferred Mode)

```csharp
services.AddCoordixWithCodeGen(); // Uses source generator
```

- **Build-time**: Roslyn source generator scans your project
- **Discovers**: All classes implementing `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`
- **Generates**: `GeneratedHandlerExecutor` class with direct, strongly-typed calls
- **Runtime**: Zero reflection, pure compiled code

### Generated Code Example

For this sample, the generator creates something like:

```csharp
public sealed class GeneratedHandlerExecutor : IHandlerExecutor
{
    private readonly IServiceProvider _provider;

    public async Task<TResponse> ExecuteRequestHandler<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        if (requestType == typeof(global::CodeGenSample.Requests.GetUserRequest))
        {
            var handler = _provider.GetRequiredService<CodeGenSample.Handlers.GetUserHandler>();
            var typedRequest = (global::CodeGenSample.Requests.GetUserRequest)request;
            var result = await handler.Handle(typedRequest, cancellationToken);
            return (TResponse)(object)result!;
        }
        else
        {
            throw new InvalidOperationException($"Handler not found for {requestType.Name}");
        }
    }

    // ... similar for ExecuteRequestHandler() and ExecuteNotificationHandler<T>()
}
```

## Running the Sample

```bash
cd samples/CodeGenSample
dotnet run
```

You should see output demonstrating:

- Handler resolution mode is `CodeGenPreferred`
- All three test cases execute successfully
- Console messages from each handler showing execution

## Inspecting Generated Code

After building the project, you can view the generated source:

```bash
# Build the project
dotnet build

# Generated file is in obj folder
cat obj/Debug/net8.0/generated/Coordix.CodeGen/Coordix.CodeGen.CoordixSourceGenerator/GeneratedHandlerExecutor.g.cs
```

## Performance Benefits

- **Zero reflection overhead**: No `GetType()`, `MakeGenericType()`, or `MethodInfo.Invoke()`
- **Direct method calls**: JIT can inline and optimize aggressively
- **Startup time**: No runtime scanning or delegate compilation
- **AOT friendly**: Works with Native AOT compilation (future)

## When to Use CodeGen

✅ **Use CodeGen when:**

- Performance is critical
- You have many handlers
- Startup time matters
- You want AOT compatibility

❌ **Use Reflection when:**

- Prototyping quickly
- Dynamic handler registration needed
- CodeGen build complexity not desired
