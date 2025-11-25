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
    private IMessageBus _wolverineBus = null!;
    private IHost _wolverineHost = null!;
    private TestRequest _coordixRequest = null!;
    private MediatRTestRequest _mediatRRequest = null!;
    private WolverineTestRequest _wolverineRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var coordixReflectionServices = new ServiceCollection();
        coordixReflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        }, typeof(RequestResponseBenchmark).Assembly);
        coordixReflectionServices.AddTransient<Coordix.Interfaces.IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var coordixReflectionProvider = coordixReflectionServices.BuildServiceProvider();
        _coordixReflectionMediator = coordixReflectionProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        var coordixCodeGenServices = new ServiceCollection();
        try
        {
            coordixCodeGenServices.AddCoordixWithCodeGen(typeof(RequestResponseBenchmark).Assembly);
        }
        catch (InvalidOperationException)
        {
            coordixCodeGenServices.AddCoordix(options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
            }, typeof(RequestResponseBenchmark).Assembly);
        }
        coordixCodeGenServices.AddTransient<Coordix.Interfaces.IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var coordixCodeGenProvider = coordixCodeGenServices.BuildServiceProvider();
        _coordixCodeGenMediator = coordixCodeGenProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RequestResponseBenchmark).Assembly));
        mediatRServices.AddTransient<MediatR.IRequestHandler<MediatRTestRequest, MediatRTestResponse>, MediatRTestRequestHandler>();
        var mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatRMediator = mediatRProvider.GetRequiredService<MediatRIMediator>();

        _wolverineHost = Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.Discovery.IncludeAssembly(typeof(RequestResponseBenchmark).Assembly);
            })
            .Build();
        _wolverineHost.Start();
        _wolverineBus = _wolverineHost.Services.GetRequiredService<IMessageBus>();

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

    [Benchmark(Description = "Wolverine")]
    public async Task<WolverineTestResponse> SendRequest_Wolverine()
    {
        return await _wolverineBus.InvokeAsync<WolverineTestResponse>(_wolverineRequest);
    }
}

