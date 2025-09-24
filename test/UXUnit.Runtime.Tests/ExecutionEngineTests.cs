using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UXUnit.Runtime;
using Xunit;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Meta-tests that validate the UXUnit execution engine itself.
/// These are XUnit tests that test our UXUnit runtime.
/// </summary>
public class ExecutionEngineTests
{
    [Fact]
    public async Task TestRunner_WithPassingTest_ShouldReturnPassedResult()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] { new PassingTestRunner() };
        var config = new TestRunConfiguration { Output = output };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert
        Assert.Single(result.TestResults);
        Assert.Equal(TestStatus.Passed, result.TestResults[0].Status);
        Assert.Equal("PassingMethod", result.TestResults[0].TestName);
        Assert.False(result.HasFailures);
        Assert.Equal(1, result.PassedTests);
        Assert.Equal(0, result.FailedTests);
    }

    [Fact]
    public async Task TestRunner_WithFailingTest_ShouldReturnFailedResult()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] { new FailingTestRunner() };
        var config = new TestRunConfiguration { Output = output };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert
        Assert.Single(result.TestResults);
        Assert.Equal(TestStatus.Failed, result.TestResults[0].Status);
        Assert.Equal("FailingMethod", result.TestResults[0].TestName);
        Assert.True(result.HasFailures);
        Assert.Equal(0, result.PassedTests);
        Assert.Equal(1, result.FailedTests);
        Assert.Contains("Intentional failure", result.TestResults[0].ErrorMessage);
        Assert.Equal("System.InvalidOperationException", result.TestResults[0].ErrorType);
    }

    [Fact]
    public async Task TestRunner_WithSkippedTest_ShouldReturnSkippedResult()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] { new SkippedTestRunner() };
        var config = new TestRunConfiguration { Output = output };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert
        Assert.Single(result.TestResults);
        Assert.Equal(TestStatus.Skipped, result.TestResults[0].Status);
        Assert.Equal("SkippedMethod", result.TestResults[0].TestName);
        Assert.False(result.HasFailures);
        Assert.Equal(0, result.PassedTests);
        Assert.Equal(0, result.FailedTests);
        Assert.Equal(1, result.SkippedTests);
        Assert.Equal("Test intentionally skipped", result.TestResults[0].SkipReason);
    }

    [Fact]
    public async Task TestRunner_WithAsyncTest_ShouldExecuteCorrectly()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] { new AsyncTestRunner() };
        var config = new TestRunConfiguration { Output = output };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert
        Assert.Single(result.TestResults);
        Assert.Equal(TestStatus.Passed, result.TestResults[0].Status);
        Assert.Equal("AsyncMethod", result.TestResults[0].TestName);
        Assert.True(result.TestResults[0].Duration.TotalMilliseconds >= 50); // Should take at least 50ms due to delay
    }

    [Fact]
    public async Task TestRunner_WithParameterizedTest_ShouldExecuteAllCases()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] { new ParameterizedTestRunner() };
        var config = new TestRunConfiguration { Output = output };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert
        Assert.Single(result.TestResults); // Parameterized tests return one result (first failure or last success)
        
        // Check that the test case arguments were used
        var testResult = result.TestResults[0];
        Assert.NotNull(testResult.TestCaseArguments);
        Assert.Equal(3, testResult.TestCaseArguments.Length);
    }

    [Fact]
    public async Task TestRunner_WithMixedResults_ShouldReturnCorrectSummary()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] 
        { 
            new PassingTestRunner(),
            new FailingTestRunner(),
            new SkippedTestRunner(),
            new AsyncTestRunner()
        };
        var config = new TestRunConfiguration { Output = output };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert
        Assert.Equal(4, result.TestResults.Count);
        Assert.Equal(2, result.PassedTests); // PassingTest + AsyncTest
        Assert.Equal(1, result.FailedTests); // FailingTest
        Assert.Equal(1, result.SkippedTests); // SkippedTest
        Assert.True(result.HasFailures);
        Assert.Equal(0.5, result.Summary.PassRate); // 2 passed out of 4 total
    }

    [Fact]
    public async Task TestRunner_WithStopOnFirstFailure_ShouldStopAfterFirstFailure()
    {
        // Arrange
        var output = new BufferedTestOutput();
        var runner = new TestRunner(output);
        var testRunners = new ITestClassRunner[] 
        { 
            new PassingTestRunner(),
            new FailingTestRunner(),
            new PassingTestRunner() // This shouldn't run
        };
        var config = new TestRunConfiguration 
        { 
            Output = output,
            StopOnFirstFailure = true,
            ParallelExecution = false // Sequential to ensure order
        };

        // Act
        var result = await runner.RunTestsAsync(testRunners, config);

        // Assert - Should have stopped after the failing test
        Assert.True(result.HasFailures);
        var outputText = output.GetOutput();
        Assert.Contains("Stopping execution on first failure", outputText);
    }

    [Fact]
    public void TestDiscovery_WithManualRunners_ShouldFindRunners()
    {
        // Arrange
        var runners = new ITestClassRunner[]
        {
            new PassingTestRunner(),
            new FailingTestRunner()
        };

        // Act
        var summary = TestDiscovery.GetDiscoverySummary(runners);

        // Assert
        Assert.Equal(2, summary.TotalClasses);
        Assert.Equal(2, summary.TotalMethods);
        Assert.Equal(2, summary.TotalTestCases);
    }
}

