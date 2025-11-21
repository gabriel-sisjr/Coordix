using System;
using System.Linq;
using System.Reflection;
using Coordix;
using Coordix.Extensions;
using Coordix.Implementation;
using Coordix.Interfaces;
using Coordix.Tests.Samples;
using Microsoft.Extensions.DependencyInjection;

namespace Coordix.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_RegistersIMediatorSingleton()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddMediator();

        ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetService<IMediator>();

        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }

    [Fact]
    public void AddCoordix_WithAssemblyArgument_RegistersRequestAndNotificationHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        // pass in the test assembly explicitly
        services.AddCoordix(typeof(PingRequestHandler).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();

        // IRequestHandler<PingRequest, PongResponse>
        IRequestHandler<PingRequest, PongResponse> reqHandler = provider.GetService<IRequestHandler<PingRequest, PongResponse>>();
        Assert.NotNull(reqHandler);
        Assert.IsType<PingRequestHandler>(reqHandler);

        // INotificationHandler<TestNotificationServiceCollection>
        INotificationHandler<TestNotificationServiceCollection> notifHandler = provider.GetService<INotificationHandler<TestNotificationServiceCollection>>();
        Assert.NotNull(notifHandler);
        Assert.IsType<TestNotificationHandler>(notifHandler);
    }

    private static MethodInfo GetResolveAssembliesMethod()
            => typeof(ServiceCollectionExtensions).GetMethod("ResolveAssemblies", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void ResolveAssemblies_NoArgs_ReturnsAllNonDynamicAssemblies()
    {
        MethodInfo m = GetResolveAssembliesMethod();
        Assembly[] result = (Assembly[])m.Invoke(null, [Array.Empty<object>()])!;

        // Expect at least this test assembly
        Assert.Contains(typeof(ServiceCollectionExtensionsTests).Assembly, result);
        // None should be dynamic
        Assert.All(result, asm => Assert.False(asm.IsDynamic));
    }

    [Fact]
    public void ResolveAssemblies_WithAssemblyArgs_ReturnsExactlyThoseAssemblies()
    {
        MethodInfo m = GetResolveAssembliesMethod();
        object[] assemblies = new object[] { typeof(PingRequestHandler).Assembly };
        Assembly[] result = (Assembly[])m.Invoke(null, [assemblies])!;

        Assert.Single(result);
        Assert.Equal(typeof(PingRequestHandler).Assembly, result[0]);
    }

    [Fact]
    public void ResolveAssemblies_WithStringPrefix_FiltersByNamespace()
    {
        MethodInfo m = GetResolveAssembliesMethod();
        // use the test namespace as prefix
        string prefix = typeof(ServiceCollectionExtensionsTests).Assembly.GetName().Name!;
        Assembly[] result = (Assembly[])m.Invoke(null, [new object[] { prefix }])!;

        Assert.Contains(typeof(ServiceCollectionExtensionsTests).Assembly, result);
        // None outside that prefix
        Assert.All(result, asm => Assert.StartsWith(prefix, asm.FullName!, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveAssemblies_MixedArgTypes_ThrowsArgumentException()
    {
        MethodInfo m = GetResolveAssembliesMethod();
        object[] badArgs = new object[] { typeof(PingRequestHandler).Assembly, "SomePrefix" };
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

    [Fact]
    public void AddCoordix_WithConfiguration_AppliesOptions()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        CoordixOptions options = provider.GetService<CoordixOptions>();
        IMediator mediator = provider.GetService<IMediator>();

        Assert.NotNull(options);
        Assert.Equal(HandlerResolutionMode.Reflection, options.HandlerResolutionMode);
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddCoordix_WithConfigurationAndAssembly_AppliesOptionsAndScansAssembly()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddCoordix(
            options => options.HandlerResolutionMode = HandlerResolutionMode.Reflection,
            typeof(PingRequestHandler).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        CoordixOptions options = provider.GetService<CoordixOptions>();
        IRequestHandler<PingRequest, PongResponse> handler = provider.GetService<IRequestHandler<PingRequest, PongResponse>>();

        Assert.NotNull(options);
        Assert.Equal(HandlerResolutionMode.Reflection, options.HandlerResolutionMode);
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddCoordix_WithCodeGenPreferredMode_ThrowsInvalidOperationException()
    {
        ServiceCollection services = new ServiceCollection();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddCoordix(options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
            });
        });

        Assert.Contains("Coordix.CodeGen", exception.Message);
        Assert.Contains("CodeGenPreferred", exception.Message);
    }

    [Fact]
    public void AddCoordix_WithNullOptions_ThrowsArgumentNullException()
    {
        ServiceCollection services = new ServiceCollection();
        MethodInfo? method = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "AddCoordix" &&
                                                     m.GetParameters().Length == 3 &&
                                                     m.GetParameters()[1].ParameterType == typeof(CoordixOptions));

        Assert.NotNull(method);
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
        {
            method.Invoke(null, new object[] { services, null!, Array.Empty<object>() });
        });

        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }


    [Fact]
    public void AddMediator_WithArgs_RegistersHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddMediator(typeof(PingRequestHandler).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        IRequestHandler<PingRequest, PongResponse> handler = provider.GetService<IRequestHandler<PingRequest, PongResponse>>();

        Assert.NotNull(handler);
    }
}
