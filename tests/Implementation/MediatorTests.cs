using Coordix.Implementation;
using Coordix.Interfaces;
using Coordix.Tests.Samples;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Coordix.Tests.Implementation;

public sealed class MediatorTests
{
    [Fact]
    public async Task Send_WithHandler_ReturnsResponse()
    {
        var req = new TestRequest();
        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(req, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello!");

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        var result = await mediator.Send(req);

        Assert.Equal("Hello!", result);
        handlerMock.Verify(h => h.Handle(req, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_Generic_WhenNoHandler_Throws()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var mediator = new Mediator(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new TestRequest()));
        Assert.Contains(nameof(TestRequest), ex.Message);
    }

    [Fact]
    public async Task Send_Void_WithHandler_Completes()
    {
        var req = new VoidRequest();
        var handlerMock = new Mock<IRequestHandler<VoidRequest>>();
        handlerMock
            .Setup(h => h.Handle(req, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await mediator.Send(req);

        handlerMock.Verify(h => h.Handle(req, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_Void_WhenNoHandler_Throws()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var mediator = new Mediator(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new VoidRequest()));
        Assert.Contains(nameof(VoidRequest), ex.Message);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        var notification = new TestNotification();
        var provider = new ServiceCollection().BuildServiceProvider();
        var mediator = new Mediator(provider);

        await mediator.Publish(notification);
    }

    [Fact]
    public async Task Publish_WithMultipleHandlers_InvokesAll()
    {
        var notification = new TestNotification();

        var handlerMock1 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock1
            .Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var handlerMock2 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock2
            .Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock1.Object);
        services.AddSingleton(handlerMock2.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await mediator.Publish(notification);

        handlerMock1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        handlerMock2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_HandlerThrows_ExceptionPropagates()
    {
        var req = new TestRequest();
        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(req, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var mediator = new Mediator(services.BuildServiceProvider());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(req));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Send_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var req = new TestRequest();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(req, It.Is<CancellationToken>(ct => ct == cts.Token)))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var mediator = new Mediator(services.BuildServiceProvider());

        await Assert.ThrowsAsync<OperationCanceledException>(() => mediator.Send(req, cts.Token));

        handlerMock.Verify(h => h.Handle(req, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Publish_HandlerThrows_ExceptionIsPropagated()
    {
        var notification = new TestNotification();
        var handlerMock = new Mock<INotificationHandler<TestNotification>>();
        handlerMock
            .Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("notify failed"));

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var mediator = new Mediator(services.BuildServiceProvider());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Publish(notification));
        Assert.Equal("notify failed", ex.Message);
    }

    [Fact]
    public async Task Publish_PassesCancellationTokenToAllHandlers()
    {
        var notification = new TestNotification();
        var cts = new CancellationTokenSource();

        var handlerMock1 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock1
            .Setup(h => h.Handle(notification, It.Is<CancellationToken>(ct => ct == cts.Token)))
            .Returns(Task.CompletedTask);

        var handlerMock2 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock2
            .Setup(h => h.Handle(notification, It.Is<CancellationToken>(ct => ct == cts.Token)))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock1.Object);
        services.AddSingleton(handlerMock2.Object);
        var mediator = new Mediator(services.BuildServiceProvider());

        await mediator.Publish(notification, cts.Token);

        handlerMock1.Verify(h => h.Handle(notification, cts.Token), Times.Once);
        handlerMock2.Verify(h => h.Handle(notification, cts.Token), Times.Once);
    }
}
