using Coordix.Extensions;
using Coordix.Implementation;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Coordix.Tests.Samples;

namespace Coordix.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_RegistersIMediatorSingleton()
    {
        var services = new ServiceCollection();
        services.AddMediator();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }

    [Fact]
    public void AddCoordix_WithAssemblyArgument_RegistersRequestAndNotificationHandlers()
    {
        var services = new ServiceCollection();
        // pass in the test assembly explicitly
        services.AddCoordix(typeof(PingRequestHandler).Assembly);

        var provider = services.BuildServiceProvider();

        // IRequestHandler<PingRequest, PongResponse>
        var reqHandler = provider.GetService<IRequestHandler<PingRequest, PongResponse>>();
        Assert.NotNull(reqHandler);
        Assert.IsType<PingRequestHandler>(reqHandler);

        // INotificationHandler<TestNotificationServiceCollection>
        var notifHandler = provider.GetService<INotificationHandler<TestNotificationServiceCollection>>();
        Assert.NotNull(notifHandler);
        Assert.IsType<TestNotificationHandler>(notifHandler);
    }

    private static MethodInfo GetResolveAssembliesMethod()
        => typeof(ServiceCollectionExtensions).GetMethod("ResolveAssemblies", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void ResolveAssemblies_NoArgs_ReturnsAllNonDynamicAssemblies()
    {
        var m = GetResolveAssembliesMethod();
        var result = (Assembly[])m.Invoke(null, [Array.Empty<object>()])!;

        // Expect at least this test assembly
        Assert.Contains(typeof(ServiceCollectionExtensionsTests).Assembly, result);
        // None should be dynamic
        Assert.All(result, asm => Assert.False(asm.IsDynamic));
    }

    [Fact]
    public void ResolveAssemblies_WithAssemblyArgs_ReturnsExactlyThoseAssemblies()
    {
        var m = GetResolveAssembliesMethod();
        var assemblies = new object[] { typeof(PingRequestHandler).Assembly };
        var result = (Assembly[])m.Invoke(null, [assemblies])!;

        Assert.Single(result);
        Assert.Equal(typeof(PingRequestHandler).Assembly, result[0]);
    }

    [Fact]
    public void ResolveAssemblies_WithStringPrefix_FiltersByNamespace()
    {
        var m = GetResolveAssembliesMethod();
        // use the test namespace as prefix
        var prefix = typeof(ServiceCollectionExtensionsTests).Assembly.GetName().Name!;
        var result = (Assembly[])m.Invoke(null, [new object[] { prefix }])!;

        Assert.Contains(typeof(ServiceCollectionExtensionsTests).Assembly, result);
        // None outside that prefix
        Assert.All(result, asm => Assert.StartsWith(prefix, asm.FullName!, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveAssemblies_MixedArgTypes_ThrowsArgumentException()
    {
        var m = GetResolveAssembliesMethod();
        var badArgs = new object[] { typeof(PingRequestHandler).Assembly, "SomePrefix" };
        Assert.Throws<TargetInvocationException>(() => m.Invoke(null, [badArgs]));

        try
        {
            m.Invoke(null, [badArgs]);
        }
        catch (TargetInvocationException tie)
        {
            Assert.IsType<ArgumentException>(tie.InnerException);
        }
    }
}
