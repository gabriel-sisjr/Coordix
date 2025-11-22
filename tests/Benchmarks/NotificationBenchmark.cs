using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Coordix.Benchmarks.TestModels;
using Coordix;
using Coordix.Extensions;
using Coordix.CodeGen.Extensions;
using Coordix.Interfaces;
using MediatR;
using MediatRIMediator = MediatR.IMediator;
using Microsoft.Extensions.DependencyInjection;

namespace Coordix.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class NotificationBenchmark
{
    private Coordix.Interfaces.IMediator _reflectionMediator = null!;
    private Coordix.Interfaces.IMediator _codeGenMediator = null!;
    private MediatRIMediator _mediatRMediator = null!;
    private TestNotification _notification = null!;
    private MediatRTestNotification _mediatRNotification = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Reflection-based mediator with 10 handlers
        var reflectionServices = new ServiceCollection();
        reflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        }, typeof(NotificationBenchmark).Assembly);
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler1>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler2>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler3>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler4>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler5>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler6>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler7>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler8>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler9>();
        reflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler10>();
        var reflectionProvider = reflectionServices.BuildServiceProvider();
        _reflectionMediator = reflectionProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        // Setup CodeGen-based mediator with 10 handlers
        var codeGenServices = new ServiceCollection();
        bool codeGenAvailable = false;
        try
        {
            codeGenServices.AddCoordixWithCodeGen(null, typeof(NotificationBenchmark).Assembly);
            codeGenAvailable = true;
        }
        catch (InvalidOperationException)
        {
            // CodeGen not available, fallback to Reflection
            codeGenServices.AddCoordix(options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
            }, typeof(NotificationBenchmark).Assembly);
        }
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler1>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler2>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler3>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler4>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler5>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler6>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler7>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler8>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler9>();
        codeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler10>();
        var codeGenProvider = codeGenServices.BuildServiceProvider();
        _codeGenMediator = codeGenProvider.GetRequiredService<Coordix.Interfaces.IMediator>();
        
        // Verify which executor is actually being used
        var executor = codeGenProvider.GetRequiredService<Coordix.Interfaces.IHandlerExecutor>();
        var executorType = executor.GetType().Name;
        var executorFullName = executor.GetType().FullName;
        
        // Log to console (will appear in benchmark output)
        Console.WriteLine($"[CodeGen Setup] Available: {codeGenAvailable}, Executor Type: {executorType}, Full Name: {executorFullName}");
        
        if (!codeGenAvailable || executorType != "GeneratedHandlerExecutor")
        {
            Console.WriteLine($"⚠️  WARNING: CodeGen benchmark is using {executorType} instead of GeneratedHandlerExecutor!");
            Console.WriteLine($"   This means CodeGen is falling back to Reflection mode.");
        }
        else
        {
            Console.WriteLine($"✅ CodeGen is using GeneratedHandlerExecutor correctly.");
        }

        // Setup MediatR mediator with 10 handlers
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NotificationBenchmark).Assembly));
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler1>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler2>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler3>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler4>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler5>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler6>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler7>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler8>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler9>();
        mediatRServices.AddTransient<MediatR.INotificationHandler<MediatRTestNotification>, MediatRTestNotificationHandler10>();
        var mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatRMediator = mediatRProvider.GetRequiredService<MediatRIMediator>();

        _notification = new TestNotification { Message = "Benchmark Notification" };
        _mediatRNotification = new MediatRTestNotification { Message = "Benchmark Notification" };
    }

    [Benchmark(Description = "Coordix - Reflection Mode (10 handlers)", Baseline = true)]
    public async Task PublishNotification_Reflection()
    {
        await _reflectionMediator.Publish(_notification);
    }

    [Benchmark(Description = "Coordix - CodeGen Mode (10 handlers, Source Generator)")]
    public async Task PublishNotification_CodeGen()
    {
        await _codeGenMediator.Publish(_notification);
    }

    [Benchmark(Description = "MediatR - Reflection Only (10 handlers)")]
    public async Task PublishNotification_MediatR()
    {
        await _mediatRMediator.Publish(_mediatRNotification);
    }
}

