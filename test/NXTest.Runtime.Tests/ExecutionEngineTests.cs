using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XunitAssert = Xunit.Assert;
using XunitFactAttribute = Xunit.FactAttribute;
using static NXTest.RunResult;

namespace NXTest.Runtime.Tests;

/// <summary>
/// Tests that validate the TestExecutionEngine.
/// These are XUnit tests that test our NXTest runtime by manually synthesizing
/// test metadata and dispatch delegates.
/// </summary>
public class ExecutionEngineTests
{
    [XunitFact]
    public async Task ExecuteTestsAsync_WithSimplePassingTest_ReturnsPassedResult()
    {
        bool executed = false;
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SimpleTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "SimplePassingTest", Skip = false },
            ],
            CreateInstance = () => null,
            TestDispatch = (testClass, methodName, _) =>
            {
                switch (methodName)
                {
                    case "SimplePassingTest":
                        executed = true;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown method: {methodName}");
                }
                return Task.CompletedTask;
            },
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.True(executed, "Test delegate should have been executed");
        var result = XunitAssert.IsType<TestResult.Passed>(XunitAssert.Single(results));
        XunitAssert.Equal("SimplePassingTest", result.Name);
        XunitAssert.Equal("SimpleTestClass", result.ClassName);
        XunitAssert.True(result.Duration >= TimeSpan.Zero);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithSkippedTest_ReturnsSkippedResult()
    {
        // Arrange: Create a test marked as skipped
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SkippedTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact
                {
                    MethodName = "SkippedTest",
                    Skip = true,
                    SkipReason = "Test intentionally skipped for testing",
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        var result = XunitAssert.IsType<TestResult.Skipped>(XunitAssert.Single(results));
        XunitAssert.Equal("SkippedTest", result.Name);
        XunitAssert.Equal("Test intentionally skipped for testing", result.Reason);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithMultipleTests_ReturnsAllResults()
    {
        // Arrange: Create multiple tests
        var testMetadata = new TestClassMetadata
        {
            ClassName = "MultiTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "Test1", Skip = false },
                new TestMethodMetadata.Fact { MethodName = "Test2", Skip = false },
                new TestMethodMetadata.Fact
                {
                    MethodName = "Test3",
                    Skip = true,
                    SkipReason = "Skip this one",
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.Equal(2, results.Count(r => r is TestResult.Passed));
        XunitAssert.Single(results, r => r is TestResult.Skipped);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithSequentialExecution_ExecutesInOrder()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SequentialTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "Test1" },
                new TestMethodMetadata.Fact { MethodName = "Test2" },
                new TestMethodMetadata.Fact { MethodName = "Test3" },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = new TestExecutionOptions { Mode = ParallelMode.None };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        AssertAllTestsPassed(results);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithParallelExecution_ExecutesAllTests()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "ParallelTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "ParallelTest1" },
                new TestMethodMetadata.Fact { MethodName = "ParallelTest2" },
                new TestMethodMetadata.Fact { MethodName = "ParallelTest3" },
                new TestMethodMetadata.Fact { MethodName = "ParallelTest4" },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = new TestExecutionOptions
        {
            Mode = ParallelMode.Tests,
            MaxDegreeOfParallelism = 2,
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(4, results.Length);
        AssertAllTestsPassed(results);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithStopOnFirstFailure_StopsAfterFirstFailure()
    {
        // Arrange: First test will be skipped (not a failure), second will pass
        var testMetadata = new TestClassMetadata
        {
            ClassName = "StopOnFailureTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "Test1" },
                new TestMethodMetadata.Fact { MethodName = "Test2" },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = new TestExecutionOptions
        {
            Mode = ParallelMode.None,
            StopOnFirstFailure = true,
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert: All should execute since none fail (placeholder implementation)
        XunitAssert.Equal(2, results.Length);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithMultipleTestClasses_ExecutesAllClasses()
    {
        // Arrange: Multiple test classes
        var testClasses = new[]
        {
            new TestClassMetadata
            {
                ClassName = "TestClass1",
                TestMethods =
                [
                    new TestMethodMetadata.Fact { MethodName = "Test1A" },
                    new TestMethodMetadata.Fact { MethodName = "Test1B" },
                ],
                CreateInstance = () => null,
                TestDispatch = (_, _, _) => Task.CompletedTask,
            },
            new TestClassMetadata
            {
                ClassName = "TestClass2",
                TestMethods =
                [
                    new TestMethodMetadata.Fact { MethodName = "Test2A" },
                    new TestMethodMetadata.Fact { MethodName = "Test2B" },
                ],
                CreateInstance = () => null,
                TestDispatch = (_, _, _) => Task.CompletedTask,
            },
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(testClasses, options);

        // Assert
        XunitAssert.Equal(4, results.Length);
        XunitAssert.Equal(2, results.Count(r => r.ClassName == "TestClass1"));
        XunitAssert.Equal(2, results.Count(r => r.ClassName == "TestClass2"));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_MeasuresDuration()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "TimingTestClass",
            TestMethods = [new TestMethodMetadata.Fact { MethodName = "TimedTest" }],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        var result = XunitAssert.IsType<TestResult.Passed>(XunitAssert.Single(results));
        XunitAssert.True(result.Duration >= TimeSpan.Zero);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithFailingTest_ReturnsFailedResult()
    {
        // Arrange: Create a test that throws an exception
        var testMetadata = new TestClassMetadata
        {
            ClassName = "FailingTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "FailingTest", Skip = false },
            ],
            CreateInstance = () => null,
            TestDispatch = async (_, methodName, _) =>
            {
                await Task.CompletedTask;
                if (methodName == "FailingTest")
                    throw new InvalidOperationException("Test intentionally failed");
            },
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        var result = XunitAssert.IsType<TestResult.Failed>(XunitAssert.Single(results));
        XunitAssert.Equal("FailingTest", result.Name);
        XunitAssert.Contains("Test intentionally failed", result.ErrorMessage);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithAsyncTest_ExecutesCorrectly()
    {
        // Arrange: Create an async test
        var asyncExecuted = false;
        var testMetadata = new TestClassMetadata
        {
            ClassName = "AsyncTestClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "AsyncTest", IsAsync = true },
            ],
            CreateInstance = () => null,
            TestDispatch = async (_, methodName, _) =>
            {
                if (methodName == "AsyncTest")
                {
                    await Task.Delay(50); // Simulate async work
                    asyncExecuted = true;
                }
            },
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.True(asyncExecuted, "Async test should have executed");
        var result = XunitAssert.IsType<TestResult.Passed>(XunitAssert.Single(results));
        XunitAssert.True(result.Duration >= TimeSpan.FromMilliseconds(40)); // Account for timing variance
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_ClassesMode_RunsTestsWithinAClassSequentially()
    {
        // Arrange: tests in the same class share a dispatch that flags any concurrent overlap
        var running = 0;
        var overlapDetected = false;

        var testMetadata = new TestClassMetadata
        {
            ClassName = "SequentialWithinClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "T1" },
                new TestMethodMetadata.Fact { MethodName = "T2" },
                new TestMethodMetadata.Fact { MethodName = "T3" },
            ],
            CreateInstance = () => null,
            TestDispatch = async (_, _, _) =>
            {
                if (Interlocked.Increment(ref running) > 1)
                    overlapDetected = true;
                await Task.Delay(30);
                Interlocked.Decrement(ref running);
            },
        };

        var options = new TestExecutionOptions
        {
            Mode = ParallelMode.Classes,
            MaxDegreeOfParallelism = 4,
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.False(
            overlapDetected,
            "Tests within a single class must not run concurrently in Classes mode"
        );
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_ClassesMode_RunsDifferentClassesInParallel()
    {
        // Arrange: each class's test rendezvouses with the other; this only
        // completes if both classes are executing concurrently.
        var classAStarted = new TaskCompletionSource();
        var classBStarted = new TaskCompletionSource();

        var classA = new TestClassMetadata
        {
            ClassName = "ClassA",
            TestMethods = [new TestMethodMetadata.Fact { MethodName = "A1" }],
            CreateInstance = () => null,
            TestDispatch = async (_, _, _) =>
            {
                classAStarted.SetResult();
                await classBStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            },
        };

        var classB = new TestClassMetadata
        {
            ClassName = "ClassB",
            TestMethods = [new TestMethodMetadata.Fact { MethodName = "B1" }],
            CreateInstance = () => null,
            TestDispatch = async (_, _, _) =>
            {
                classBStarted.SetResult();
                await classAStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            },
        };

        var options = new TestExecutionOptions
        {
            Mode = ParallelMode.Classes,
            MaxDegreeOfParallelism = 2,
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([classA, classB], options);

        // Assert: both pass, proving they ran concurrently (sequential execution would time out)
        XunitAssert.Equal(2, results.Length);
        AssertAllTestsPassed(results);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_RandomlyPermutesTestsAcrossRuns()
    {
        async Task<string> RunAndCaptureOrder()
        {
            var order = new System.Collections.Generic.List<string>();
            var gate = new object();

            var metadata = new TestClassMetadata
            {
                ClassName = "PermuteClass",
                TestMethods = Enumerable
                    .Range(0, 25)
                    .Select(i => (TestMethodMetadata)new TestMethodMetadata.Fact { MethodName = $"T{i:D2}" })
                    .ToArray(),
                CreateInstance = () => null,
                TestDispatch = (_, methodName, _) =>
                {
                    lock (gate)
                    {
                        order.Add(methodName);
                    }
                    return Task.CompletedTask;
                },
            };

            // Sequential mode so execution order reflects the (shuffled) collection order.
            var options = new TestExecutionOptions { Mode = ParallelMode.None };
            await TestExecutionEngine.ExecuteTestsAsync([metadata], options);

            return string.Join(",", order);
        }

        var run1 = await RunAndCaptureOrder();
        var run2 = await RunAndCaptureOrder();

        // With 25 tests, two independent shuffles producing the same order is astronomically unlikely.
        XunitAssert.NotEqual(run1, run2);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithBenchmark_ReturnsTimingStatistics()
    {
        long invocationCount = 0;
        var metadata = new TestClassMetadata
        {
            ClassName = "BenchmarkClass",
            TestMethods =
            [
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "MeasureWork",
                    BenchmarkDispatch = async (_, _, operations) =>
                    {
                        invocationCount += operations;
                        await Task.Delay(Math.Min(operations, 25));
                    },
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var results = await TestExecutionEngine.ExecuteTestsAsync(
            [metadata],
            new TestExecutionOptions
            {
                Mode = ParallelMode.None,
                RunBenchmarks = true,
            }
        );

        var result = XunitAssert.IsType<BenchmarkResult.Completed>(
            XunitAssert.Single(results)
        );
        XunitAssert.True(invocationCount > 13);
        var benchmark = XunitAssert.IsType<BenchmarkStatistics>(result.Statistics);
        XunitAssert.InRange(benchmark.Iterations, 10, 50);
        XunitAssert.True(benchmark.OperationsPerIteration > 1);
        XunitAssert.True(benchmark.CalibrationTargetReached);
        XunitAssert.InRange(benchmark.WarmupIterations, 4, 20);
        XunitAssert.Equal(benchmark.Iterations, benchmark.SamplesNanoseconds.Count);
        XunitAssert.True(benchmark.MinimumNanoseconds <= benchmark.MeanNanoseconds);
        XunitAssert.True(benchmark.MeanNanoseconds <= benchmark.MaximumNanoseconds);
        XunitAssert.True(benchmark.MinimumNanoseconds <= benchmark.MedianNanoseconds);
        XunitAssert.True(benchmark.MedianNanoseconds <= benchmark.MaximumNanoseconds);
        XunitAssert.True(benchmark.MinimumNanoseconds <= benchmark.LowerQuantileNanoseconds);
        XunitAssert.True(benchmark.LowerQuantileNanoseconds <= benchmark.MedianNanoseconds);
        XunitAssert.True(benchmark.StandardDeviationNanoseconds >= 0);
        XunitAssert.True(benchmark.StandardErrorNanoseconds >= 0);
        XunitAssert.True(benchmark.MedianAbsoluteDeviationNanoseconds >= 0);
        XunitAssert.True(benchmark.AllocatedBytes >= 0);
        XunitAssert.True(benchmark.Gen0Collections >= 0);
        XunitAssert.True(
            benchmark.ConfidenceIntervalLowerNanoseconds <= benchmark.MeanNanoseconds
        );
        XunitAssert.True(
            benchmark.MeanNanoseconds <= benchmark.ConfidenceIntervalUpperNanoseconds
        );
        XunitAssert.InRange(benchmark.OutlierCount, 0, benchmark.Iterations);
        XunitAssert.True(benchmark.TotalMeasurementTime > TimeSpan.Zero);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_CalibrationExcludesColdFirstInvocation()
    {
        var dispatchCount = 0;
        var metadata = new TestClassMetadata
        {
            ClassName = "ColdBenchmarkClass",
            TestMethods =
            [
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "MeasureWork",
                    BenchmarkDispatch = async (_, _, _) =>
                    {
                        if (Interlocked.Increment(ref dispatchCount) == 1)
                            await Task.Delay(50);
                    },
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var result = XunitAssert.IsType<BenchmarkResult.Completed>(
            XunitAssert.Single(
                await TestExecutionEngine.ExecuteTestsAsync(
                    [metadata],
                    new TestExecutionOptions { RunBenchmarks = true }
                )
            )
        );

        XunitAssert.True(result.Statistics.OperationsPerIteration > 1);
    }

    [XunitFact]
    public void BenchmarkAnalysis_UsesSampleStatisticsAndRetainsOutliers()
    {
        double[] samples = [1, 2, 3, 4, 5, 6, 7, 8, 9, 100];

        var statistics = BenchmarkAnalysis.Calculate(
            samples,
            operationsPerIteration: 32,
            calibrationTargetReached: true,
            warmupIterations: 5,
            measurementConverged: false,
            totalMeasurementTimestampTicks: System.Diagnostics.Stopwatch.Frequency
        );

        XunitAssert.Equal(14.5, statistics.MeanNanoseconds);
        XunitAssert.Equal(5.5, statistics.MedianNanoseconds);
        XunitAssert.Equal(1.9, statistics.LowerQuantileNanoseconds);
        XunitAssert.Equal(2.5, statistics.MedianAbsoluteDeviationNanoseconds);
        XunitAssert.Equal(1, statistics.MinimumNanoseconds);
        XunitAssert.Equal(100, statistics.MaximumNanoseconds);
        XunitAssert.Equal(1, statistics.OutlierCount);
        XunitAssert.Equal(samples, statistics.SamplesNanoseconds);
        XunitAssert.Equal(TimeSpan.FromSeconds(1), statistics.TotalMeasurementTime);
        XunitAssert.True(
            statistics.ConfidenceIntervalLowerNanoseconds
                <= statistics.MeanNanoseconds
        );
        XunitAssert.True(
            statistics.MeanNanoseconds
                <= statistics.ConfidenceIntervalUpperNanoseconds
        );
    }

    [XunitFact]
    public void BenchmarkAnalysis_FlagsDriftingSamplesAsUnstable()
    {
        // A clear regime shift between the first and second halves.
        XunitAssert.False(
            BenchmarkAnalysis.IsStable([100, 100, 100, 100, 200, 200, 200, 200])
        );
        // Steady timing across both halves.
        XunitAssert.True(
            BenchmarkAnalysis.IsStable([100, 101, 99, 100, 100, 99, 101, 100])
        );
        // Too few samples to judge; assume stable rather than cry wolf.
        XunitAssert.True(BenchmarkAnalysis.IsStable([100, 200]));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_RecalibratesBatchAfterWarmup()
    {
        // Simulate tiered compilation: the method is slow until it has been
        // dispatched enough times, then speeds up. The post-warmup
        // recalibration should grow the batch size beyond the cold pilot value.
        var dispatchCount = 0;
        var metadata = new TestClassMetadata
        {
            ClassName = "TieredBenchmarkClass",
            TestMethods =
            [
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "MeasureWork",
                    BenchmarkDispatch = async (_, _, operations) =>
                    {
                        var callIndex = Interlocked.Increment(ref dispatchCount);
                        // Cold calls are an order of magnitude slower per op.
                        var perOpMicroseconds = callIndex <= 6 ? 200 : 20;
                        var delayMs = Math.Min(operations * perOpMicroseconds / 1000, 30);
                        if (delayMs > 0)
                            await Task.Delay(delayMs);
                    },
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var result = XunitAssert.IsType<BenchmarkResult.Completed>(
            XunitAssert.Single(
                await TestExecutionEngine.ExecuteTestsAsync(
                    [metadata],
                    new TestExecutionOptions { RunBenchmarks = true }
                )
            )
        );

        XunitAssert.True(result.Statistics.OperationsPerIteration > 1);
        XunitAssert.InRange(result.Statistics.WarmupIterations, 4, 20);
    }

    [XunitFact]
    public void BenchmarkAnalysis_RequiresTenPreciseSamplesToConverge()
    {
        XunitAssert.False(
            BenchmarkAnalysis.HasMetPrecision([100, 100, 100, 100, 100, 100, 100, 100, 100])
        );
        XunitAssert.True(
            BenchmarkAnalysis.HasMetPrecision(
                [100, 100, 100, 100, 100, 100, 100, 100, 100, 100]
            )
        );
        XunitAssert.False(
            BenchmarkAnalysis.HasMetPrecision(
                [1, 100, 1, 100, 1, 100, 1, 100, 1, 100]
            )
        );
    }

    [XunitFact]
    public void BenchmarkAnalysis_ConvergesDespiteOutliersWhenMedianIsStable()
    {
        // Nine tight samples and one large outlier. The mean-based margin of
        // error would stay inflated and never converge, but the robust
        // criterion recognizes that precision has been met.
        double[] samples = [100, 100, 100, 100, 100, 100, 100, 100, 100, 300];
        XunitAssert.True(BenchmarkAnalysis.HasMetPrecision(samples));
    }

    [XunitFact]
    public void BenchmarkResultFormatter_NormalizesAllocationAndGcPerOperation()
    {
        double[] samples = [100, 100, 100, 100, 100, 100, 100, 100, 100, 100];
        var statistics = BenchmarkAnalysis.Calculate(
            samples,
            operationsPerIteration: 100,
            calibrationTargetReached: true,
            warmupIterations: 5,
            measurementConverged: true,
            totalMeasurementTimestampTicks: System.Diagnostics.Stopwatch.Frequency,
            new TestExecutionEngine.BenchmarkGcStatistics(1, 0, 0, 416_000)
        );

        var formatted = BenchmarkResultFormatter.Format(statistics);

        // 10 samples x 100 operations = 1000 operations; 416000 bytes / 1000 = 416 B/op.
        XunitAssert.Contains("Allocated: 416.00 B/op", formatted);
        // 1 gen0 collection over 1000 operations = 1.0000 per 1000 operations.
        XunitAssert.Contains("GC/1k op: 1.0000/0.0000/0.0000", formatted);
        // Median and low-quantile floor lead the summary; mean is no longer shown.
        XunitAssert.Contains("Median:", formatted);
        XunitAssert.Contains("Floor (P10):", formatted);
        XunitAssert.DoesNotContain("Mean:", formatted);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_RunsBenchmarksSequentially()
    {
        var running = 0;
        var overlapDetected = false;
        var metadata = new TestClassMetadata
        {
            ClassName = "BenchmarkClass",
            TestMethods =
            [
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "Benchmark1",
                    BenchmarkDispatch = RunBenchmarkBatch,
                },
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "Benchmark2",
                    BenchmarkDispatch = RunBenchmarkBatch,
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        async Task RunBenchmarkBatch(
            object? receiver,
            object? benchmarkArguments,
            int operations
        )
        {
            if (Interlocked.Increment(ref running) > 1)
                overlapDetected = true;
            await Task.Delay(Math.Min(operations, 25));
            Interlocked.Decrement(ref running);
        }

        var results = await TestExecutionEngine.ExecuteTestsAsync(
            [metadata],
            new TestExecutionOptions
            {
                Mode = ParallelMode.Tests,
                MaxDegreeOfParallelism = 2,
                RunBenchmarks = true,
            }
        );

        XunitAssert.Equal(2, results.Length);
        XunitAssert.False(overlapDetected);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_ExcludesBenchmarksByDefault()
    {
        var benchmarkInvocations = 0;
        var metadata = new TestClassMetadata
        {
            ClassName = "MixedClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "Test" },
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "Benchmark",
                    BenchmarkDispatch = (_, _, operations) =>
                    {
                        benchmarkInvocations += operations;
                        return Task.CompletedTask;
                    },
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, methodName, _) =>
            {
                if (methodName == "Benchmark")
                    benchmarkInvocations++;
                return Task.CompletedTask;
            },
        };

        var result = XunitAssert.IsType<TestResult.Passed>(XunitAssert.Single(
            await TestExecutionEngine.ExecuteTestsAsync(
                [metadata],
                TestExecutionOptions.Default
            )
        ));

        XunitAssert.Equal("Test", result.Name);
        XunitAssert.Equal(0, benchmarkInvocations);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_RunBenchmarksExcludesTests()
    {
        var testInvocations = 0;
        var metadata = new TestClassMetadata
        {
            ClassName = "MixedClass",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "Test" },
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "Benchmark",
                    BenchmarkDispatch = (_, _, _) => Task.CompletedTask,
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, methodName, _) =>
            {
                if (methodName == "Test")
                    testInvocations++;
                return Task.CompletedTask;
            },
        };

        var result = XunitAssert.IsType<BenchmarkResult.Completed>(
            XunitAssert.Single(
                await TestExecutionEngine.ExecuteTestsAsync(
                    [metadata],
                    new TestExecutionOptions { RunBenchmarks = true }
                )
            )
        );

        XunitAssert.Equal("Benchmark", result.Name);
        XunitAssert.Equal(0, testInvocations);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_InstanceBenchmarkReusesAndDisposesOneInstance()
    {
        var instancesCreated = 0;
        var instancesDisposed = 0;
        var invocations = 0;
        object? benchmarkInstance = null;
        var reusedInstance = true;
        var metadata = new TestClassMetadata
        {
            ClassName = "InstanceBenchmarkClass",
            TestMethods =
            [
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "Benchmark",
                    IsStatic = false,
                    BenchmarkDispatch = (receiver, _, operations) =>
                    {
                        benchmarkInstance ??= receiver;
                        reusedInstance &= ReferenceEquals(benchmarkInstance, receiver);
                        invocations += operations;
                        return Task.CompletedTask;
                    },
                },
            ],
            CreateInstance = () =>
            {
                instancesCreated++;
                return new DisposableBenchmarkInstance(() => instancesDisposed++);
            },
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var result = XunitAssert.Single(
            await TestExecutionEngine.ExecuteTestsAsync(
                [metadata],
                new TestExecutionOptions { RunBenchmarks = true }
            )
        );

        XunitAssert.IsType<BenchmarkResult.Completed>(result);
        XunitAssert.Equal(1, instancesCreated);
        XunitAssert.Equal(1, instancesDisposed);
        XunitAssert.True(invocations > 13);
        XunitAssert.True(reusedInstance);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_ParameterizedBenchmarkRunsEachCaseIndependently()
    {
        var instancesCreated = 0;
        var instancesDisposed = 0;
        var receivers = new HashSet<object>();
        var arguments = new HashSet<int>();
        var metadata = new TestClassMetadata
        {
            ClassName = "ParameterizedBenchmarkClass",
            TestMethods =
            [
                new TestMethodMetadata.Benchmark
                {
                    MethodName = "Benchmark",
                    IsStatic = false,
                    TestCases =
                    [
                        new TestCaseInfo { Arguments = 16, DisplayName = "size: 16" },
                        new TestCaseInfo { Arguments = 64, DisplayName = "size: 64" },
                    ],
                    BenchmarkDispatch = (receiver, benchmarkArguments, _) =>
                    {
                        receivers.Add(receiver!);
                        arguments.Add((int)benchmarkArguments!);
                        return Task.CompletedTask;
                    },
                },
            ],
            CreateInstance = () =>
            {
                instancesCreated++;
                return new DisposableBenchmarkInstance(() => instancesDisposed++);
            },
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var results = await TestExecutionEngine.ExecuteTestsAsync(
            [metadata],
            new TestExecutionOptions { RunBenchmarks = true }
        );

        XunitAssert.Equal(2, results.Length);
        XunitAssert.All(
            results,
            result => XunitAssert.IsType<BenchmarkResult.Completed>(result)
        );
        XunitAssert.Equal(
            ["Benchmark(size: 16)", "Benchmark(size: 64)"],
            results.Select(result => result.Name).Order()
        );
        XunitAssert.Equal(2, instancesCreated);
        XunitAssert.Equal(2, instancesDisposed);
        XunitAssert.Equal(2, receivers.Count);
        XunitAssert.Equal([16, 64], arguments.Order());
    }

    private static void AssertAllTestsPassed(RunResult[] results)
    {
        XunitAssert.All(
            results,
            result => XunitAssert.IsType<TestResult.Passed>(result)
        );
    }

    private sealed class DisposableBenchmarkInstance(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
