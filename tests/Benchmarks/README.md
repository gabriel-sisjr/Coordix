# Coordix Benchmarks

This project contains performance benchmarks comparing different execution strategies in Coordix.

## Running Benchmarks

```bash
cd tests/Benchmarks
dotnet run -c Release
```

## Benchmark Scenarios

### 1. Request/Response Pattern

Measures latency for single request/response operations.

- **Reflection**: Uses runtime reflection to invoke handlers
- **CodeGen**: Uses compile-time generated code

### 2. Notification Pattern (10 handlers)

Measures throughput when publishing notifications to multiple handlers.

- Tests with 10 concurrent handlers
- Evaluates scaling characteristics

## Results

Results will be generated after running the benchmarks and added here.

### Expected Improvements

CodeGen is expected to provide:

- **Lower latency** for request/response (~30-50% faster)
- **Reduced allocations** (no reflection overhead)
- **Better scalability** with multiple handlers

## Interpreting Results

- **Mean**: Average execution time
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation
- **Allocated**: Memory allocated per operation
- **Rank**: Performance ranking (1 = fastest)

Lower values are better for all metrics.
