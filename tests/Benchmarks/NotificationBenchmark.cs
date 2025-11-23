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
using Microsoft.Extensions.Hosting;
using Wolverine;
using IMessageBus = Wolverine.IMessageBus;

namespace Coordix.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class NotificationBenchmark
{
    private Coordix.Interfaces.IMediator _coordixReflectionMediator = null!;
    private Coordix.Interfaces.IMediator _coordixCodeGenMediator = null!;
    private MediatRIMediator _mediatRMediator = null!;
    private IMessageBus _wolverineReflectionBus = null!;
    private IMessageBus _wolverineCodeGenBus = null!;
    private IHost _wolverineReflectionHost = null!;
    private IHost _wolverineCodeGenHost = null!;
    private TestNotification _coordixNotification = null!;
    private MediatRTestNotification _mediatRNotification = null!;
    private WolverineTestNotification _wolverineNotification = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Coordix Reflection-based mediator with 10 handlers
        var coordixReflectionServices = new ServiceCollection();
        coordixReflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        }, typeof(NotificationBenchmark).Assembly);
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler1>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler2>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler3>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler4>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler5>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler6>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler7>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler8>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler9>();
        coordixReflectionServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler10>();
        var coordixReflectionProvider = coordixReflectionServices.BuildServiceProvider();
        _coordixReflectionMediator = coordixReflectionProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        // Setup Coordix CodeGen-based mediator with 10 handlers
        var coordixCodeGenServices = new ServiceCollection();
        try
        {
            coordixCodeGenServices.AddCoordixWithCodeGen(null, typeof(NotificationBenchmark).Assembly);
        }
        catch (InvalidOperationException)
        {
            // CodeGen not available, fallback to Reflection
            coordixCodeGenServices.AddCoordix(options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
            }, typeof(NotificationBenchmark).Assembly);
        }
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler1>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler2>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler3>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler4>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler5>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler6>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler7>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler8>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler9>();
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.INotificationHandler<TestNotification>, TestNotificationHandler10>();
        var coordixCodeGenProvider = coordixCodeGenServices.BuildServiceProvider();
        _coordixCodeGenMediator = coordixCodeGenProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

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

        // Setup Wolverine Reflection mode with 10 handlers
        _wolverineReflectionHost = Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(NotificationBenchmark).Assembly);
                // Reflection mode: no code generation (default)
            })
            .Build();
        _wolverineReflectionHost.Start();
        _wolverineReflectionBus = _wolverineReflectionHost.Services.GetRequiredService<IMessageBus>();

        // Setup Wolverine CodeGen mode with 10 handlers
        // Note: Wolverine 5.x uses code generation by default when handlers are discovered
        _wolverineCodeGenHost = Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(NotificationBenchmark).Assembly);
                // CodeGen mode: Wolverine generates code automatically
            })
            .Build();
        _wolverineCodeGenHost.Start();
        _wolverineCodeGenBus = _wolverineCodeGenHost.Services.GetRequiredService<IMessageBus>();

        // Initialize test data (identical for all libraries)
        _coordixNotification = new TestNotification { Message = "Benchmark Notification" };
        _mediatRNotification = new MediatRTestNotification { Message = "Benchmark Notification" };
        _wolverineNotification = new WolverineTestNotification { Message = "Benchmark Notification" };
    }

    [Benchmark(Description = "Coordix - Reflection Mode (10 handlers)", Baseline = true)]
    public async Task PublishNotification_CoordixReflection()
    {
        await _coordixReflectionMediator.Publish(_coordixNotification);
    }

    [Benchmark(Description = "Coordix - CodeGen Mode (10 handlers, Source Generator)")]
    public async Task PublishNotification_CoordixCodeGen()
    {
        await _coordixCodeGenMediator.Publish(_coordixNotification);
    }

    [Benchmark(Description = "MediatR - Reflection Only (10 handlers)")]
    public async Task PublishNotification_MediatR()
    {
        await _mediatRMediator.Publish(_mediatRNotification);
    }

    [Benchmark(Description = "Wolverine - Reflection Mode (10 handlers)")]
    public async Task PublishNotification_WolverineReflection()
    {
        await _wolverineReflectionBus.PublishAsync(_wolverineNotification);
    }

    [Benchmark(Description = "Wolverine - CodeGen Mode (10 handlers)")]
    public async Task PublishNotification_WolverineCodeGen()
    {
        await _wolverineCodeGenBus.PublishAsync(_wolverineNotification);
    }
}

