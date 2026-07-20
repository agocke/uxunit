
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using static NXTest.RunResult;

namespace NXTest.Runtime;

public sealed class TestFramework : ITestFramework, IDataProducer
{
    private const string s_benchmarkArgument = "--bench";
    private const string s_displayName = "nxtest Microsoft.Testing.Platform framework";

    public string Uid => "64e8dd3a-ae2c-448f-9481-587f0252bfb8";

    public string Version => "1.0.0";

    public string DisplayName => s_displayName;

    public string Description => s_displayName;

    public Type[] DataTypesProduced { get; } = [ typeof(TestNodeUpdateMessage) ];

    private readonly TestClassMetadata[] _testClasses;
    private TestExecutionOptions _options;
    private readonly bool _runBenchmarks;
    private readonly CancellationToken _cancellationToken;
    private readonly List<BenchmarkResult> _benchmarkResults = [];


    private TestFramework(
        TestClassMetadata[] testClasses,
        TestExecutionOptions options,
        bool runBenchmarks,
        CancellationToken cancellationToken
    )
    {
        _testClasses = testClasses;
        _options = options;
        _runBenchmarks = runBenchmarks;
        _cancellationToken = cancellationToken;
    }

    private sealed class TrxReportCapability : ITrxReportCapability
    {
        public bool IsSupported => true;

        public void Enable()
        {
            // No state to manage for this simple implementation
        }
    }

    public static async Task<int> RunAsync(
        string[] args,
        TestClassMetadata[] testClasses,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var runBenchmarks =
            options?.RunBenchmarks == true
            || args.Contains(s_benchmarkArgument, StringComparer.Ordinal);
        var platformArgs = GetPlatformArguments(args, runBenchmarks);

        var builder = await TestApplication.CreateBuilderAsync(platformArgs);
        builder.AddTrxReportProvider();
        builder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(new TrxReportCapability()),
            (capabilities, serviceProvider) =>
            {
                return new TestFramework(
                    testClasses,
                    options ?? new TestExecutionOptions(),
                    runBenchmarks,
                    cancellationToken
                );
            }
        );

