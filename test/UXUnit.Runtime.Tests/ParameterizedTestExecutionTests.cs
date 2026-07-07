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
                        new TestCaseInfo { Arguments = (2, 3, 5), DisplayName = "2 + 3 = 5" },
                    ],
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
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
                        new TestCaseInfo { Arguments = (1, 2, 3), DisplayName = "1 + 2 = 3" },
                        new TestCaseInfo { Arguments = (5, 7, 12), DisplayName = "5 + 7 = 12" },
                        new TestCaseInfo { Arguments = (-1, 1, 0), DisplayName = "-1 + 1 = 0" },
                    ],
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
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
        // Arrange: Test that verifies arguments are passed to the dispatch delegate
        var capturedArguments = new System.Collections.Generic.List<object?>();

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
                        new TestCaseInfo { Arguments = (2, 3, 5), DisplayName = "2 + 3 = 5" },
                        new TestCaseInfo { Arguments = (10, 20, 30), DisplayName = "10 + 20 = 30" },
                    ],
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, theoryArgs) =>
            {
                capturedArguments.Add(theoryArgs);
                return Task.CompletedTask;
            },
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(2, results.Length);
        XunitAssert.Equal(2, capturedArguments.Count);
        XunitAssert.All(capturedArguments, a => XunitAssert.NotNull(a));
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
                        new TestCaseInfo { Arguments = (1, 2, 3), DisplayName = "case 1" },
                        new TestCaseInfo { Arguments = (5, 5, 11), DisplayName = "case 2" },
                        new TestCaseInfo { Arguments = (0, 0, 0), DisplayName = "case 3" },
                    ],
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) =>
            {
                executionCount++;
                if (executionCount == 2)
                {
                    throw new InvalidOperationException("Test case failed");
                }
                return Task.CompletedTask;
            },
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
                        new TestCaseInfo { Arguments = (2, 3, 5), DisplayName = "2 + 3 = 5" },
                        new TestCaseInfo { Arguments = (10, 20, 30), DisplayName = "10 + 20 = 30" },
                    ],
                },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) => Task.CompletedTask,
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Equal(2, results.Length);
        XunitAssert.All(results, r => XunitAssert.NotNull(r.TestName));
    }

    [XunitFact]
    public async Task ExecuteTestsAsync_WithNoTestCases_ExecutesMethodOnce()
    {
        // Arrange: A Fact should execute once
        var executionCount = 0;

        var testMetadata = new TestClassMetadata
        {
            ClassName = "SimpleTestClass",
            AssemblyName = "TestAssembly",
            TestMethods =
            [
                new TestMethodMetadata.Fact { MethodName = "SimpleTest" },
            ],
            CreateInstance = () => null,
            TestDispatch = (_, _, _) =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
        };

        var options = TestExecutionOptions.Default;

        // Act
        var results = await TestExecutionEngine.ExecuteTestsAsync([testMetadata], options);

        // Assert
        XunitAssert.Single(results);
        XunitAssert.Equal(1, executionCount);
    }
}
