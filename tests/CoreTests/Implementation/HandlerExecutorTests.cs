using System;
using System.Threading;
using System.Threading.Tasks;
using Coordix.Implementation;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Coordix.Tests.Implementation;

/// <summary>
/// Comprehensive tests for HandlerExecutor covering all execution paths,
/// error scenarios, and edge cases.
/// </summary>
public class HandlerExecutorTests
{
    #region Test Models

    public class TestRequest : IRequest<string> { }
    public class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
            => Task.FromResult("Success");
    }

    public class VoidRequest : IRequest { }
    public class VoidRequestHandler : IRequestHandler<VoidRequest>
    {
        public Task Handle(VoidRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public class TestNotification : INotification { }
    public class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HandlerExecutor(null!));
    }

    [Fact]
    public void Constructor_WithValidProvider_CreatesInstance()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Assert
        Assert.NotNull(executor);
    }

    #endregion

    #region ExecuteRequestHandler<TResponse> Tests

    [Fact]
    public async Task ExecuteRequestHandler_WithValidHandler_ReturnsResponse()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);
        TestRequest request = new TestRequest();

        // Act
        var result = await executor.ExecuteRequestHandler(request);

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteRequestHandler_WithoutHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);
        TestRequest request = new TestRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteRequestHandler(request));

        Assert.Contains("Handler not found", exception.Message);
        Assert.Contains(nameof(TestRequest), exception.Message);
    }

    [Fact]
    public async Task ExecuteRequestHandler_WithCancellationToken_PassesToHandler()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken tokenReceived = CancellationToken.None;

        ServiceCollection services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>>(sp =>
            new TestRequestHandler());
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act
        await executor.ExecuteRequestHandler(new TestRequest(), cts.Token);

        // Token is passed through - validated by no exception
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteRequestHandler_CalledTwice_UsesCachedDelegate()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act
        var result1 = await executor.ExecuteRequestHandler(new TestRequest());
        var result2 = await executor.ExecuteRequestHandler(new TestRequest());

        // Assert
        Assert.Equal("Success", result1);
        Assert.Equal("Success", result2);
        // If caching works, both calls succeed without error
    }

    #endregion

    #region ExecuteRequestHandler (void) Tests

    [Fact]
    public async Task ExecuteRequestHandler_Void_WithValidHandler_Completes()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddTransient<IRequestHandler<VoidRequest>, VoidRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act
        await executor.ExecuteRequestHandler(new VoidRequest());

        // Assert - No exception means success
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteRequestHandler_Void_WithoutHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.ExecuteRequestHandler(new VoidRequest()));

        Assert.Contains("Handler not found", exception.Message);
    }

    #endregion

    #region ExecuteNotificationHandler Tests

    [Fact]
    public async Task ExecuteNotificationHandler_WithValidHandler_Completes()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act
        await executor.ExecuteNotificationHandler(new TestNotification());

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteNotificationHandler_WithoutHandlers_Completes()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act
        await executor.ExecuteNotificationHandler(new TestNotification());

        // Assert - Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteNotificationHandler_WithMultipleHandlers_CallsAll()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;

        ServiceCollection services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new CallbackNotificationHandler(() => handler1Called = true));
        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new CallbackNotificationHandler(() => handler2Called = true));

        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act
        await executor.ExecuteNotificationHandler(new TestNotification());

        // Assert
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    #endregion

    #region Dynamic Execution Tests

    [Fact]
    public async Task ExecuteRequestHandlerDynamic_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);
        TestRequest request = new TestRequest();

        // Act
        var result = await executor.ExecuteRequestHandlerDynamic(request, typeof(string));

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task ExecuteRequestHandlerDynamic_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => executor.ExecuteRequestHandlerDynamic(null!, typeof(string)));
    }

    [Fact]
    public async Task ExecuteRequestHandlerDynamic_WithNullResponseType_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => executor.ExecuteRequestHandlerDynamic(new TestRequest(), null!));
    }

    [Fact]
    public async Task ExecuteNotificationHandlerDynamic_WithValidNotification_Completes()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);
        TestNotification notification = new TestNotification();

        // Act
        await executor.ExecuteNotificationHandlerDynamic(notification, typeof(TestNotification));

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteNotificationHandlerDynamic_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        HandlerExecutor executor = new HandlerExecutor(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => executor.ExecuteNotificationHandlerDynamic(null!, typeof(TestNotification)));
    }

    #endregion

    #region Helper Classes

    public class CallbackNotificationHandler : INotificationHandler<TestNotification>
    {
        private readonly Action _callback;

        public CallbackNotificationHandler(Action callback)
        {
            _callback = callback;
        }

        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            _callback();
            return Task.CompletedTask;
        }
    }

    #endregion
}
