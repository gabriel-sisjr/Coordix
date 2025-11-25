using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Coordix.Tests.EdgeCases;

/// <summary>
/// Tests for edge cases and boundary conditions that might not be covered elsewhere.
/// </summary>
public class EdgeCaseTests
{
    #region Large Payload Tests

    public class LargePayloadRequest : IRequest<string>
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public class LargePayloadHandler : IRequestHandler<LargePayloadRequest, string>
    {
        public Task<string> Handle(LargePayloadRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Processed {request.Data.Length} bytes");
        }
    }

    [Fact]
    public async Task Send_WithLargePayload_HandlesSuccessfully()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddTransient<IRequestHandler<LargePayloadRequest, string>, LargePayloadHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        var largeData = new byte[10 * 1024 * 1024]; // 10MB
        LargePayloadRequest request = new LargePayloadRequest { Data = largeData };

        // Act
        var result = await mediator.Send(request);

        // Assert
        Assert.Contains("10485760 bytes", result);
    }

    #endregion

    #region Concurrent Execution Tests

    public class ConcurrentRequest : IRequest<int>
    {
        public int Value { get; set; }
    }

    public class ConcurrentRequestHandler : IRequestHandler<ConcurrentRequest, int>
    {
        private static int _counter = 0;

        public async Task<int> Handle(ConcurrentRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            return Interlocked.Increment(ref _counter);
        }
    }

    [Fact]
    public async Task Send_ConcurrentRequests_HandlesAllSuccessfully()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddTransient<IRequestHandler<ConcurrentRequest, int>, ConcurrentRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        List<ConcurrentRequest> requests = Enumerable.Range(0, 100)
            .Select(i => new ConcurrentRequest { Value = i })
            .ToList();

        // Act
        List<Task<int>> tasks = requests.Select(r => mediator.Send(r)).ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Length);
        Assert.Equal(100, results.Distinct().Count()); // All results should be unique
    }

    #endregion

    #region Handler Lifetime Tests

    public class DisposableHandler : IRequestHandler<TestRequest, string>, IDisposable
    {
        public static int DisposeCount = 0;
        public bool IsDisposed { get; private set; }

        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(DisposableHandler));
            }

            return Task.FromResult("Success");
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Interlocked.Increment(ref DisposeCount);
            }
        }
    }

    public class TestRequest : IRequest<string> { }

    #endregion

    #region Null/Empty Handler Tests

    public class NullReturningRequest : IRequest<string?> { }

    public class NullReturningHandler : IRequestHandler<NullReturningRequest, string?>
    {
        public Task<string?> Handle(NullReturningRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }
    }

    [Fact]
    public async Task Send_HandlerReturnsNull_DoesNotThrow()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddTransient<IRequestHandler<NullReturningRequest, string?>, NullReturningHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new NullReturningRequest());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Notification Order Tests

    public class OrderedNotification : INotification { }

    public class OrderTrackingHandler : INotificationHandler<OrderedNotification>
    {
        private static readonly List<int> _executionOrder = new();
        private readonly int _handlerId;

        public OrderTrackingHandler(int handlerId)
        {
            _handlerId = handlerId;
        }

        public async Task Handle(OrderedNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(10 - _handlerId); // Handlers finish in reverse order
            lock (_executionOrder)
            {
                _executionOrder.Add(_handlerId);
            }
        }

        public static List<int> GetExecutionOrder()
        {
            lock (_executionOrder)
            {
                return new List<int>(_executionOrder);
            }
        }

        public static void ResetOrder()
        {
            lock (_executionOrder)
            {
                _executionOrder.Clear();
            }
        }
    }

    #endregion

    #region Memory Pressure Tests

    [Fact]
    public async Task Send_ManySequentialRequests_DoesNotLeakMemory()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddTransient<IRequestHandler<TestRequest, string>, SimpleHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        var initialMemory = GC.GetTotalMemory(true);

        // Act - Send 10000 requests
        for (int i = 0; i < 10000; i++)
        {
            await mediator.Send(new TestRequest());
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Assert - Memory growth should be reasonable (less than 10MB for 10k requests)
        Assert.True(memoryGrowth < 10 * 1024 * 1024,
            $"Memory grew by {memoryGrowth / 1024 / 1024}MB, which exceeds threshold");
    }

    #endregion

    #region Helper Classes

    public class SimpleHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Success");
        }
    }

    #endregion
}