// Helper test runners for validation

public class PassingTestRunner : TestClassRunnerBase
{
    public override TestClassMetadata Metadata => new()
    {
        ClassName = "PassingTestClass",
        TestMethods = new[] { new TestMethodMetadata { MethodName = "PassingMethod" } }
    };

    protected override object CreateTestInstance() => new PassingTestClass();
    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "PassingMethod" => (testInstance) => { 
                ((PassingTestClass)testInstance).PassingMethod(); 
                return Task.CompletedTask; 
            },
            _ => throw new InvalidOperationException($"Unknown test method: {methodName}")
        };
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(string methodName)
    {
        throw new InvalidOperationException($"No parameterized test methods in this runner: {methodName}");
    }
}

public class PassingTestClass
{
    public void PassingMethod()
    {
        // This test always passes
        var result = 2 + 2;
        if (result != 4) throw new Exception("Math is broken!");
    }
}

public class FailingTestRunner : TestClassRunnerBase
{
    public override TestClassMetadata Metadata => new()
    {
        ClassName = "FailingTestClass",
        TestMethods = new[] { new TestMethodMetadata { MethodName = "FailingMethod" } }
    };

    protected override object CreateTestInstance() => new FailingTestClass();
    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "FailingMethod" => (testInstance) => { 
                ((FailingTestClass)testInstance).FailingMethod(); 
                return Task.CompletedTask; 
            },
            _ => throw new InvalidOperationException($"Unknown test method: {methodName}")
        };
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(string methodName)
    {
        throw new InvalidOperationException($"No parameterized test methods in this runner: {methodName}");
    }
}

public class FailingTestClass
{
    public void FailingMethod()
    {
        throw new InvalidOperationException("Intentional failure for testing");
    }
}

public class SkippedTestRunner : TestClassRunnerBase
{
    public override TestClassMetadata Metadata => new()
    {
        ClassName = "SkippedTestClass",
        TestMethods = new[] { new TestMethodMetadata 
        { 
            MethodName = "SkippedMethod",
            Skip = true,
            SkipReason = "Test intentionally skipped"
        } }
    };

    protected override object CreateTestInstance() => new SkippedTestClass();
    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "SkippedMethod" => (testInstance) => { 
                ((SkippedTestClass)testInstance).SkippedMethod(); 
                return Task.CompletedTask; 
            },
            _ => throw new InvalidOperationException($"Unknown test method: {methodName}")
        };
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(string methodName)
    {
        throw new InvalidOperationException($"No parameterized test methods in this runner: {methodName}");
    }
}

public class SkippedTestClass
{
    public void SkippedMethod()
    {
        throw new Exception("This should never execute!");
    }
}

public class AsyncTestRunner : TestClassRunnerBase
{
    public override TestClassMetadata Metadata => new()
    {
        ClassName = "AsyncTestClass",
        TestMethods = new[] { new TestMethodMetadata 
        { 
            MethodName = "AsyncMethod",
            IsAsync = true
        } }
    };

    protected override object CreateTestInstance() => new AsyncTestClass();
    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "AsyncMethod" => (testInstance) => ((AsyncTestClass)testInstance).AsyncMethod(),
            _ => throw new InvalidOperationException($"Unknown test method: {methodName}")
        };
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(string methodName)
    {
        throw new InvalidOperationException($"No parameterized test methods in this runner: {methodName}");
    }
}

public class AsyncTestClass
{
    public async Task AsyncMethod()
    {
        await Task.Delay(50); // Simulate async work
        var result = await GetResultAsync();
        if (result != "success") throw new Exception("Async test failed");
    }

    private async Task<string> GetResultAsync()
    {
        await Task.Delay(10);
        return "success";
    }
}

public class ParameterizedTestRunner : TestClassRunnerBase
{
    public override TestClassMetadata Metadata => new()
    {
        ClassName = "ParameterizedTestClass",
        TestMethods = new[] { new TestMethodMetadata 
        { 
            MethodName = "ParameterizedMethod",
            TestCases = new[]
            {
                new TestCaseMetadata { Arguments = new object[] { 1, 2, 3 } },
                new TestCaseMetadata { Arguments = new object[] { 5, 5, 10 } },
                new TestCaseMetadata { Arguments = new object[] { 0, 100, 100 } }
            }
        } }
    };

    protected override object CreateTestInstance() => new ParameterizedTestClass();
    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        throw new InvalidOperationException($"No simple test methods in this runner: {methodName}");
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "ParameterizedMethod" => (testInstance, arguments) => { 
                ((ParameterizedTestClass)testInstance).ParameterizedMethod((int)arguments[0]!, (int)arguments[1]!, (int)arguments[2]!); 
                return Task.CompletedTask; 
            },
            _ => throw new InvalidOperationException($"Unknown parameterized test method: {methodName}")
        };
    }
}

public class ParameterizedTestClass
{
    public void ParameterizedMethod(int a, int b, int expected)
    {
        var result = a + b;
        if (result != expected) 
            throw new Exception($"Expected {expected}, got {result}");
    }
}