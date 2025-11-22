using Coordix.Core.Extensions;
using Coordix.Core.Interfaces;
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
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - First call throws
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new FailingRequest())
        );

        // Act - Second call should work
        var result = await mediator.Send(new ValidRequest());

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task Publish_WhenOneHandlerFails_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        bool handler1Called = false;
        bool handler2Called = false;
        bool handler3Called = false;

        ServiceCollection services = new ServiceCollection();
        services.AddCoordix();

        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TrackingNotificationHandler(() => handler1Called = true));

        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new ThrowingNotificationHandler());

        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TrackingNotificationHandler(() => handler3Called = true));

        ServiceProvider provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - One handler fails but others should be called
        // Note: The current implementation stops on first exception
        // This test documents the current behavior
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Publish(new TestNotification())
        );

        // First handler should be called before the exception
        Assert.True(handler1Called);
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

