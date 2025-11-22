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
public class RequestResponseBenchmark
{
    private Coordix.Interfaces.IMediator _reflectionMediator = null!;
    private Coordix.Interfaces.IMediator _codeGenMediator = null!;
    private MediatRIMediator _mediatRMediator = null!;
    private TestRequest _request = null!;
    private MediatRTestRequest _mediatRRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Reflection-based mediator
        var reflectionServices = new ServiceCollection();
        reflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        }, typeof(RequestResponseBenchmark).Assembly);
        reflectionServices.AddTransient<Coordix.Interfaces.IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var reflectionProvider = reflectionServices.BuildServiceProvider();
        _reflectionMediator = reflectionProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        // Setup CodeGen-based mediator
        var codeGenServices = new ServiceCollection();
        bool codeGenAvailable = false;
        try
        {
            codeGenServices.AddCoordixWithCodeGen(null, typeof(RequestResponseBenchmark).Assembly);
            codeGenAvailable = true;
        }
        catch (InvalidOperationException)
        {
            // CodeGen not available, fallback to Reflection
            codeGenServices.AddCoordix(options =>
            {
                options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
            }, typeof(RequestResponseBenchmark).Assembly);
        }
        codeGenServices.AddTransient<Coordix.Interfaces.IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var codeGenProvider = codeGenServices.BuildServiceProvider();
        _codeGenMediator = codeGenProvider.GetRequiredService<Coordix.Interfaces.IMediator>();

        // Setup MediatR mediator
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RequestResponseBenchmark).Assembly));
        mediatRServices.AddTransient<MediatR.IRequestHandler<MediatRTestRequest, MediatRTestResponse>, MediatRTestRequestHandler>();
        var mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatRMediator = mediatRProvider.GetRequiredService<MediatRIMediator>();

        _request = new TestRequest { Data = "Benchmark Test Data" };
        _mediatRRequest = new MediatRTestRequest { Data = "Benchmark Test Data" };
    }

    [Benchmark(Description = "Coordix - Reflection Mode", Baseline = true)]
    public async Task<TestResponse> SendRequest_Reflection()
    {
        return await _reflectionMediator.Send(_request);
    }

    [Benchmark(Description = "Coordix - CodeGen Mode (Source Generator)")]
    public async Task<TestResponse> SendRequest_CodeGen()
    {
        return await _codeGenMediator.Send(_request);
    }

    [Benchmark(Description = "MediatR")]
    public async Task<MediatRTestResponse> SendRequest_MediatR()
    {
        return await _mediatRMediator.Send(_mediatRRequest);
    }
}

