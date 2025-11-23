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
public class RequestResponseBenchmark
{
    private Coordix.Interfaces.IMediator _coordixReflectionMediator = null!;
    private Coordix.Interfaces.IMediator _coordixCodeGenMediator = null!;
    private MediatRIMediator _mediatRMediator = null!;
    private IMessageBus _wolverineReflectionBus = null!;
    private IMessageBus _wolverineCodeGenBus = null!;
    private IHost _wolverineReflectionHost = null!;
    private IHost _wolverineCodeGenHost = null!;
    private TestRequest _coordixRequest = null!;
    private MediatRTestRequest _mediatRRequest = null!;
    private WolverineTestRequest _wolverineRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Coordix Reflection-based mediator
        var coordixReflectionServices = new ServiceCollection();
        coordixReflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        }, typeof(RequestResponseBenchmark).Assembly);
        coordixReflectionServices.AddTransient<Coordix.Interfaces.IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var coordixReflectionProvider = coordixReflectionServices.BuildServiceProvider();
        _coordixReflectionMediator = coordixReflectionProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        // Setup Coordix CodeGen-based mediator
        var coordixCodeGenServices = new ServiceCollection();
        try
        {
            coordixCodeGenServices.AddCoordixWithCodeGen(null, typeof(RequestResponseBenchmark).Assembly);
        }
        catch (InvalidOperationException)
        {
            // CodeGen not available, fallback to Reflection
            coordixCodeGenServices.AddCoordix(options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
            }, typeof(RequestResponseBenchmark).Assembly);
        }
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var coordixCodeGenProvider = coordixCodeGenServices.BuildServiceProvider();
        _coordixCodeGenMediator = coordixCodeGenProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        // Setup MediatR mediator
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RequestResponseBenchmark).Assembly));
        mediatRServices.AddTransient<MediatR.IRequestHandler<MediatRTestRequest, MediatRTestResponse>, MediatRTestRequestHandler>();
        var mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatRMediator = mediatRProvider.GetRequiredService<MediatRIMediator>();

        // Setup Wolverine Reflection mode (default, no code generation)
        _wolverineReflectionHost = Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(RequestResponseBenchmark).Assembly);
                // Reflection mode: no code generation (default)
            })
            .Build();
        _wolverineReflectionHost.Start();
        _wolverineReflectionBus = _wolverineReflectionHost.Services.GetRequiredService<IMessageBus>();

        // Setup Wolverine CodeGen mode (with code generation)
        // Note: Wolverine 5.x uses code generation by default when handlers are discovered
        _wolverineCodeGenHost = Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(RequestResponseBenchmark).Assembly);
                // CodeGen mode: Wolverine generates code automatically
            })
            .Build();
        _wolverineCodeGenHost.Start();
        _wolverineCodeGenBus = _wolverineCodeGenHost.Services.GetRequiredService<IMessageBus>();

        // Initialize test data (identical for all libraries)
        _coordixRequest = new TestRequest { Data = "Benchmark Test Data" };
        _mediatRRequest = new MediatRTestRequest { Data = "Benchmark Test Data" };
        _wolverineRequest = new WolverineTestRequest { Data = "Benchmark Test Data" };
    }

    [Benchmark(Description = "Coordix - Reflection Mode", Baseline = true)]
    public async Task<TestResponse> SendRequest_CoordixReflection()
    {
        return await _coordixReflectionMediator.Send(_coordixRequest);
    }

    [Benchmark(Description = "Coordix - CodeGen Mode (Source Generator)")]
    public async Task<TestResponse> SendRequest_CoordixCodeGen()
    {
        return await _coordixCodeGenMediator.Send(_coordixRequest);
    }

    [Benchmark(Description = "MediatR")]
    public async Task<MediatRTestResponse> SendRequest_MediatR()
    {
        return await _mediatRMediator.Send(_mediatRRequest);
    }

    [Benchmark(Description = "Wolverine - Reflection Mode")]
    public async Task<WolverineTestResponse> SendRequest_WolverineReflection()
    {
        return await _wolverineReflectionBus.InvokeAsync<WolverineTestResponse>(_wolverineRequest);
    }

    [Benchmark(Description = "Wolverine - CodeGen Mode")]
    public async Task<WolverineTestResponse> SendRequest_WolverineCodeGen()
    {
        return await _wolverineCodeGenBus.InvokeAsync<WolverineTestResponse>(_wolverineRequest);
    }
}

