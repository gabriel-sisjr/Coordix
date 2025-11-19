# Installation Guide

This guide will help you install and configure Coordix in your .NET application.

## Requirements

- .NET Standard 2.1 or higher
- .NET Core 2.1+ / .NET 5+ / .NET 6+ / .NET 7+ / .NET 8+
- Microsoft.Extensions.DependencyInjection (included with ASP.NET Core)

## Installation

### Via .NET CLI

```bash
dotnet add package Coordix
```

### Via Package Manager Console

```powershell
Install-Package Coordix
```

### Via PackageReference (in .csproj)

```xml
<ItemGroup>
  <PackageReference Include="Coordix" Version="0.0.4" />
</ItemGroup>
```

### Via NuGet Package Manager

1. Right-click on your project in Visual Studio
2. Select "Manage NuGet Packages..."
3. Search for "Coordix"
4. Click "Install"

## Basic Configuration

### Option 1: Automatic Registration (Recommended)

The easiest way to get started is to use automatic handler discovery:

```csharp
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// This will:
// 1. Register IMediator as a singleton
// 2. Automatically discover and register all handlers in the current AppDomain
builder.Services.AddCoordix();
```

### Option 2: Automatic Registration with Assembly Filtering

If you want to scan specific assemblies:

```csharp
using System.Reflection;
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Scan only specific assemblies
var assembly = Assembly.GetExecutingAssembly();
builder.Services.AddCoordix(assembly);

// Or scan multiple assemblies
var assemblies = new[] 
{ 
    Assembly.GetExecutingAssembly(),
    typeof(SomeHandler).Assembly 
};
builder.Services.AddCoordix(assemblies);
```

### Option 3: Automatic Registration with Namespace Filtering

Filter by namespace prefix:

```csharp
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Only scan assemblies whose FullName starts with "MyApp"
builder.Services.AddCoordix("MyApp");
```

### Option 4: Manual Registration

For full control over registration:

```csharp
using Coordix.Implementation;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register the mediator
builder.Services.AddSingleton<IMediator, Mediator>();

// Register handlers manually
builder.Services.AddScoped<IRequestHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();
builder.Services.AddTransient<IRequestHandler<CreateUserCommand>, CreateUserCommandHandler>();
builder.Services.AddScoped<INotificationHandler<UserCreatedEvent>, SendWelcomeEmailHandler>();
```

## Configuration Options

### Service Lifetime

By default, `AddCoordix()` registers:
- `IMediator` as **Singleton**
- Handlers as **Transient**

You can override handler lifetimes by registering them manually:

```csharp
// Register as Scoped
builder.Services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyHandler>();

// Register as Singleton
builder.Services.AddSingleton<INotificationHandler<MyEvent>, MyEventHandler>();
```

### Using AddMediator Alias

If you're migrating from another mediator library, you can use the `AddMediator` alias:

```csharp
using Coordix.Extensions;

builder.Services.AddMediator(); // Same as AddCoordix()
```

## Framework-Specific Setup

### ASP.NET Core

```csharp
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Coordix
builder.Services.AddCoordix();

var app = builder.Build();

// Use in controllers
app.MapControllers();
app.Run();
```

### Console Application

```csharp
using Coordix.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddCoordix();
        services.AddHostedService<MyService>();
    })
    .Build();

await host.RunAsync();
```

### Class Library

```csharp
using Coordix.Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyLibrary(this IServiceCollection services)
    {
        services.AddCoordix();
        // Add your other services
        return services;
    }
}
```

## Verification

To verify that Coordix is properly installed and configured:

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
// If no exception is thrown, Coordix is properly configured
```

## Next Steps

- Read the [Getting Started Guide](./getting-started.md) for a step-by-step tutorial
- Check out the [Usage Guide](./usage.md) for advanced patterns
- Explore the [Examples](../samples) folder for complete examples

