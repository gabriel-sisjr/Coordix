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
public class RequestResponseBenchmark
{
    private IMediator _reflectionMediator = null!;
    private IMediator _codeGenMediator = null!;
    private TestRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup Reflection-based mediator
        var reflectionServices = new ServiceCollection();
        reflectionServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.Reflection;
        });
        reflectionServices.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var reflectionProvider = reflectionServices.BuildServiceProvider();
        _reflectionMediator = reflectionProvider.GetRequiredService<IMediator>();

        // Setup CodeGen-based mediator
        var codeGenServices = new ServiceCollection();
        codeGenServices.AddCoordix(options =>
        {
            options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
        });
        codeGenServices.AddTransient<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        var codeGenProvider = codeGenServices.BuildServiceProvider();
        _codeGenMediator = codeGenProvider.GetRequiredService<IMediator>();

        _request = new TestRequest { Data = "Benchmark Test Data" };
    }

    [Benchmark(Description = "Request/Response - Reflection")]
    public async Task<TestResponse> SendRequest_Reflection()
    {
        return await _reflectionMediator.Send(_request);
    }

    [Benchmark(Description = "Request/Response - CodeGen", Baseline = true)]
    public async Task<TestResponse> SendRequest_CodeGen()
    {
        return await _codeGenMediator.Send(_request);
    }
}

