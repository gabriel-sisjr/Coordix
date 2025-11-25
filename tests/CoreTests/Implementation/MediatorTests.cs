using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Coordix.Implementation;
using Coordix.Interfaces;
using Coordix.Tests.Samples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Coordix.Tests.Implementation;

public sealed class MediatorTests
{
    [Fact]
    public async Task Send_WithHandler_ReturnsResponse()
    {
        TestRequest req = new TestRequest();
        Mock<IRequestHandler<TestRequest, string>> handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
                .Setup(h => h.Handle(req, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Hello!");

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        string result = await mediator.Send(req);

        Assert.Equal("Hello!", result);
        handlerMock.Verify(h => h.Handle(req, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_Generic_WhenNoHandler_Throws()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new TestRequest()));
        Assert.Contains(nameof(TestRequest), ex.Message);
    }

    [Fact]
    public async Task Send_Void_WithHandler_Completes()
    {
        VoidRequest req = new VoidRequest();
        Mock<IRequestHandler<VoidRequest>> handlerMock = new Mock<IRequestHandler<VoidRequest>>();
        handlerMock
                .Setup(h => h.Handle(req, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        await mediator.Send(req);

        handlerMock.Verify(h => h.Handle(req, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_Void_WhenNoHandler_Throws()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new VoidRequest()));
        Assert.Contains(nameof(VoidRequest), ex.Message);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        TestNotification notification = new TestNotification();
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        await mediator.Publish(notification);
    }

    [Fact]
    public async Task Publish_WithMultipleHandlers_InvokesAll()
    {
        TestNotification notification = new TestNotification();

        Mock<INotificationHandler<TestNotification>> handlerMock1 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock1
                .Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

        Mock<INotificationHandler<TestNotification>> handlerMock2 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock2
                .Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock1.Object);
        services.AddSingleton(handlerMock2.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        await mediator.Publish(notification);

        handlerMock1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        handlerMock2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_HandlerThrows_ExceptionPropagates()
    {
        TestRequest req = new TestRequest();
        Mock<IRequestHandler<TestRequest, string>> handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
                .Setup(h => h.Handle(req, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(req));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Send_WithCancelledToken_ThrowsOperationCanceledException()
    {
        TestRequest req = new TestRequest();
        CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Mock<IRequestHandler<TestRequest, string>> handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
                .Setup(h => h.Handle(req, It.Is<CancellationToken>(ct => ct == cts.Token)))
                .ThrowsAsync(new OperationCanceledException(cts.Token));

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        await Assert.ThrowsAsync<OperationCanceledException>(() => mediator.Send(req, cts.Token));

        handlerMock.Verify(h => h.Handle(req, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Publish_HandlerThrows_ExceptionIsPropagated()
    {
        TestNotification notification = new TestNotification();
        Mock<INotificationHandler<TestNotification>> handlerMock = new Mock<INotificationHandler<TestNotification>>();
        handlerMock
                .Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("notify failed"));

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Publish(notification));
        Assert.Equal("notify failed", ex.Message);
    }

    [Fact]
    public async Task Publish_PassesCancellationTokenToAllHandlers()
    {
        TestNotification notification = new TestNotification();
        CancellationTokenSource cts = new CancellationTokenSource();

        Mock<INotificationHandler<TestNotification>> handlerMock1 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock1
                .Setup(h => h.Handle(notification, It.Is<CancellationToken>(ct => ct == cts.Token)))
                .Returns(Task.CompletedTask);

        Mock<INotificationHandler<TestNotification>> handlerMock2 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock2
                .Setup(h => h.Handle(notification, It.Is<CancellationToken>(ct => ct == cts.Token)))
                .Returns(Task.CompletedTask);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock1.Object);
        services.AddSingleton(handlerMock2.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        await mediator.Publish(notification, cts.Token);

        handlerMock1.Verify(h => h.Handle(notification, cts.Token), Times.Once);
        handlerMock2.Verify(h => h.Handle(notification, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Send_MultipleInvocations_ReusesCachedDelegate()
    {
        TestRequest req1 = new TestRequest();
        TestRequest req2 = new TestRequest();
        Mock<IRequestHandler<TestRequest, string>> handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
                .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response");

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        // First invocation - should create and cache delegate
        await mediator.Send(req1);

        // Get cache count before second invocation
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        int cacheBefore = GetRequestHandlerWithResponseDelegateCacheCount(handlerType);

        // Second invocation - should reuse cached delegate
        await mediator.Send(req2);

        // Get cache count after second invocation
        int cacheAfter = GetRequestHandlerWithResponseDelegateCacheCount(handlerType);

        // Cache count should remain the same (delegate reused)
        Assert.Equal(cacheBefore, cacheAfter);
        Assert.Equal(1, cacheAfter); // Only one entry for this handler type
        handlerMock.Verify(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Send_Void_MultipleInvocations_ReusesCachedDelegate()
    {
        VoidRequest req1 = new VoidRequest();
        VoidRequest req2 = new VoidRequest();
        Mock<IRequestHandler<VoidRequest>> handlerMock = new Mock<IRequestHandler<VoidRequest>>();
        handlerMock
                .Setup(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        // First invocation
        await mediator.Send(req1);

        Type handlerType = typeof(IRequestHandler<>).MakeGenericType(typeof(VoidRequest));
        int cacheBefore = GetRequestHandlerDelegateCacheCount(handlerType);

        // Second invocation - should reuse cached delegate
        await mediator.Send(req2);

        int cacheAfter = GetRequestHandlerDelegateCacheCount(handlerType);

        Assert.Equal(cacheBefore, cacheAfter);
        Assert.Equal(1, cacheAfter);
        handlerMock.Verify(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Publish_MultipleInvocations_ReusesCachedDelegate()
    {
        TestNotification notification1 = new TestNotification();
        TestNotification notification2 = new TestNotification();
        Mock<INotificationHandler<TestNotification>> handlerMock = new Mock<INotificationHandler<TestNotification>>();
        handlerMock
                .Setup(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        // First invocation
        await mediator.Publish(notification1);

        Type handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TestNotification));
        int cacheBefore = GetNotificationHandlerDelegateCacheCount(handlerType);

        // Second invocation - should reuse cached delegate
        await mediator.Publish(notification2);

        int cacheAfter = GetNotificationHandlerDelegateCacheCount(handlerType);

        Assert.Equal(cacheBefore, cacheAfter);
        Assert.Equal(1, cacheAfter);
        handlerMock.Verify(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Send_DifferentHandlerTypes_CreatesSeparateCacheEntries()
    {
        TestRequest req1 = new TestRequest();
        VoidRequest req2 = new VoidRequest();

        Mock<IRequestHandler<TestRequest, string>> handlerMock1 = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock1.Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response1");

        Mock<IRequestHandler<VoidRequest>> handlerMock2 = new Mock<IRequestHandler<VoidRequest>>();
        handlerMock2.Setup(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock1.Object);
        services.AddSingleton(handlerMock2.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        await mediator.Send(req1);
        await mediator.Send(req2);

        Type handlerType1 = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        Type handlerType2 = typeof(IRequestHandler<>).MakeGenericType(typeof(VoidRequest));

        int cache1Count = GetRequestHandlerWithResponseDelegateCacheCount(handlerType1);
        int cache2Count = GetRequestHandlerDelegateCacheCount(handlerType2);

        // Both should have cache entries
        Assert.Equal(1, cache1Count);
        Assert.Equal(1, cache2Count);
    }

    [Fact]
    public async Task Send_MethodInfoIsCached_AfterFirstInvocation()
    {
        TestRequest req = new TestRequest();
        Mock<IRequestHandler<TestRequest, string>> handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
                .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response");

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        // First invocation - should cache MethodInfo
        await mediator.Send(req);

        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        int methodInfoCacheCount = GetMethodInfoCacheCount(handlerType);

        // MethodInfo should be cached
        Assert.Equal(1, methodInfoCacheCount);

        // Second invocation - should reuse cached MethodInfo
        await mediator.Send(req);

        int methodInfoCacheCountAfter = GetMethodInfoCacheCount(handlerType);

        // Cache count should remain the same
        Assert.Equal(methodInfoCacheCount, methodInfoCacheCountAfter);
    }

    [Fact]
    public async Task Publish_MethodInfoIsCached_AfterFirstInvocation()
    {
        TestNotification notification = new TestNotification();
        Mock<INotificationHandler<TestNotification>> handlerMock = new Mock<INotificationHandler<TestNotification>>();
        handlerMock
                .Setup(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        // First invocation
        await mediator.Publish(notification);

        Type handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TestNotification));
        int methodInfoCacheCount = GetMethodInfoCacheCount(handlerType);

        Assert.Equal(1, methodInfoCacheCount);

        // Second invocation
        await mediator.Publish(notification);

        int methodInfoCacheCountAfter = GetMethodInfoCacheCount(handlerType);

        Assert.Equal(methodInfoCacheCount, methodInfoCacheCountAfter);
    }

    [Fact]
    public async Task Send_ConcurrentInvocations_ThreadSafeCache()
    {
        TestRequest req = new TestRequest();
        Mock<IRequestHandler<TestRequest, string>> handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
                .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response");

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        services.AddSingleton<IHandlerExecutor, HandlerExecutor>();
        ServiceProvider provider = services.BuildServiceProvider();
        IHandlerExecutor handlerExecutor = provider.GetRequiredService<IHandlerExecutor>();
        Mediator mediator = new Mediator(handlerExecutor);

        // Concurrent invocations
        Task<string>[] tasks = Enumerable.Range(0, 10)
                .Select(_ => mediator.Send(req))
                .ToArray();

        await Task.WhenAll(tasks);

        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        int cacheCount = GetRequestHandlerWithResponseDelegateCacheCount(handlerType);

        // Should have only one cache entry despite concurrent access
        Assert.Equal(1, cacheCount);
        handlerMock.Verify(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }

    // Helper methods to access private static cache fields via reflection
    // Note: Caches are now in HandlerExecutor, not Mediator
    private static int GetMethodInfoCacheCount(Type handlerType)
    {
        FieldInfo? field = typeof(HandlerExecutor).GetField("_methodInfoCache", BindingFlags.NonPublic | BindingFlags.Static);
        ConcurrentDictionary<Type, MethodInfo> cache = (ConcurrentDictionary<Type, MethodInfo>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }

    private static int GetRequestHandlerDelegateCacheCount(Type handlerType)
    {
        FieldInfo? field = typeof(HandlerExecutor).GetField("_requestHandlerDelegates", BindingFlags.NonPublic | BindingFlags.Static);
        ConcurrentDictionary<Type, Func<object, IRequest, CancellationToken, Task>> cache = (ConcurrentDictionary<Type, Func<object, IRequest, CancellationToken, Task>>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }

    private static int GetRequestHandlerWithResponseDelegateCacheCount(Type handlerType)
    {
        FieldInfo? field = typeof(HandlerExecutor).GetField("_requestHandlerWithResponseDelegates", BindingFlags.NonPublic | BindingFlags.Static);
        ConcurrentDictionary<Type, Delegate> cache = (ConcurrentDictionary<Type, Delegate>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }

    private static int GetNotificationHandlerDelegateCacheCount(Type handlerType)
    {
        FieldInfo? field = typeof(HandlerExecutor).GetField("_notificationHandlerDelegates", BindingFlags.NonPublic | BindingFlags.Static);
        ConcurrentDictionary<Type, Func<object, INotification, CancellationToken, Task>> cache = (ConcurrentDictionary<Type, Func<object, INotification, CancellationToken, Task>>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }
}
