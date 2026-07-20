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
2. Reuses it for every preparation, pilot, warmup, and measured invocation.
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

The runner first executes one untimed preparation operation so cold JIT compilation
cannot make the first pilot appear long enough and incorrectly select one operation
per sample. The pilot stage then starts with one operation per iteration. Very short
pilots increase geometrically; once the clock has a usable signal, the runner
projects the operation count needed for an iteration of at least 20 milliseconds.
The count is bounded by an internal safety limit. This reaches a useful signal with
fewer state-mutating pilot invocations than simple doubling. If the limit is reached
first, the result reports a calibration warning.

After calibration, the runner warms up with an *adaptive* stability condition rather
than a fixed duration: it runs batches until the most recent window of per-operation
timings varies by no more than a small relative threshold, which catches both level
shifts and monotonic drift as a benchmark climbs the JIT's compilation tiers. Warmup
runs for at least a few iterations and is bounded by an iteration and time cap.

Because the pilot calibration measured cold, un-tiered code, the runner then
**recalibrates** the batch size against the warmed per-operation timing, targeting
roughly 50 milliseconds per sample. This keeps each measured sample large enough for
good clock resolution once the method has reached steady state, instead of using a
batch size computed from slower startup code.

Before measurement the runner performs a single full garbage collection and settles
pending finalizers, then records the heap state. It does **not** collect between
samples, so realistic allocation costs remain visible. It records between ten and
fifty samples; sampling stops once the measurement is precise enough, or at the
sample limit. A result that reaches the limit first reports a precision warning.

Convergence uses a **robust** precision criterion. Rather than testing the mean's
confidence interval — whose margin a few retained outliers can inflate indefinitely
— the runner estimates dispersion from the median absolute deviation (scaled to a
standard-deviation equivalent) and stops once that estimate's margin of error is at
most 2% of the median. This lets a benchmark with a stable median and MAD converge
even when an occasional slow sample would otherwise prevent it.

Each sample times one calibrated batch and is divided by its operation count. The
generated dispatch places the repetition loop around a direct method call, so clock
and delegate dispatch overhead occur once per sample rather than once per operation;
benchmark dispatch performs no string lookup. Cancellation checks and statistical
analysis occur outside timing. This policy is internal and intentionally not
configurable. Benchmarks run sequentially to reduce interference.

### Stability and robust statistics

In-process timing is prone to *non-stationary execution* — the timing can shift
partway through a run due to late tiering, cache effects, or scheduling. The runner
guards against reporting a deceptively precise mean over such a run by comparing the
median of the first half of the samples with the median of the second half. When
those regimes differ materially, the result is flagged as **unstable**. Comparing
medians of sample groups also blunts the effect of autocorrelation between adjacent
samples.

Summary statistics lead with the **median** and **median absolute deviation (MAD)**,
which are robust to the occasional slow sample; the mean and its confidence interval
are retained as secondary measures.

A completed benchmark produces `BenchmarkResult.Completed` with:

- Total measured time
- Raw per-operation samples
- Median and median absolute deviation (primary)
- Mean, sample standard deviation, and standard error (secondary)
- Minimum and maximum
- 95% confidence interval for the mean
- Tukey outlier count
- Stability flag (whether distinct timing regimes were detected)
- Gen0/Gen1/Gen2 collection counts and bytes allocated during measurement

The `BenchmarkStatistics` record stores GC collection counts and allocated bytes as
raw totals over the whole measurement window. The console formatter presents them
**normalized per operation** — bytes allocated per operation and collections per
1,000 operations — matching the convention used by BenchmarkDotNet.
- Measured iteration count
- Operations per iteration (after post-warmup recalibration)
- Warmup iteration count and calibration/convergence status

Outliers remain in all calculations. They are classified and reported rather than
silently discarded.

Failures and skipped benchmarks produce `BenchmarkResult.Failed` and
`BenchmarkResult.Skipped`, respectively. At the Microsoft Testing Platform boundary,
a completed benchmark is represented as a passed node because the platform requires
test-oriented states; benchmark code does not model it as a passed test.

## Current Scope

NXTest intentionally performs best-effort measurement in the test process. Benchmark
mode excludes regular tests, and the pilot, adaptive warmup, recalibration, and
measurement stages reduce several important sources of noise without requiring
process isolation.

- Timing still includes loop and method-call overhead, although pilot batching
  amortizes clock and generated-dispatch overhead.
- There is no overhead subtraction, baseline, or regression comparison. A paired,
  interleaved comparison mode (for A/B ratios with paired confidence intervals) would
  be a natural future addition but is out of scope today.
- Instability is detected and reported, but the runner does not model change points
  or autocorrelation explicitly, nor does it bootstrap confidence intervals.
- Parameterless benchmark signatures are documented but not yet diagnosed by the
  source generator.

Use these results for coarse comparisons and tracking relatively substantial
operations. Very small operations can be dominated by framework and clock overhead.
