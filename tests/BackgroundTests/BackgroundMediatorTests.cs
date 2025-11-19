using System;
using System.Threading;
using System.Threading.Tasks;
using Coordix.Background.Extensions;
using Coordix.Background.Interfaces;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Coordix.Background.Tests;

public class BackgroundMediatorTests
{

	[Fact]
	public async Task Enqueue_Request_Should_Process_In_Background()
	{
		// Arrange
		var processed = false;
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.AddConsole());
		services.AddCoordix();
		services.AddCoordixBackground();
		services.AddScoped<IRequestHandler<TestRequest>, TestRequestHandler>(sp =>
			new TestRequestHandler(() => processed = true));
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();
		var host = serviceProvider.GetRequiredService<IHostedService>();

		// Act
		await backgroundMediator.Enqueue(new TestRequest());

		// Start the background worker
		var cts = new CancellationTokenSource();
		await host.StartAsync(cts.Token);

		// Wait a bit for processing
		await Task.Delay(1000);

		// Assert
		Assert.True(processed, "Request should have been processed in background");

		cts.Cancel();
		await host.StopAsync(cts.Token);
	}

	[Fact]
	public async Task Enqueue_Request_With_Exception_Should_Not_Kill_Processing()
	{
		// Arrange
		var secondProcessed = false;
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.AddConsole());
		services.AddCoordix();
		services.AddCoordixBackground();
		services.AddScoped<IRequestHandler<TestRequest>, TestRequestHandler>(sp =>
			new TestRequestHandler(() => throw new InvalidOperationException("Test exception")));
		services.AddScoped<IRequestHandler<TestRequest2>, TestRequest2Handler>(sp =>
			new TestRequest2Handler(() => secondProcessed = true));
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();
		var host = serviceProvider.GetRequiredService<IHostedService>();

		// Act
		await backgroundMediator.Enqueue(new TestRequest()); // This will throw
		await backgroundMediator.Enqueue(new TestRequest2()); // This should still process

		// Start the background worker
		var cts = new CancellationTokenSource();
		await host.StartAsync(cts.Token);

		// Wait for processing
		await Task.Delay(1500);

		// Assert
		Assert.True(secondProcessed, "Second request should have been processed even after first failed");

		cts.Cancel();
		await host.StopAsync(cts.Token);
	}

	[Fact]
	public async Task Enqueue_Notification_Should_Process_In_Background()
	{
		// Arrange
		var processed = false;
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.AddConsole());
		services.AddCoordix();
		services.AddCoordixBackground();
		services.AddScoped<INotificationHandler<TestNotification>, TestNotificationHandler>(sp =>
			new TestNotificationHandler(() => processed = true));
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();
		var host = serviceProvider.GetRequiredService<IHostedService>();

		// Act
		await backgroundMediator.Enqueue(new TestNotification());

		// Start the background worker
		var cts = new CancellationTokenSource();
		await host.StartAsync(cts.Token);

		// Wait a bit for processing
		await Task.Delay(1000);

		// Assert
		Assert.True(processed, "Notification should have been processed in background");

		cts.Cancel();
		await host.StopAsync(cts.Token);
	}
}

// Test classes
public class TestRequest : IRequest { }
public class TestRequest2 : IRequest { }

public class TestRequestHandler : IRequestHandler<TestRequest>
{
	private readonly Action _action;

	public TestRequestHandler(Action? action = null)
	{
		_action = action ?? (() => { });
	}

	public Task Handle(TestRequest request, CancellationToken cancellationToken)
	{
		_action();
		return Task.CompletedTask;
	}
}

public class TestRequest2Handler : IRequestHandler<TestRequest2>
{
	private readonly Action? _action;

	public TestRequest2Handler(Action? action = null)
	{
		_action = action;
	}

	public Task Handle(TestRequest2 request, CancellationToken cancellationToken)
	{
		_action?.Invoke();
		return Task.CompletedTask;
	}
}

public class TestNotification : INotification { }

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
	private readonly Action _action;

	public TestNotificationHandler(Action? action = null)
	{
		_action = action ?? (() => { });
	}

	public Task Handle(TestNotification notification, CancellationToken cancellationToken)
	{
		_action();
		return Task.CompletedTask;
	}
}

