using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Background.Extensions;
using Coordix.Background.Implementation;
using Coordix.Background.Interfaces;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
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
	public async Task Enqueue_Request_With_Response_Should_Process_In_Background()
	{
		// Arrange
		var processed = false;
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.AddConsole());
		services.AddCoordix();
		services.AddCoordixBackground();
		services.AddScoped<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>(sp =>
			new TestRequestWithResponseHandler(() => processed = true));
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();
		var host = serviceProvider.GetRequiredService<IHostedService>();

		// Act
		await backgroundMediator.Enqueue<string>(new TestRequestWithResponse());

		// Start the background worker
		var cts = new CancellationTokenSource();
		await host.StartAsync(cts.Token);

		// Wait a bit for processing - need more time for reflection-based method invocation
		await Task.Delay(1500);

		// Assert
		Assert.True(processed, "Request with response should have been processed in background");

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

	[Fact]
	public async Task Enqueue_Notification_With_Multiple_Handlers_Should_Process_All()
	{
		// Arrange
		var handler1Processed = false;
		var handler2Processed = false;
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.AddConsole());
		services.AddCoordix();
		services.AddCoordixBackground();
		services.AddScoped<INotificationHandler<TestNotification>, TestNotificationHandler>(sp =>
			new TestNotificationHandler(() => handler1Processed = true));
		services.AddScoped<INotificationHandler<TestNotification>, TestNotificationHandler2>(sp =>
			new TestNotificationHandler2(() => handler2Processed = true));
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
		Assert.True(handler1Processed, "First handler should have been processed");
		Assert.True(handler2Processed, "Second handler should have been processed");

		cts.Cancel();
		await host.StopAsync(cts.Token);
	}

	[Fact]
	public async Task Enqueue_Request_With_Null_Should_Throw_ArgumentNullException()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddCoordix();
		services.AddCoordixBackground();
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await backgroundMediator.Enqueue((IRequest)null!));
	}

	[Fact]
	public async Task Enqueue_Request_With_Response_With_Null_Should_Throw_ArgumentNullException()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddCoordix();
		services.AddCoordixBackground();
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await backgroundMediator.Enqueue<string>((IRequest<string>)null!));
	}

	[Fact]
	public async Task Enqueue_Notification_With_Null_Should_Throw_ArgumentNullException()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddCoordix();
		services.AddCoordixBackground();
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await backgroundMediator.Enqueue((TestNotification)null!));
	}

	[Fact]
	public async Task Enqueue_With_CancellationToken_Should_Respect_Cancellation()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddCoordix();
		services.AddCoordixBackground();
		var serviceProvider = services.BuildServiceProvider();
		var backgroundMediator = serviceProvider.GetRequiredService<IBackgroundMediator>();

		// Act & Assert
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// TaskCanceledException is thrown when cancellation token is used
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await backgroundMediator.Enqueue(new TestRequest(), cts.Token));
	}

	[Fact]
	public void BackgroundMediator_Constructor_With_Null_ChannelWriter_Should_Throw_ArgumentNullException()
	{
		// Arrange
		var logger = new Mock<ILogger<BackgroundMediator>>().Object;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new BackgroundMediator(null!, logger));
	}

	[Fact]
	public void BackgroundMediator_Constructor_With_Null_Logger_Should_Throw_ArgumentNullException()
	{
		// Arrange
		var channel = Channel.CreateUnbounded<BackgroundJob>();
		var channelWriter = channel.Writer;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new BackgroundMediator(channelWriter, null!));
	}

	[Fact]
	public async Task EnqueueJob_When_Channel_Is_Closed_Should_Catch_And_Log_Error()
	{
		// Arrange
		var channel = Channel.CreateUnbounded<BackgroundJob>();
		var channelWriter = channel.Writer;
		channelWriter.Complete(); // Close the channel

		var loggerMock = new Mock<ILogger<BackgroundMediator>>();
		var mediator = new BackgroundMediator(channelWriter, loggerMock.Object);

		// Act & Assert
		// ChannelClosedException is thrown, but it's caught and wrapped/rethrown
		// The catch block should catch InvalidOperationException, but Channel throws ChannelClosedException
		// which inherits from InvalidOperationException, so it should be caught
		await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
			await mediator.Enqueue(new TestRequest()));

		// Verify error was logged - the catch block should catch it
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to enqueue background job")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}
}

// Test classes
public class TestRequest : IRequest { }
public class TestRequest2 : IRequest { }
public class TestRequestWithResponse : IRequest<string> { }

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

public class TestRequestWithResponseHandler : IRequestHandler<TestRequestWithResponse, string>
{
	private readonly Action _action;

	public TestRequestWithResponseHandler(Action? action = null)
	{
		_action = action ?? (() => { });
	}

	public Task<string> Handle(TestRequestWithResponse request, CancellationToken cancellationToken)
	{
		_action();
		return Task.FromResult("Response");
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

public class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
	private readonly Action _action;

	public TestNotificationHandler2(Action? action = null)
	{
		_action = action ?? (() => { });
	}

	public Task Handle(TestNotification notification, CancellationToken cancellationToken)
	{
		_action();
		return Task.CompletedTask;
	}
}
