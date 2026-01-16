
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace UXUnit.Runtime;

public sealed class TestFramework : ITestFramework, IDataProducer
{
    private const string s_displayName = "uxunit Microsoft.Testing.Platform framework";

    public string Uid => "64e8dd3a-ae2c-448f-9481-587f0252bfb8";

    public string Version => "1.0.0";

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
        builder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(new TrxReportCapability()),
            (capabilities, serviceProvider) =>
            {
                return new TestFramework(testClasses, options ?? new TestExecutionOptions(), cancellationToken);
            }
        );

        var app = await builder.BuildAsync();
        return await app.RunAsync();
    }

    public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        // No special teardown needed for UXUnit
        return new CloseTestSessionResult() { IsSuccess = true };
    }

    public async Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        // No special setup needed for UXUnit
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

        var testMethods = TestExecutionEngine.CollectAllTests(_testClasses);

        await Parallel.ForEachAsync(
            testMethods,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                CancellationToken = linkedCt,
            },
            async (test, ct) =>
            {
                var result = await TestExecutionEngine.ExecuteTestAsync(test, _options, ct);

                if (_options.StopOnFirstFailure && result.Status == TestStatus.Failed)
                    stopCts.Cancel();

                await PublishTestResultAsync(context, result);
            }
        );
    }

    private async Task PublishTestResultAsync(ExecuteRequestContext context, TestResult result)
    {
        var testfqn = $"{result.ClassDisplayName ?? result.ClassName}.{result.TestName}";
        TestNode testNode = result.Status switch
        {
            TestStatus.Skipped => new TestNode()
            {
                Uid = testfqn,
                DisplayName = testfqn,
                Properties = new PropertyBag(new SkippedTestNodeStateProperty(result.SkipReason ?? ""))
            },
            TestStatus.Passed => new TestNode()
            {
                Uid = testfqn,
                DisplayName = testfqn,
                Properties = new PropertyBag(new PassedTestNodeStateProperty())
            },
            TestStatus.Failed => new TestNode()
            {
                Uid = testfqn,
                DisplayName = testfqn,
                Properties = new PropertyBag(
                    new FailedTestNodeStateProperty(result.ErrorMessage!),
                    new TrxExceptionProperty(result.ErrorMessage, result.StackTrace)
                )
            },
            _ => throw new InvalidOperationException($"Unknown test status {result.Status}")
        };
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(context.Request.Session.SessionUid, testNode));
    }

    public async Task<bool> IsEnabledAsync() => true;
}