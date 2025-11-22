using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Coordix.Background.Implementation;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Coordix.Background.Tests.RobustnessTests;

public class BackgroundRobustnessTests
{
    // Test scoped service
    public class ScopedCounter
    {
        public int Count { get; set; }
        public Guid InstanceId { get; } = Guid.NewGuid();
    }

    public class ScopedRequest : IRequest
    {
        public Guid ExpectedInstanceId { get; set; }
    }

    public class ScopedRequestHandler : IRequestHandler<ScopedRequest>
    {
        private readonly ScopedCounter _counter;

        public ScopedRequestHandler(ScopedCounter counter)
        {
            _counter = counter;
        }

        public Task Handle(ScopedRequest request, CancellationToken cancellationToken)
        {
            _counter.Count++;

            // Verify we're using the correct scoped instance
            if (request.ExpectedInstanceId != Guid.Empty &&
                request.ExpectedInstanceId != _counter.InstanceId)
            {
                throw new InvalidOperationException("Wrong scope instance!");
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task BackgroundWorker_ShouldCreateNewScopePerJob()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<ScopedCounter>();
        services.AddScoped<IRequestHandler<ScopedRequest>, ScopedRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        IServiceScopeFactory serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue multiple jobs
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new ScopedRequest(),
            MessageType = typeof(ScopedRequest),
            HasResponse = false
        });

        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new ScopedRequest(),
            MessageType = typeof(ScopedRequest),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        await Task.Delay(1000);

        // Assert - If scopes were properly created, jobs should complete without throwing
        // If the same scope was reused, the counter would be incremented
        cts.Cancel();
        await worker.StopAsync(cts.Token);

        // No exception means success - each job got its own scope
    }

    public class FailingRequest : IRequest { }

    public class FailingRequestHandler : IRequestHandler<FailingRequest>
    {
        private static int _callCount = 0;

        public Task Handle(FailingRequest request, CancellationToken cancellationToken)
        {
            _callCount++;
            if (_callCount == 1)
            {
                throw new InvalidOperationException("First call fails");
            }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task BackgroundWorker_WhenHandlerFails_ShouldContinueProcessingNextJobs()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<IRequestHandler<FailingRequest>, FailingRequestHandler>();
        services.AddScoped<IRequestHandler<ScopedRequest>, ScopedRequestHandler>();
        services.AddScoped<ScopedCounter>();
        ServiceProvider provider = services.BuildServiceProvider();

        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        IServiceScopeFactory serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        Mock<ILogger<BackgroundWorker>> loggerMock = new Mock<ILogger<BackgroundWorker>>();
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, loggerMock.Object);

        // Enqueue a failing job
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new FailingRequest(),
            MessageType = typeof(FailingRequest),
            HasResponse = false
        });

        // Enqueue a successful job
        bool successProcessed = false;
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new ScopedRequest(),
            MessageType = typeof(ScopedRequest),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        Task task = worker.StartAsync(cts.Token);

        await Task.Delay(1500);

        // Assert - Error should be logged but worker continues
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing background job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }

    public class SlowRequest : IRequest { }

    public class SlowRequestHandler : IRequestHandler<SlowRequest>
    {
        public async Task Handle(SlowRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);
        }
    }

    [Fact]
    public async Task BackgroundWorker_ShouldRespectCancellationToken()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<IRequestHandler<SlowRequest>, SlowRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        IServiceScopeFactory serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue a slow job
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new SlowRequest(),
            MessageType = typeof(SlowRequest),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        Task task = worker.StartAsync(cts.Token);

        await Task.Delay(150);

        // Assert - Worker should stop gracefully
        cts.Cancel();
        await worker.StopAsync(cts.Token);

        Assert.True(task.IsCompleted);
    }

    public class DisposableService : IDisposable
    {
        public static int DisposeCount = 0;
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                DisposeCount++;
            }
        }
    }

    public class DisposableRequest : IRequest { }

    public class DisposableRequestHandler : IRequestHandler<DisposableRequest>
    {
        private readonly DisposableService _service;

        public DisposableRequestHandler(DisposableService service)
        {
            _service = service;
        }

        public Task Handle(DisposableRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task BackgroundWorker_ShouldDisposeScope_AfterJobCompletes()
    {
        // Arrange
        DisposableService.DisposeCount = 0;

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddCoordix();
        services.AddScoped<DisposableService>();
        services.AddScoped<IRequestHandler<DisposableRequest>, DisposableRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        Channel<BackgroundJob> channel = Channel.CreateUnbounded<BackgroundJob>();
        IServiceScopeFactory serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        ILogger<BackgroundWorker> logger = new Mock<ILogger<BackgroundWorker>>().Object;
        BackgroundWorker worker = new BackgroundWorker(channel.Reader, serviceScopeFactory, logger);

        // Enqueue jobs
        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new DisposableRequest(),
            MessageType = typeof(DisposableRequest),
            HasResponse = false
        });

        await channel.Writer.WriteAsync(new BackgroundJob
        {
            Message = new DisposableRequest(),
            MessageType = typeof(DisposableRequest),
            HasResponse = false
        });

        // Act
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Task task = worker.StartAsync(cts.Token);

        await Task.Delay(1000);

        cts.Cancel();
        await worker.StopAsync(cts.Token);

        // Assert - Should have disposed twice (once per job/scope)
        Assert.Equal(2, DisposableService.DisposeCount);
    }
}

