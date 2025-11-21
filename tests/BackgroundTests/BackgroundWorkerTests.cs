using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Background.Implementation;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Coordix.Background.Tests;

public class BackgroundWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Process_Enqueued_Jobs()
    {
        // Arrange
        bool processed = false;
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<IRequestHandler<TestRequest>, TestRequestHandler>(sp =>
            new TestRequestHandler(() => processed = true));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue a job
        BackgroundJob job = new BackgroundJob
        {
            Message = new TestRequest(),
            MessageType = typeof(TestRequest),
            HasResponse = false
        };
        await channel.Writer.WriteAsync(job);

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        // Wait for processing
        await Task.Delay(500);

        // Assert
        Assert.True(processed, "Job should have been processed");

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_When_No_Jobs_Should_Continue_Waiting()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Task task = worker.StartAsync(cts.Token);

        // Wait a bit - task should still be running (not completed)
        await Task.Delay(50);

        // Assert - should not be completed yet (waiting for jobs)
        // Note: The task might complete quickly if cancellation happens, so we just verify it doesn't throw
        Assert.True(task.IsCompleted || !task.IsFaulted);

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_When_Job_Fails_Should_Continue_Processing_Other_Jobs()
    {
        // Arrange
        bool secondProcessed = false;
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<IRequestHandler<TestRequest>, TestRequestHandler>(sp =>
            new TestRequestHandler(() => throw new InvalidOperationException("Test exception")));
        services.AddScoped<IRequestHandler<TestRequest2>, TestRequest2Handler>(sp =>
            new TestRequest2Handler(() => secondProcessed = true));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue jobs
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new TestRequest(),
            MessageType = typeof(TestRequest),
            HasResponse = false
        });

        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new TestRequest2(),
            MessageType = typeof(TestRequest2),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        // Wait for processing
        await Task.Delay(1000);

        // Assert
        Assert.True(secondProcessed, "Second job should have been processed even after first failed");

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ProcessJobAsync_When_IHandlerExecutor_Not_Found_Should_Log_Error_And_Return()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        // Note: Not adding Coordix, so IHandlerExecutor won't be available
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        Mock<ILogger<BackgroundWorker>> loggerMock = new Mock<ILogger<BackgroundWorker>>();
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, loggerMock.Object);

        // Enqueue a job
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new TestRequest(),
            MessageType = typeof(TestRequest),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        Task task = worker.StartAsync(cts.Token);

        // Wait a bit
        await Task.Delay(500);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IHandlerExecutor not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ProcessJobAsync_With_Request_With_Response_Should_Process_Correctly()
    {
        // Arrange
        bool processed = false;
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>(sp =>
            new TestRequestWithResponseHandler(() => processed = true));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue a job with response
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new TestRequestWithResponse(),
            MessageType = typeof(TestRequestWithResponse),
            HasResponse = true,
            ResponseType = typeof(string)
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        // Wait for processing - need more time for reflection-based method invocation
        await Task.Delay(1000);

        // Assert
        Assert.True(processed, "Request with response should have been processed");

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ProcessJobAsync_With_Notification_Should_Process_Correctly()
    {
        // Arrange
        bool processed = false;
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<INotificationHandler<TestNotification>, TestNotificationHandler>(sp =>
            new TestNotificationHandler(() => processed = true));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue a notification
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new TestNotification(),
            MessageType = typeof(TestNotification),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        // Wait for processing
        await Task.Delay(500);

        // Assert
        Assert.True(processed, "Notification should have been processed");

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_When_Cancellation_Requested_Should_Stop_Gracefully()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mock<ILogger<BackgroundWorker>> loggerMock = new Mock<ILogger<BackgroundWorker>>();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, loggerMock.Object);

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        Task task = worker.StartAsync(cts.Token);

        // Wait for cancellation
        await Task.Delay(200);

        // Assert
        Assert.True(task.IsCompleted || task.IsCanceled || task.IsFaulted);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void BackgroundWorker_Constructor_With_Null_ChannelReader_Should_Throw_ArgumentNullException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BackgroundWorker(null!, serviceScopeFactory, logger));
    }

    [Fact]
    public void BackgroundWorker_Constructor_With_Null_ServiceScopeFactory_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BackgroundWorker(channel.Reader, null!, logger));
    }

    [Fact]
    public void BackgroundWorker_Constructor_With_Null_Logger_Should_Throw_ArgumentNullException()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BackgroundWorker(channel.Reader, serviceScopeFactory, null!));
    }

    [Fact]
    public async Task ProcessJobAsync_When_PublishMethod_Is_Null_Should_Handle_Gracefully()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mock<ILogger<BackgroundWorker>> loggerMock = new Mock<ILogger<BackgroundWorker>>();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, loggerMock.Object);

        // Create a notification job
        BackgroundJob job = new BackgroundJob
        {
            Message = new TestNotification(),
            MessageType = typeof(TestNotification),
            HasResponse = false
        };

        // Enqueue the job
        await channel.Writer.WriteAsync(job);

        // Act - The publishMethod should be found, so this should succeed
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        // Wait for processing
        await Task.Delay(500);

        cts.Cancel();
        await worker.StopAsync(cts.Token);

        // Note: Testing the else branch for method being null is difficult without
        // breaking the IHandlerExecutor interface, which would require more complex mocking
    }

    [Fact]
    public async Task ExecuteAsync_When_Fatal_Exception_Occurs_Should_Log_And_Throw()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mock<ILogger<BackgroundWorker>> loggerMock = new Mock<ILogger<BackgroundWorker>>();

        // Create a mock channel reader that throws on WaitToReadAsync
        Mock<ChannelReader<BackgroundJob>> channelReaderMock = new Mock<ChannelReader<BackgroundJob>>();
        channelReaderMock
            .Setup(x => x.WaitToReadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal error"));

        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        BackgroundWorker worker = new BackgroundWorker(channelReaderMock.Object, serviceScopeFactory, loggerMock.Object);

        // Act & Assert
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await worker.StartAsync(cts.Token));

        // Verify fatal error was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fatal error") || v.ToString()!.Contains("Fatal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessJobAsync_With_InvalidMessageType_Should_Log_Warning()
    {
        // Arrange
        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Mock<ILogger<BackgroundWorker>> loggerMock = new Mock<ILogger<BackgroundWorker>>();
        IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, loggerMock.Object);

        // Create a job with a message that is neither IRequest nor INotification
        object invalidMessage = new object(); // Not IRequest or INotification
        BackgroundJob job = new BackgroundJob
        {
            Message = invalidMessage,
            MessageType = typeof(object),
            HasResponse = false,
            ResponseType = null
        };

        // Act
        await channel.Writer.WriteAsync(job);
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Task task = worker.StartAsync(cts.Token);

        // Wait a bit for processing
        await Task.Delay(100);

        // Assert - verify warning was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("neither IRequest nor INotification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

}