        var app = await builder.BuildAsync();
        return await app.RunAsync();
    }

    internal static string[] GetPlatformArguments(string[] args, bool runBenchmarks)
    {
        // Benchmark results are presented through our own summary table
        // (see PrintBenchmarkSummary), so the platform output is left at its
        // default verbosity to keep the run quiet. We only strip our own
        // custom flag before handing the arguments to the platform.
        return args
            .Where(arg => !string.Equals(arg, s_benchmarkArgument, StringComparison.Ordinal))
            .ToArray();
    }

    public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        // No special teardown needed for NXTest
        return new CloseTestSessionResult() { IsSuccess = true };
    }

    public async Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        // No special setup needed for NXTest
        return new CreateTestSessionResult() { IsSuccess = true };
    }

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        if (context.Request is DiscoverTestExecutionRequest)
        {
            throw new System.NotImplementedException();
        }
        else if (context.Request is RunTestExecutionRequest)
        {
            // Filters are not supported yet
            try
            {
                // Run all the tests and publish the results to the MTP IMessageBus
                await ExecuteTestsAsync(context);
            }
            finally
            {
                context.Complete();
            }
        }
    }

    private async Task ExecuteTestsAsync(ExecuteRequestContext context)
    {
        var stopCts = new CancellationTokenSource();
        var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(
            _cancellationToken,
            stopCts.Token,
            context.CancellationToken
        ).Token;

        var tests = TestExecutionEngine.CollectAllTests(_testClasses);
        if (_runBenchmarks)
        {
            tests.RemoveAll(
                test => test.Method is not TestMethodMetadata.Benchmark
            );
            foreach (var benchmark in tests)
            {
                if (linkedCt.IsCancellationRequested)
                    break;

                await RunAndPublishAsync(context, benchmark, stopCts, linkedCt);
            }

            PrintBenchmarkSummary();
            return;
        }

        tests.RemoveAll(test => test.Method is TestMethodMetadata.Benchmark);

        switch (_options.Mode)
        {
            case ParallelMode.None:
                foreach (var test in tests)
                {
                    if (linkedCt.IsCancellationRequested)
                        break;

                    await RunAndPublishAsync(context, test, stopCts, linkedCt);
                }
                break;

            case ParallelMode.Classes:
                var classGroups = tests.GroupBy(t => t.ClassName, StringComparer.Ordinal);

                await Parallel.ForEachAsync(
                    classGroups,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                        CancellationToken = linkedCt,
                    },
                    async (group, ct) =>
                    {
                        foreach (var test in group)
                        {
                            if (ct.IsCancellationRequested)
                                break;

                            await RunAndPublishAsync(context, test, stopCts, ct);
                        }
                    }
                );
                break;

            default: // ParallelMode.Tests
                await Parallel.ForEachAsync(
                    tests,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                        CancellationToken = linkedCt,
                    },
                    async (test, ct) => await RunAndPublishAsync(context, test, stopCts, ct)
                );
                break;
        }

    }

    private async Task RunAndPublishAsync(
        ExecuteRequestContext context,
        TestExecutionEngine.TestDescriptor test,
        CancellationTokenSource stopCts,
        CancellationToken ct
    )
    {
        var results = await TestExecutionEngine.ExecuteTestAsync(test, _options, ct);

        if (_options.StopOnFirstFailure && results.Any(TestExecutionEngine.IsFailure))
            stopCts.Cancel();

        await PublishRunResultsAsync(context, results);
    }

    private async Task PublishRunResultsAsync(ExecuteRequestContext context, RunResult[] results)
    {
        foreach (var result in results)
        {
            if (result is BenchmarkResult benchmarkResult)
                _benchmarkResults.Add(benchmarkResult);

            var testNode = result switch
            {
                TestResult testResult => CreateTestNode(testResult),
                BenchmarkResult br => CreateBenchmarkNode(br),
                _ => throw new InvalidOperationException(
                    $"Unknown run result type: {result.GetType()}"
                ),
            };
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, testNode));
        }
    }

    private void PrintBenchmarkSummary()
    {
        if (_benchmarkResults.Count == 0)
            return;

        Console.WriteLine();
        Console.Write(BenchmarkSummaryFormatter.FormatSummary(_benchmarkResults));
    }

    private static TestNode CreateTestNode(TestResult result)
    {
        var fullyQualifiedName = $"{result.ClassDisplayName ?? result.ClassName}.{result.Name}";
        return result switch
        {
            TestResult.Skipped skipped => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(new SkippedTestNodeStateProperty(skipped.Reason))
            },
            TestResult.Passed => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(new PassedTestNodeStateProperty())
            },
            TestResult.Failed failed => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(
                    new FailedTestNodeStateProperty(failed.ErrorMessage),
                    new TrxExceptionProperty(failed.ErrorMessage, failed.StackTrace)
                )
            },
            TestResult.Faulted faulted => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(
                    new ErrorTestNodeStateProperty(faulted.ErrorMessage),
                    new TrxExceptionProperty(faulted.ErrorMessage, faulted.StackTrace)
                )
            },
        };
    }

    private static TestNode CreateBenchmarkNode(BenchmarkResult result)
    {
        var fullyQualifiedName = $"{result.ClassDisplayName ?? result.ClassName}.{result.Name}";
        return result switch
        {
            BenchmarkResult.Completed completed => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(new PassedTestNodeStateProperty())
            },
            BenchmarkResult.Failed failed => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(
                    new FailedTestNodeStateProperty(failed.ErrorMessage),
                    new TrxExceptionProperty(failed.ErrorMessage, failed.StackTrace)
                )
            },
            BenchmarkResult.Skipped skipped => new TestNode()
            {
                Uid = fullyQualifiedName,
                DisplayName = fullyQualifiedName,
                Properties = new PropertyBag(
                    new SkippedTestNodeStateProperty(skipped.Reason)
                )
            },
        };
    }

    public async Task<bool> IsEnabledAsync() => true;
}