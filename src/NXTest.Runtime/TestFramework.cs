
using System;
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
using Microsoft.Testing.Platform.TestHost;

namespace NXTest.Runtime;

public sealed class TestFramework : ITestFramework, IDataProducer
{
    private const string s_displayName = "nxtest Microsoft.Testing.Platform framework";

    public string Uid => "64e8dd3a-ae2c-448f-9481-587f0252bfb8";

    public string Version => "0.1.1";

    public string DisplayName => s_displayName;

    public string Description => s_displayName;

    public Type[] DataTypesProduced { get; } = [ typeof(TestNodeUpdateMessage) ];

    private readonly TestClassMetadata[] _testClasses;
    private TestExecutionOptions _options;
    private readonly CancellationToken _cancellationToken;


    private TestFramework(
        TestClassMetadata[] testClasses,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        _testClasses = testClasses;
        _options = options;
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
        var builder = await TestApplication.CreateBuilderAsync(args);
        builder.AddTrxReportProvider();
        builder.AddNXTest(testClasses, options, cancellationToken);

        var app = await builder.BuildAsync();
        return await app.RunAsync();
    }

    /// <summary>
    /// Registers the NXTest framework with a Microsoft.Testing.Platform application
    /// builder. This is the integration point used by the auto-generated MTP entry point
    /// so tests can be run without a hand-written <c>Main</c>. TRX reporting is contributed
    /// separately (by the platform's self-registration, or by <see cref="RunAsync"/> for
    /// manually hosted builders).
    /// </summary>
    internal static void Register(
        ITestApplicationBuilder builder,
        TestClassMetadata[] testClasses,
        TestExecutionOptions? options,
        CancellationToken cancellationToken
    )
    {
        builder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(new TrxReportCapability()),
            (capabilities, serviceProvider) =>
            {
                return new TestFramework(testClasses, options ?? new TestExecutionOptions(), cancellationToken);
            }
        );
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
            try
            {
                // Enumerate all tests and publish them as discovered nodes (no execution).
                await DiscoverTestsAsync(context);
            }
            finally
            {
                context.Complete();
            }
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

    private async Task DiscoverTestsAsync(ExecuteRequestContext context)
    {
        var sessionUid = context.Request.Session.SessionUid;

        // Walk test classes in parallel; the message bus accepts concurrent publishes (the run
        // path already relies on this). Discovery has no ordering dependencies, so the run-time
        // shuffle is intentionally skipped here.
        await Parallel.ForEachAsync(
            _testClasses,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                CancellationToken = context.CancellationToken,
            },
            async (testClass, ct) =>
            {
                foreach (var method in testClass.TestMethods)
                {
                    if (ct.IsCancellationRequested)
                        return;

                    // A skipped test (fact or theory) runs as a single node named after the
                    // method, so discovery must mirror that to keep Uids correlated with a run.
                    if (method.Skip)
                    {
                        await PublishDiscoveredNodeAsync(
                            context, sessionUid, testClass.ClassName, $"{testClass.ClassName}.{method.MethodName}");
                        continue;
                    }

                    switch (method)
                    {
                        case TestMethodMetadata.Theory theory:
                            foreach (var testCase in theory.TestCases)
                            {
                                await PublishDiscoveredNodeAsync(
                                    context,
                                    sessionUid,
                                    testClass.ClassName,
                                    $"{testClass.ClassName}.{theory.MethodName}({testCase.DisplayName})"
                                );
                            }
                            break;

                        default:
                            await PublishDiscoveredNodeAsync(
                                context, sessionUid, testClass.ClassName, $"{testClass.ClassName}.{method.MethodName}");
                            break;
                    }
                }
            }
        );
    }

    // Publishes a single discovered TestNode. The fully-qualified name matches what a run
    // produces as TestNode Uid/DisplayName so discovered nodes correlate with executed ones.
    private async Task PublishDiscoveredNodeAsync(
        ExecuteRequestContext context,
        SessionUid sessionUid,
        string className,
        string fqn
    )
    {
        var testNode = new TestNode()
        {
            Uid = fqn,
            DisplayName = fqn,
            Properties = new PropertyBag(
                new TrxFullyQualifiedTypeNameProperty(className),
                DiscoveredTestNodeStateProperty.CachedInstance
            )
        };
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(sessionUid, testNode));
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

        if (_options.StopOnFirstFailure && results.Any(r => r.Status == TestStatus.Failed))
            stopCts.Cancel();

        await PublishTestResultsAsync(context, results);
    }

    private async Task PublishTestResultsAsync(ExecuteRequestContext context, TestResult[] results)
    {
        foreach (var result in results)
        {
            var testfqn = $"{result.ClassDisplayName ?? result.ClassName}.{result.TestName}";
            var trxTypeName = new TrxFullyQualifiedTypeNameProperty(result.ClassName);
            TestNode testNode = result.Status switch
            {
                TestStatus.Skipped => new TestNode()
                {
                    Uid = testfqn,
                    DisplayName = testfqn,
                    Properties = new PropertyBag(trxTypeName, new SkippedTestNodeStateProperty(result.SkipReason ?? ""))
                },
                TestStatus.Passed => new TestNode()
                {
                    Uid = testfqn,
                    DisplayName = testfqn,
                    Properties = new PropertyBag(trxTypeName, new PassedTestNodeStateProperty())
                },
                TestStatus.Failed => new TestNode()
                {
                    Uid = testfqn,
                    DisplayName = testfqn,
                    Properties = new PropertyBag(
                        trxTypeName,
                        new FailedTestNodeStateProperty(result.ErrorMessage!),
                        new TrxExceptionProperty(result.ErrorMessage, result.StackTrace)
                    )
                },
                TestStatus.Faulted => new TestNode()
                {
                    Uid = testfqn,
                    DisplayName = testfqn,
                    Properties = new PropertyBag(
                        trxTypeName,
                        new ErrorTestNodeStateProperty(result.ErrorMessage ?? "Test infrastructure fault"),
                        new TrxExceptionProperty(result.ErrorMessage, result.StackTrace)
                    )
                }
            };
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, testNode));
        }
    }

    public async Task<bool> IsEnabledAsync() => true;
}