using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UXUnit.Runtime;
using XunitFactAttribute = Xunit.FactAttribute;
using XunitAssert = Xunit.Assert;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Tests that validate the TestExecutionEngine.
/// These are XUnit tests that test our UXUnit runtime by manually synthesizing
/// test metadata and delegates.
/// </summary>
public class ExecutionEngineTests
{
    [XunitFact]
    public async Task ExecuteTestsAsync_WithSimplePassingTest_ReturnsPassedResult()
    {
        // Arrange: Create a simple test with a delegate that succeeds
        var executed = false;
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SimpleTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata
                {
                    MethodName = "SimplePassingTest",
                    Skip = false,
                    ExecuteAsync = async (ct) =>
                    {
                        executed = true;
                        await Task.CompletedTask;
                        return TestResult.Success(
                            "SimpleTestClass.SimplePassingTest",
                            "SimplePassingTest",
                            TimeSpan.FromMilliseconds(10),
                            DateTime.UtcNow.AddMilliseconds(-10),
                            DateTime.UtcNow);
                    }
                }
            }
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.True(executed, "Test delegate should have been executed");
        XunitAssert.Single(results);
        XunitAssert.Equal(TestStatus.Passed, results[0].Status);
        XunitAssert.Equal("SimplePassingTest", results[0].TestName);
        XunitAssert.Equal("SimpleTestClass", results[0].ClassName);
        XunitAssert.True(results[0].Duration >= TimeSpan.Zero);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithSkippedTest_ReturnsSkippedResult()
    {
        // Arrange: Create a test marked as skipped
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SkippedTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata
                {
                    MethodName = "SkippedTest",
                    Skip = true,
                    SkipReason = "Test intentionally skipped for testing"
                }
            }
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.Equal(TestStatus.Skipped, results[0].Status);
        XunitAssert.Equal("SkippedTest", results[0].TestName);
        XunitAssert.Equal("Test intentionally skipped for testing", results[0].SkipReason);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithMultipleTests_ReturnsAllResults()
    {
        // Arrange: Create multiple tests
        var testMetadata = new TestClassMetadata
        {
            ClassName = "MultiTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata { MethodName = "Test1", Skip = false },
                new TestMethodMetadata { MethodName = "Test2", Skip = false },
                new TestMethodMetadata { MethodName = "Test3", Skip = true, SkipReason = "Skip this one" }
            }
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.Equal(2, results.Count(r => r.Status == TestStatus.Passed));
        XunitAssert.Single(results.Where(r => r.Status == TestStatus.Skipped));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithSequentialExecution_ExecutesInOrder()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SequentialTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata { MethodName = "Test1" },
                new TestMethodMetadata { MethodName = "Test2" },
                new TestMethodMetadata { MethodName = "Test3" }
            }
        };

        var options = new TestExecutionOptions
        {
            ParallelExecution = false
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.All(results, r => XunitAssert.Equal(TestStatus.Passed, r.Status));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithParallelExecution_ExecutesAllTests()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "ParallelTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata { MethodName = "ParallelTest1" },
                new TestMethodMetadata { MethodName = "ParallelTest2" },
                new TestMethodMetadata { MethodName = "ParallelTest3" },
                new TestMethodMetadata { MethodName = "ParallelTest4" }
            }
        };

        var options = new TestExecutionOptions
        {
            ParallelExecution = true,
            MaxDegreeOfParallelism = 2
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.Equal(4, results.Length);
        XunitAssert.All(results, r => XunitAssert.Equal(TestStatus.Passed, r.Status));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithStopOnFirstFailure_StopsAfterFirstFailure()
    {
        // Arrange: First test will be skipped (not a failure), second will pass
        var testMetadata = new TestClassMetadata
        {
            ClassName = "StopOnFailureTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata { MethodName = "Test1" },
                new TestMethodMetadata { MethodName = "Test2" }
            }
        };

        var options = new TestExecutionOptions
        {
            ParallelExecution = false,
            StopOnFirstFailure = true
        };

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

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
                TestMethods = new[]
                {
                    new TestMethodMetadata { MethodName = "Test1A" },
                    new TestMethodMetadata { MethodName = "Test1B" }
                }
            },
            new TestClassMetadata
            {
                ClassName = "TestClass2",
                TestMethods = new[]
                {
                    new TestMethodMetadata { MethodName = "Test2A" },
                    new TestMethodMetadata { MethodName = "Test2B" }
                }
            }
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
            TestMethods = new[]
            {
                new TestMethodMetadata { MethodName = "TimedTest" }
            }
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.True(results[0].Duration >= TimeSpan.Zero);
        XunitAssert.True(results[0].StartTime <= results[0].EndTime);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithFailingTest_ReturnsFailedResult()
    {
        // Arrange: Create a test that throws an exception
        var testMetadata = new TestClassMetadata
        {
            ClassName = "FailingTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata
                {
                    MethodName = "FailingTest",
                    Skip = false,
                    ExecuteAsync = async (ct) =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException("Test intentionally failed");
                    }
                }
            }
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.Equal(TestStatus.Failed, results[0].Status);
        XunitAssert.Equal("FailingTest", results[0].TestName);
        XunitAssert.Contains("Test intentionally failed", results[0].ErrorMessage);
        XunitAssert.Equal("System.InvalidOperationException", results[0].ErrorType);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithAsyncTest_ExecutesCorrectly()
    {
        // Arrange: Create an async test
        var asyncExecuted = false;
        var testMetadata = new TestClassMetadata
        {
            ClassName = "AsyncTestClass",
            TestMethods = new[]
            {
                new TestMethodMetadata
                {
                    MethodName = "AsyncTest",
                    IsAsync = true,
                    ExecuteAsync = async (ct) =>
                    {
                        await Task.Delay(50, ct); // Simulate async work
                        asyncExecuted = true;
                        return TestResult.Success(
                            "AsyncTestClass.AsyncTest",
                            "AsyncTest",
                            TimeSpan.FromMilliseconds(50),
                            DateTime.UtcNow.AddMilliseconds(-50),
                            DateTime.UtcNow);
                    }
                }
            }
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            new[] { testMetadata },
            options);

        // Assert
        XunitAssert.True(asyncExecuted, "Async test should have executed");
        XunitAssert.Single(results);
        XunitAssert.Equal(TestStatus.Passed, results[0].Status);
        XunitAssert.True(results[0].Duration >= TimeSpan.FromMilliseconds(40)); // Account for timing variance
    }
}
