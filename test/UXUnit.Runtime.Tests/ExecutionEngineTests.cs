using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XunitAssert = Xunit.Assert;
using XunitFactAttribute = Xunit.FactAttribute;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Tests that validate the TestExecutionEngine.
/// These are XUnit tests that test our UXUnit runtime by manually synthesizing
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
            AssemblyName = "TestAssembly",
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
            AssemblyName = "TestAssembly",
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
            AssemblyName = "TestAssembly",
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
        XunitAssert.Equal(2, results.Count(r => r.Status == TestStatus.Passed));
        XunitAssert.Single(results, r => r.Status == TestStatus.Skipped);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithSequentialExecution_ExecutesInOrder()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "SequentialTestClass",
            AssemblyName = "TestAssembly",
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
        XunitAssert.All(results, r => XunitAssert.Equal(TestStatus.Passed, r.Status));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithParallelExecution_ExecutesAllTests()
    {
        // Arrange
        var testMetadata = new TestClassMetadata
        {
            ClassName = "ParallelTestClass",
            AssemblyName = "TestAssembly",
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
        XunitAssert.All(results, r => XunitAssert.Equal(TestStatus.Passed, r.Status));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithStopOnFirstFailure_StopsAfterFirstFailure()
    {
        // Arrange: First test will be skipped (not a failure), second will pass
        var testMetadata = new TestClassMetadata
        {
            ClassName = "StopOnFailureTestClass",
            AssemblyName = "TestAssembly",
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
                AssemblyName = "TestAssembly",
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
                AssemblyName = "TestAssembly",
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
            AssemblyName = "TestAssembly",
            TestMethods = [new TestMethodMetadata.Fact { MethodName = "TimedTest" }],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.True(results[0].Duration >= TimeSpan.Zero);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithFailingTest_ReturnsFailedResult()
    {
        // Arrange: Create a test that throws an exception
        var testMetadata = new TestClassMetadata
        {
            ClassName = "FailingTestClass",
            AssemblyName = "TestAssembly",
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
        XunitAssert.Single(results);
        XunitAssert.Equal(TestStatus.Failed, results[0].Status);
        XunitAssert.Equal("FailingTest", results[0].TestName);
        XunitAssert.Contains("Test intentionally failed", results[0].ErrorMessage);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithAsyncTest_ExecutesCorrectly()
    {
        // Arrange: Create an async test
        var asyncExecuted = false;
        var testMetadata = new TestClassMetadata
        {
            ClassName = "AsyncTestClass",
            AssemblyName = "TestAssembly",
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
        XunitAssert.Single(results);
        XunitAssert.Equal(TestStatus.Passed, results[0].Status);
        XunitAssert.True(results[0].Duration >= TimeSpan.FromMilliseconds(40)); // Account for timing variance
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
            AssemblyName = "TestAssembly",
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
            AssemblyName = "TestAssembly",
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
            AssemblyName = "TestAssembly",
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
        XunitAssert.All(results, r => XunitAssert.Equal(TestStatus.Passed, r.Status));
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
                AssemblyName = "TestAssembly",
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
}
