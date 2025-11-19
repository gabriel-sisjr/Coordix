using Coordix.Implementation;
using Coordix.Interfaces;
using Coordix.Tests.Samples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

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

    [Fact]
    public async Task Send_MultipleInvocations_ReusesCachedDelegate()
    {
        var req1 = new TestRequest();
        var req2 = new TestRequest();
        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response");

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        // First invocation - should create and cache delegate
        await mediator.Send(req1);
        
        // Get cache count before second invocation
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        var cacheBefore = GetRequestHandlerWithResponseDelegateCacheCount(handlerType);

        // Second invocation - should reuse cached delegate
        await mediator.Send(req2);
        
        // Get cache count after second invocation
        var cacheAfter = GetRequestHandlerWithResponseDelegateCacheCount(handlerType);

        // Cache count should remain the same (delegate reused)
        Assert.Equal(cacheBefore, cacheAfter);
        Assert.Equal(1, cacheAfter); // Only one entry for this handler type
        handlerMock.Verify(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Send_Void_MultipleInvocations_ReusesCachedDelegate()
    {
        var req1 = new VoidRequest();
        var req2 = new VoidRequest();
        var handlerMock = new Mock<IRequestHandler<VoidRequest>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        // First invocation
        await mediator.Send(req1);
        
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(typeof(VoidRequest));
        var cacheBefore = GetRequestHandlerDelegateCacheCount(handlerType);

        // Second invocation - should reuse cached delegate
        await mediator.Send(req2);
        
        var cacheAfter = GetRequestHandlerDelegateCacheCount(handlerType);

        Assert.Equal(cacheBefore, cacheAfter);
        Assert.Equal(1, cacheAfter);
        handlerMock.Verify(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Publish_MultipleInvocations_ReusesCachedDelegate()
    {
        var notification1 = new TestNotification();
        var notification2 = new TestNotification();
        var handlerMock = new Mock<INotificationHandler<TestNotification>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        // First invocation
        await mediator.Publish(notification1);
        
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TestNotification));
        var cacheBefore = GetNotificationHandlerDelegateCacheCount(handlerType);

        // Second invocation - should reuse cached delegate
        await mediator.Publish(notification2);
        
        var cacheAfter = GetNotificationHandlerDelegateCacheCount(handlerType);

        Assert.Equal(cacheBefore, cacheAfter);
        Assert.Equal(1, cacheAfter);
        handlerMock.Verify(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Send_DifferentHandlerTypes_CreatesSeparateCacheEntries()
    {
        var req1 = new TestRequest();
        var req2 = new VoidRequest();
        
        var handlerMock1 = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock1.Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response1");

        var handlerMock2 = new Mock<IRequestHandler<VoidRequest>>();
        handlerMock2.Setup(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock1.Object);
        services.AddSingleton(handlerMock2.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await mediator.Send(req1);
        await mediator.Send(req2);

        var handlerType1 = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        var handlerType2 = typeof(IRequestHandler<>).MakeGenericType(typeof(VoidRequest));

        var cache1Count = GetRequestHandlerWithResponseDelegateCacheCount(handlerType1);
        var cache2Count = GetRequestHandlerDelegateCacheCount(handlerType2);

        // Both should have cache entries
        Assert.Equal(1, cache1Count);
        Assert.Equal(1, cache2Count);
    }

    [Fact]
    public async Task Send_MethodInfoIsCached_AfterFirstInvocation()
    {
        var req = new TestRequest();
        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response");

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        // First invocation - should cache MethodInfo
        await mediator.Send(req);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        var methodInfoCacheCount = GetMethodInfoCacheCount(handlerType);

        // MethodInfo should be cached
        Assert.Equal(1, methodInfoCacheCount);

        // Second invocation - should reuse cached MethodInfo
        await mediator.Send(req);
        
        var methodInfoCacheCountAfter = GetMethodInfoCacheCount(handlerType);

        // Cache count should remain the same
        Assert.Equal(methodInfoCacheCount, methodInfoCacheCountAfter);
    }

    [Fact]
    public async Task Publish_MethodInfoIsCached_AfterFirstInvocation()
    {
        var notification = new TestNotification();
        var handlerMock = new Mock<INotificationHandler<TestNotification>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        // First invocation
        await mediator.Publish(notification);

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TestNotification));
        var methodInfoCacheCount = GetMethodInfoCacheCount(handlerType);

        Assert.Equal(1, methodInfoCacheCount);

        // Second invocation
        await mediator.Publish(notification);
        
        var methodInfoCacheCountAfter = GetMethodInfoCacheCount(handlerType);

        Assert.Equal(methodInfoCacheCount, methodInfoCacheCountAfter);
    }

    [Fact]
    public async Task Send_ConcurrentInvocations_ThreadSafeCache()
    {
        var req = new TestRequest();
        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response");

        var services = new ServiceCollection();
        services.AddSingleton(handlerMock.Object);
        var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        // Concurrent invocations
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => mediator.Send(req))
            .ToArray();

        await Task.WhenAll(tasks);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TestRequest), typeof(string));
        var cacheCount = GetRequestHandlerWithResponseDelegateCacheCount(handlerType);

        // Should have only one cache entry despite concurrent access
        Assert.Equal(1, cacheCount);
        handlerMock.Verify(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }

    // Helper methods to access private static cache fields via reflection
    private static int GetMethodInfoCacheCount(Type handlerType)
    {
        var field = typeof(Mediator).GetField("_methodInfoCache", BindingFlags.NonPublic | BindingFlags.Static);
        var cache = (ConcurrentDictionary<Type, MethodInfo>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }

    private static int GetRequestHandlerDelegateCacheCount(Type handlerType)
    {
        var field = typeof(Mediator).GetField("_requestHandlerDelegates", BindingFlags.NonPublic | BindingFlags.Static);
        var cache = (ConcurrentDictionary<Type, Func<object, IRequest, CancellationToken, Task>>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }

    private static int GetRequestHandlerWithResponseDelegateCacheCount(Type handlerType)
    {
        var field = typeof(Mediator).GetField("_requestHandlerWithResponseDelegates", BindingFlags.NonPublic | BindingFlags.Static);
        var cache = (ConcurrentDictionary<Type, Delegate>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }

    private static int GetNotificationHandlerDelegateCacheCount(Type handlerType)
    {
        var field = typeof(Mediator).GetField("_notificationHandlerDelegates", BindingFlags.NonPublic | BindingFlags.Static);
        var cache = (ConcurrentDictionary<Type, Func<object, INotification, CancellationToken, Task>>)field!.GetValue(null)!;
        return cache.ContainsKey(handlerType) ? 1 : 0;
    }
}
