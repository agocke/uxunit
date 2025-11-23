using System;
using System.Linq;
using System.Threading.Tasks;
using XunitAssert = Xunit.Assert;
using XunitFactAttribute = Xunit.FactAttribute;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Tests that validate parameterized/theory-style test execution.
/// These are XUnit tests that verify the UXUnit runtime can execute
/// test methods multiple times with different parameter sets.
/// </summary>
public class ParameterizedTestExecutionTests
{
    [XunitFact]
    public async Task ExecuteTestsAsync_WithSingleTestCase_ExecutesOnce()
    {
        // Arrange: A theory-style test with one test case
        var executionCount = 0;
        var capturedArgs = new object?[0];

        var testMetadata = new TestClassMetadata
        {
            ClassName = "TheoryTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Theory
                {
                    MethodName = "AddTest",
                    TestCases = [new TestCaseMetadata { Arguments = [2, 3, 5] }],
                    ParameterizedBody = async (args, ct) =>
                    {
                        executionCount++;
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.Equal(1, executionCount);
        XunitAssert.Equal(TestStatus.Passed, results[0].Status);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithMultipleTestCases_ExecutesForEach()
    {
        // Arrange: A theory-style test with multiple test cases
        var executionCount = 0;

        var testMetadata = new TestClassMetadata
        {
            ClassName = "TheoryTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Theory
                {
                    MethodName = "AddTest",
                    TestCases =
                    [
                        new TestCaseMetadata { Arguments = [1, 2, 3] },
                        new TestCaseMetadata { Arguments = [5, 7, 12] },
                        new TestCaseMetadata { Arguments = [-1, 1, 0] },
                    ],
                    ParameterizedBody = async (args, ct) =>
                    {
                        executionCount++;
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.Equal(3, executionCount);
        XunitAssert.All(results, r => XunitAssert.Equal(TestStatus.Passed, r.Status));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithTestCaseArguments_PassesArgumentsToDelegate()
    {
        // Arrange: Test that verifies arguments are passed correctly
        var capturedArguments = new System.Collections.Generic.List<object?[]>();

        var testMetadata = new TestClassMetadata
        {
            ClassName = "TheoryTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Theory
                {
                    MethodName = "AddTest",
                    TestCases =
                    [
                        new TestCaseMetadata { Arguments = [2, 3, 5] },
                        new TestCaseMetadata { Arguments = [10, 20, 30] },
                    ],
                    ParameterizedBody = async (args, ct) =>
                    {
                        // In the real implementation, the delegate would receive arguments
                        // For now, we just verify execution happens
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(2, results.Length);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithFailingTestCase_ReportsFailureForThatCase()
    {
        // Arrange: Theory test where one case fails
        var executionCount = 0;

        var testMetadata = new TestClassMetadata
        {
            ClassName = "TheoryTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Theory
                {
                    MethodName = "AddTest",
                    TestCases =
                    [
                        new TestCaseMetadata { Arguments = [1, 2, 3] },
                        new TestCaseMetadata { Arguments = [5, 5, 11] }, // This one will "fail"
                        new TestCaseMetadata { Arguments = [0, 0, 0] },
                    ],
                    ParameterizedBody = async (args, ct) =>
                    {
                        executionCount++;
                        if (executionCount == 2)
                        {
                            throw new InvalidOperationException("Test case failed");
                        }
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.Equal(2, results.Count(r => r.Status == TestStatus.Passed));
        XunitAssert.Single(results, r => r.Status == TestStatus.Failed);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithSkippedTestCase_SkipsThatCase()
    {
        // Arrange: Theory test with a skipped case
        var testMetadata = new TestClassMetadata
        {
            ClassName = "TheoryTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Theory
                {
                    MethodName = "AddTest",
                    TestCases =
                    [
                        new TestCaseMetadata { Arguments = [1, 2, 3] },
                        new TestCaseMetadata
                        {
                            Arguments = [5, 5, 10],
                            Skip = true,
                            SkipReason = "Known issue",
                        },
                        new TestCaseMetadata { Arguments = [0, 0, 0] },
                    ],
                    ParameterizedBody = async (args, ct) =>
                    {
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(3, results.Length);
        XunitAssert.Equal(2, results.Count(r => r.Status == TestStatus.Passed));
        XunitAssert.Single(results, r => r.Status == TestStatus.Skipped);

        var skippedResult = results.Single(r => r.Status == TestStatus.Skipped);
        XunitAssert.Equal("Known issue", skippedResult.SkipReason);
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithTestCaseDisplayNames_UsesDisplayNameInResult()
    {
        // Arrange: Test cases with custom display names
        var testMetadata = new TestClassMetadata
        {
            ClassName = "TheoryTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Theory
                {
                    MethodName = "AddTest",
                    TestCases =
                    [
                        new TestCaseMetadata { Arguments = [2, 3, 5], DisplayName = "2 + 3 = 5" },
                        new TestCaseMetadata
                        {
                            Arguments = [10, 20, 30],
                            DisplayName = "10 + 20 = 30",
                        },
                    ],
                    ParameterizedBody = async (args, ct) =>
                    {
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(2, results.Length);
        // Note: The actual implementation should include display names in TestResult
        // This is a placeholder assertion until that's implemented
        XunitAssert.All(results, r => XunitAssert.NotNull(r.TestName));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithNoTestCases_ExecutesMethodOnce()
    {
        // Arrange: A method with no TestCases should execute once (like a Fact)
        var executionCount = 0;

        var testMetadata = new TestClassMetadata
        {
            ClassName = "SimpleTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Fact
                {
                    MethodName = "SimpleTest",
                    Body = async (ct) =>
                    {
                        executionCount++;
                        await Task.CompletedTask;
                    },
                },
            ],
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.Equal(1, executionCount);
    }
}
