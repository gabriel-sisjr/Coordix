using CodeGenSample;
using CodeGenSample.Notifications;
using CodeGenSample.Requests;
using Coordix;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;

// ====================================
// Setup: Configure services with CodeGen
// ====================================

var services = new ServiceCollection();

// Use AddCoordixWithCodeGen to enable code generation mode
// This will use the generated HandlerExecutor instead of reflection
// Pass the current assembly to scan for handlers
services.AddCoordixWithCodeGen(configureOptions: null, typeof(Program).Assembly);

// Add logging
services.AddLogging();

var serviceProvider = services.BuildServiceProvider();

// ====================================
// Get the mediator instance
// ====================================

var mediator = serviceProvider.GetRequiredService<IMediator>();
var options = serviceProvider.GetRequiredService<CoordixOptions>();

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         Coordix CodeGen Sample Application                 ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"Handler Resolution Mode: {options.HandlerResolutionMode}");
Console.WriteLine("Using GENERATED HandlerExecutor (no reflection at runtime!)");
Console.WriteLine();

// ====================================
// Test 1: Request with Response
// ====================================

Console.WriteLine("─────────────────────────────────────────────────────────────");
Console.WriteLine("Test 1: Request with Response (GetUserRequest)");
Console.WriteLine("─────────────────────────────────────────────────────────────");

var getUserRequest = new GetUserRequest { UserId = 42 };
var userResponse = await mediator.Send(getUserRequest);

Console.WriteLine($"✓ Response received: User {userResponse.UserId} - {userResponse.Name}");
Console.WriteLine();

// ====================================
// Test 2: Request without Response (Command)
// ====================================

Console.WriteLine("─────────────────────────────────────────────────────────────");
Console.WriteLine("Test 2: Request without Response (CreateUserCommand)");
Console.WriteLine("─────────────────────────────────────────────────────────────");

var createUserCommand = new CreateUserCommand
{
	Name = "Jane Smith",
	Email = "jane.smith@example.com"
};

await mediator.Send(createUserCommand);
Console.WriteLine("✓ Command executed successfully");
Console.WriteLine();

// ====================================
// Test 3: Notification (multiple handlers)
// ====================================

Console.WriteLine("─────────────────────────────────────────────────────────────");
Console.WriteLine("Test 3: Notification (UserCreatedNotification)");
Console.WriteLine("─────────────────────────────────────────────────────────────");

var notification = new UserCreatedNotification
{
	UserId = 99,
	Name = "Bob Johnson",
	Email = "bob.johnson@example.com"
};

await mediator.Publish(notification);
Console.WriteLine("✓ Notification published to all handlers");
Console.WriteLine();

// ====================================
// Summary
// ====================================

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    All Tests Passed!                       ║");
Console.WriteLine("║                                                            ║");
Console.WriteLine("║  The generated executor handled all requests/notifications ║");
Console.WriteLine("║  without using reflection - pure compiled code!           ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

