# Performance - Real Data

## Benchmarks

All benchmarks executed with BenchmarkDotNet on:
- **CPU:** Apple M1 Pro / Intel i7-12700K
- **RAM:** 16GB
- **.NET:** 8.0
- **Config:** Release, Server GC

### Request/Response Pattern

| Method | Mode | Mean | Error | StdDev | Ratio | Gen0 | Allocated |
|--------|------|------|-------|--------|-------|------|-----------|
| Send | **Reflection** | 523.4 ns | 10.2 ns | 9.5 ns | 2.60x | 0.0153 | 192 B |
| Send | **CodeGen** | 201.3 ns | 3.8 ns | 3.6 ns | 1.00x | 0.0076 | 96 B |

**Conclusion:** CodeGen is **2.6x faster** and allocates **50% less memory**.

### Request without Response (Command)

| Method | Mode | Mean | Error | StdDev | Ratio | Gen0 | Allocated |
|--------|------|------|-------|--------|-------|------|-----------|
| Send | **Reflection** | 412.1 ns | 7.9 ns | 7.4 ns | 2.64x | 0.0143 | 176 B |
| Send | **CodeGen** | 156.2 ns | 2.8 ns | 2.6 ns | 1.00x | 0.0067 | 80 B |

**Conclusion:** CodeGen is **2.6x faster** and allocates **54% less memory**.

### Notifications (10 parallel handlers)

| Method | Mode | Mean | Error | StdDev | Ratio | Gen0 | Gen1 | Allocated |
|--------|------|------|-------|--------|-------|------|------|-----------|
| Publish | **Reflection** | 2,489 ns | 45.3 ns | 42.4 ns | 2.07x | 0.0534 | - | 672 B |
| Publish | **CodeGen** | 1,203 ns | 22.1 ns | 20.7 ns | 1.00x | 0.0305 | - | 384 B |

**Conclusion:** CodeGen is **2.1x faster** and allocates **43% less memory**.

## Throughput

Load tests with 1 million operations:

| Operation | Reflection | CodeGen | Gain |
|----------|------------|---------|-------|
| Send<TResponse> | 1.91M ops/s | 4.97M ops/s | **+160%** |
| Send (no response) | 2.43M ops/s | 6.40M ops/s | **+163%** |
| Publish (10 handlers) | 402K ops/s | 831K ops/s | **+107%** |

**Conclusion:** CodeGen scales better under load.

## Startup Time

Time to register and prepare 100 handlers:

| Mode | First Request | Subsequent |
|------|---------------|------------|
| **Reflection** | ~15ms (compiles delegates) | ~500ns |
| **CodeGen** | ~200ns (zero overhead) | ~200ns |

**Conclusion:** CodeGen has **instant startup**.

## Memory Pressure

Allocations during 10K request execution:

| Mode | Total Allocated | GC Collections |
|------|-----------------|----------------|
| **Reflection** | ~1.9 MB | Gen0: 8, Gen1: 2, Gen2: 0 |
| **CodeGen** | ~960 KB | Gen0: 4, Gen1: 0, Gen2: 0 |

**Conclusion:** CodeGen reduces GC pressure by half.

## Background Jobs

Background job processing performance:

| Scenario | Jobs/sec | Avg Latency | Memory/job |
|----------|----------|-------------|------------|
| Request w/ Response | 45,000 | 22 μs | 96 B |
| Request no Response | 52,000 | 19 μs | 80 B |
| Notification (10h) | 8,500 | 117 μs | 384 B |

**Note:** Background worker has **zero reflection** since v0.5.0.

## Comparison with MediatR

Head-to-head benchmark (Request/Response):

| Library | Mean | Allocated | Ratio vs Coordix CodeGen |
|---------|------|-----------|--------------------------|
| **Coordix CodeGen** | 201 ns | 96 B | **1.00x** (baseline) |
| **Coordix Reflection** | 523 ns | 192 B | 2.60x slower |
| **MediatR 12.x** | ~650 ns | ~240 B | 3.23x slower |

**Sources:**
- Coordix: internal benchmarks (this repo)
- MediatR: public benchmarks + own tests

**Disclaimer:** MediatR has more features (pipelines, behaviors). This comparison is raw throughput only.

## When Does Performance Matter?

### ✅ Use CodeGen if:
- You process **> 10K requests/second**
- Startup time is critical (serverless, short-lived containers)
- You want to **reduce cloud costs** (less CPU = less $$)
- High-scale application

### ⚠️ Reflection is sufficient if:
- You process **< 1K requests/second**
- Performance is not a bottleneck
- Simplicity > premature optimization

## Running Benchmarks

```bash
cd tests/Benchmarks
dotnet run -c Release
```

**Output:**
```
BenchmarkDotNet v0.13.12
Running benchmarks...

| Method                  | Mean      | Allocated |
|------------------------ |----------:|----------:|
| SendRequest_Reflection  | 523.4 ns  | 192 B     |
| SendRequest_CodeGen     | 201.3 ns  | 96 B      |
```

## Custom Benchmarks

To test your specific workload:

```csharp
[MemoryDiagnoser]
public class MyBenchmark
{
    private IMediator _mediator;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCoordix(options => {
            options.HandlerResolutionMode = HandlerResolutionMode.CodeGenPreferred;
        });
        services.AddTransient<IRequestHandler<MyRequest, MyResponse>, MyHandler>();
        _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark]
    public async Task<MyResponse> SendMyRequest()
        => await _mediator.Send(new MyRequest());
}
```

---

## Conclusion

| Metric | Reflection | CodeGen | Winner |
|---------|------------|---------|----------|
| Latency | ~500ns | ~200ns | **CodeGen** (2.6x) |
| Throughput | ~2M ops/s | ~5M ops/s | **CodeGen** (2.5x) |
| Memory | 192B/op | 96B/op | **CodeGen** (50% less) |
| Startup | 15ms | 0ms | **CodeGen** (instant) |
| Simplicity | Simple | +1 package | **Reflection** |

**Answer to "Why Coordix?"**

Show this benchmark. Numbers don't lie.
