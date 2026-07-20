# Benchmarking

NXTest provides basic in-process benchmarking through the `[Bench]` attribute.
Benchmarks are discovered by the source generator but run separately from tests.

## Defining a Benchmark

Mark a method with `[Bench]`:

```csharp
public class ParserBenchmarks
{
    private readonly byte[] _payload = LoadPayload();

    [Bench]
    public void ParsePayload()
    {
        Benchmark.Consume(Parser.Parse(_payload));
    }
}
```

Benchmark methods must return `void` or non-generic `Task`. `ValueTask`, `Task<T>`,
and other return types produce a compile-time diagnostic. An exception fails the
benchmark.

Parameterized benchmarks reuse theory-style `[InlineData]`:

```csharp
[Bench]
[InlineData(16)]
[InlineData(256)]
public void ParsePayload(int payloadSize)
{
    Benchmark.Consume(Parser.Parse(_payload.AsSpan(0, payloadSize)));
}
```

Each data row is a separate benchmark result, such as
`ParsePayload(payloadSize: 16)`. It receives its own calibration, warmup,
measurements, and class instance. A benchmark with parameters must provide at least
one `[InlineData]` row, and every row must match the method's parameter count;
violations produce compile-time diagnostics.

## Running Benchmarks

Normal test runs exclude benchmarks:

```bash
dotnet test
```

Run the benchmark project directly in Release mode to see timing details for
successful benchmarks:

```bash
dotnet run --project perf/bench/bench.csproj -c Release -- \
  --bench
```

Replace `perf/bench/bench.csproj` with the path to your benchmark project.
`--bench` runs benchmarks exclusively; facts and theories are not run. NXTest
defaults the native Microsoft Testing Platform runner to `Detailed` output in
benchmark mode because its normal output hides successful benchmark timings. Pass
an explicit `--output` value to override this default.

Programmatic callers can select the same mode with
`TestExecutionOptions.RunBenchmarks = true`.

## Instance Lifecycle

Each instance benchmark case gets its own class instance. NXTest:

1. Constructs the instance once.
2. Reuses it for every pilot, warmup, and measured invocation.
3. Disposes it once after the benchmark completes when it implements `IDisposable`.

Construction and disposal are outside the measurement window. Constructors can
therefore perform per-benchmark setup without adding allocation time to every
sample.

State mutations persist between invocations. Benchmarks that require fresh state
must reset that state themselves; per-iteration setup is not currently supported.
Static benchmark methods do not create a class instance.

## Optimization Resistance

The JIT can remove pure computations when their results are unused. Pass benchmark
outputs to `Benchmark.Consume` to make them observable without boxing:

```csharp
[Bench]
public void ParsePayload()
{
    Benchmark.Consume(Parser.Parse(_payload));
}
```

`Benchmark.Consume` is a non-inlined generic call, following the same strategy as
BenchmarkDotNet's dead-code-elimination helper. The call is part of the measured
operation. Methods that already produce observable side effects do not need it.

## Measurement and Results

The pilot stage starts with one operation per iteration. Very short pilots increase
geometrically; once the clock has a usable signal, the runner projects the operation
count needed for an iteration of at least 20 milliseconds. The count is bounded by
an internal safety limit. This reaches a useful signal with fewer state-mutating
pilot invocations than simple doubling. If the limit is reached first, the result
reports a calibration warning.

After calibration, the runner performs between three and ten unmeasured warmup
iterations, continuing for at least 100 milliseconds when possible. It then records
between ten and fifty samples. Sampling stops when the 95% confidence interval's
margin of error is at most 2% of the mean, or at the sample limit. A result that
reaches the limit first reports a precision warning.

Each sample times one calibrated batch and is divided by its operation count. The
generated dispatch places the repetition loop around a direct method call, so clock,
and delegate dispatch overhead occur once per sample rather than once per operation;
benchmark dispatch performs no string lookup. Cancellation checks and statistical
analysis occur outside timing.
This policy is internal and intentionally not configurable. Benchmarks run
sequentially to reduce interference.

A completed benchmark produces `BenchmarkResult.Completed` with:

- Total measured time
- Raw per-operation samples
- Mean and median
- Minimum
- Maximum
- Sample standard deviation and standard error
- 95% confidence interval for the mean
- Tukey outlier count
- Measured iteration count
- Operations per iteration
- Warmup iteration count and calibration/convergence status

Outliers remain in all calculations. They are classified and reported rather than
silently discarded.

Failures and skipped benchmarks produce `BenchmarkResult.Failed` and
`BenchmarkResult.Skipped`, respectively. At the Microsoft Testing Platform boundary,
a completed benchmark is represented as a passed node because the platform requires
test-oriented states; benchmark code does not model it as a passed test.

## Current Scope

NXTest intentionally performs best-effort measurement in the test process. Benchmark
mode excludes regular tests, and the pilot, warmup, and measurement stages reduce
several important sources of noise without requiring process isolation.

- Timing still includes loop and method-call overhead, although pilot batching
  amortizes clock and generated-dispatch overhead.
- There is no overhead subtraction, baseline, or regression comparison.
- Parameterless benchmark signatures are documented but not yet diagnosed by the
  source generator.

Use these results for coarse comparisons and tracking relatively substantial
operations. Very small operations can be dominated by framework and clock overhead.
