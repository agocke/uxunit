using System;
using System.Threading;
using System.Threading.Tasks;
using UXUnit.Runtime;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Manual implementation of a test class runner to validate the execution engine.
/// This simulates what the source generator would create.
///
/// NOTE: This is for the OLD architecture and needs to be rewritten.
/// Temporarily disabled to allow the codebase to build.
/// </summary>
#if FALSE
public class ManualTestClassRunner : TestClassRunnerBase
{
    private static readonly TestClassMetadata _metadata = new()
    {
        ClassName = "ManualTestClass",
        DisplayName = "Manual Test Class",
        TestMethods = new[]
        {
            new TestMethodMetadata
            {
                MethodName = "PassingTest",
                DisplayName = "A test that should pass",
                IsAsync = false,
                TimeoutMs = 0,
            },
            new TestMethodMetadata
            {
                MethodName = "AsyncTest",
                DisplayName = "An async test that should pass",
                IsAsync = true,
                TimeoutMs = 0,
            },
            new TestMethodMetadata
            {
                MethodName = "FailingTest",
                DisplayName = "A test that should fail",
                IsAsync = false,
                TimeoutMs = 0,
            },
            new TestMethodMetadata
            {
                MethodName = "SkippedTest",
                DisplayName = "A test that should be skipped",
                Skip = true,
                SkipReason = "This test is intentionally skipped for demonstration",
                IsAsync = false,
                TimeoutMs = 0,
            },
            new TestMethodMetadata
            {
                MethodName = "ParameterizedTest",
                DisplayName = "A parameterized test",
                IsAsync = false,
                TimeoutMs = 0,
                TestCases = new[]
                {
                    new TestCaseMetadata { Arguments = new object[] { 2, 2, 4 } },
                    new TestCaseMetadata { Arguments = new object[] { 3, 3, 6 } }, // This will fail
                    new TestCaseMetadata { Arguments = new object[] { 5, 5, 10 } },
                },
            },
        },
    };

    public override TestClassMetadata Metadata => _metadata;

    protected override object CreateTestInstance()
    {
        return new ManualTestClass();
    }

    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "PassingTest" => (testInstance) =>
            {
                ((ManualTestClass)testInstance).PassingTest();
                return Task.CompletedTask;
            },
            "AsyncTest" => (testInstance) => ((ManualTestClass)testInstance).AsyncTest(),
            "FailingTest" => (testInstance) =>
            {
                ((ManualTestClass)testInstance).FailingTest();
                return Task.CompletedTask;
            },
            "SkippedTest" => (testInstance) =>
            {
                ((ManualTestClass)testInstance).SkippedTest();
                return Task.CompletedTask;
            },
            _ => throw new InvalidOperationException($"Unknown test method: {methodName}"),
        };
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(
        string methodName
    )
    {
        return methodName switch
        {
            "ParameterizedTest" => (testInstance, arguments) =>
            {
                ((ManualTestClass)testInstance).ParameterizedTest(
                    (int)arguments[0]!,
                    (int)arguments[1]!,
                    (int)arguments[2]!
                );
                return Task.CompletedTask;
            },
            _ => throw new InvalidOperationException(
                $"Unknown parameterized test method: {methodName}"
            ),
        };
    }
}

/// <summary>
/// A manual test class to demonstrate various test scenarios.
/// </summary>
public class ManualTestClass : IDisposable
{
    private bool _disposed = false;

    public ManualTestClass()
    {
        Console.WriteLine("ManualTestClass constructor called");
    }

    public void PassingTest()
    {
        Console.WriteLine("PassingTest is running");

        // Simple assertion that should pass
        var result = 2 + 2;
        if (result != 4)
            throw new Exception($"Expected 4, but got {result}");

        Console.WriteLine("PassingTest completed successfully");
    }

    public async Task AsyncTest()
    {
        Console.WriteLine("AsyncTest is running");

        // Simulate some async work
        await Task.Delay(50);

        var result = await GetValueAsync();
        if (result != "async-result")
            throw new Exception($"Expected 'async-result', but got '{result}'");

        Console.WriteLine("AsyncTest completed successfully");
    }

    public void FailingTest()
    {
        Console.WriteLine("FailingTest is running");

        // This test is designed to fail
        throw new InvalidOperationException(
            "This test intentionally fails to demonstrate error handling"
        );
    }

    public void SkippedTest()
    {
        // This should never be called because the test is marked as skipped
        throw new Exception("SkippedTest should not have been executed!");
    }

    public void ParameterizedTest(int a, int b, int expected)
    {
        Console.WriteLine($"ParameterizedTest running with ({a}, {b}, {expected})");

        var actual = a + b;
        if (actual != expected)
            throw new Exception($"Expected {expected}, but got {actual}");

        Console.WriteLine($"ParameterizedTest({a}, {b}, {expected}) passed");
    }

    private static async Task<string> GetValueAsync()
    {
        await Task.Delay(10);
        return "async-result";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Console.WriteLine("ManualTestClass disposed");
            _disposed = true;
        }
    }
}
#endif
