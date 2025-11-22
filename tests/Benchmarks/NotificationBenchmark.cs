using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Coordix.Benchmarks.TestModels;
using Coordix;
using Coordix.Extensions;
using Coordix.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Coordix.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class NotificationBenchmark
{
    private IMediator _reflectionMediator = null!;
    private IMediator _codeGenMediator = null!;
    private TestNotification _notification = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Reflection-based mediator with 10 handlers
        var reflectionServices = new ServiceCollection();
        reflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        });
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler1>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler2>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler3>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler4>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler5>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler6>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler7>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler8>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler9>();
        reflectionServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler10>();
        var reflectionProvider = reflectionServices.BuildServiceProvider();
        _reflectionMediator = reflectionProvider.GetRequiredService<IMediator>();

        // Setup CodeGen-based mediator with 10 handlers
        var codeGenServices = new ServiceCollection();
        codeGenServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
        });
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler1>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler2>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler3>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler4>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler5>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler6>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler7>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler8>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler9>();
        codeGenServices.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler10>();
        var codeGenProvider = codeGenServices.BuildServiceProvider();
        _codeGenMediator = codeGenProvider.GetRequiredService<IMediator>();

        _notification = new TestNotification { Message = "Benchmark Notification" };
    }

    [Benchmark(Description = "Notification (10 handlers) - Reflection")]
    public async Task PublishNotification_Reflection()
    {
        await _reflectionMediator.Publish(_notification);
    }

    [Benchmark(Description = "Notification (10 handlers) - CodeGen", Baseline = true)]
    public async Task PublishNotification_CodeGen()
    {
        await _codeGenMediator.Publish(_notification);
    }
}

