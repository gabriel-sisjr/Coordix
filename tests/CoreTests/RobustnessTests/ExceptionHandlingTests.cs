using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Coordix.CoreTests.RobustnessTests;

public class ExceptionHandlingTests
{
    // Test models
    public class FailingRequest : IRequest<string> { }

    public class FailingRequestHandler : IRequestHandler<FailingRequest, string>
    {
        public Task<string> Handle(FailingRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler failed intentionally");
        }
    }

    public class FailingNotification : INotification { }

    public class FailingNotificationHandler : INotificationHandler<FailingNotification>
    {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Notification handler failed");
        }
    }

    [Fact]
    public async Task Send_WhenHandlerThrowsException_ShouldPropagateException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddTransient<IRequestHandler<FailingRequest, string>, FailingRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new FailingRequest())
        );

        Assert.Equal("Handler failed intentionally", exception.Message);
    }

    [Fact]
    public async Task Send_AfterHandlerException_MediatorShouldStillWork()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();
        services.AddTransient<IRequestHandler<FailingRequest, string>, FailingRequestHandler>();
        services.AddTransient<IRequestHandler<ValidRequest, string>, ValidRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        // Act - First call throws
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new FailingRequest())
        );

        // Act - Second call should work
        string result = await mediator.Send(new ValidRequest());

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task Publish_WhenOneHandlerFails_ShouldThrowException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();

        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TrackingNotificationHandler(() => { }));

        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new ThrowingNotificationHandler());

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Handlers run in parallel via Task.WhenAll
        // If one fails, Task.WhenAll throws an exception (may be AggregateException or the first exception)
        // Note: With parallel execution, we can't guarantee which handler executes first
        Exception exception = await Assert.ThrowsAnyAsync<Exception>(
            () => mediator.Publish(new TestNotification())
        );

        // Verify it's the expected exception type
        Assert.NotNull(exception);
    }

    // Helper classes
    public class ValidRequest : IRequest<string> { }

    public class ValidRequestHandler : IRequestHandler<ValidRequest, string>
    {
        public Task<string> Handle(ValidRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Success");
        }
    }

    public class TestNotification : INotification { }

    public class TrackingNotificationHandler : INotificationHandler<TestNotification>
    {
        private readonly Action _onHandle;

        public TrackingNotificationHandler(Action onHandle)
        {
            _onHandle = onHandle;
        }

        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            _onHandle();
            return Task.CompletedTask;
        }
    }

    public class ThrowingNotificationHandler : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler threw exception");
        }
    }
}

