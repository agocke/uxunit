using System;
using System.Threading;
using System.Threading.Tasks;
using UXUnit.Runtime;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Another manual test class runner to demonstrate multiple test classes.
/// </summary>
public class SecondManualTestClassRunner : TestClassRunnerBase
{
    private static readonly TestClassMetadata _metadata = new()
    {
        ClassName = "SecondManualTestClass",
        DisplayName = "Second Manual Test Class",
        TestMethods = new[]
        {
            new TestMethodMetadata
            {
                MethodName = "QuickTest",
                DisplayName = "A quick test",
                IsAsync = false,
                TimeoutMs = 0,
            },
            new TestMethodMetadata
            {
                MethodName = "SlowAsyncTest",
                DisplayName = "A slower async test",
                IsAsync = true,
                TimeoutMs = 0,
            },
            new TestMethodMetadata
            {
                MethodName = "MathTest",
                DisplayName = "Math operations test",
                IsAsync = false,
                TimeoutMs = 0,
                TestCases = new[]
                {
                    new TestCaseMetadata
                    {
                        Arguments = new object[] { "multiply", 3, 4, 12 },
                        DisplayName = "3 × 4 = 12",
                    },
                    new TestCaseMetadata
                    {
                        Arguments = new object[] { "divide", 10, 2, 5 },
                        DisplayName = "10 ÷ 2 = 5",
                    },
                    new TestCaseMetadata
                    {
                        Arguments = new object[] { "subtract", 8, 3, 5 },
                        DisplayName = "8 - 3 = 5",
                    },
                },
            },
        },
    };

    public override TestClassMetadata Metadata => _metadata;

    protected override object CreateTestInstance()
    {
        return new SecondManualTestClass();
    }

    protected override Func<object, Task> GetTestMethodDelegate(string methodName)
    {
        return methodName switch
        {
            "QuickTest" => (testInstance) =>
            {
                ((SecondManualTestClass)testInstance).QuickTest();
                return Task.CompletedTask;
            },
            "SlowAsyncTest" => (testInstance) =>
                ((SecondManualTestClass)testInstance).SlowAsyncTest(),
            _ => throw new InvalidOperationException($"Unknown test method: {methodName}"),
        };
    }

    protected override Func<object, object?[], Task> GetParameterizedTestMethodDelegate(
        string methodName
    )
    {
        return methodName switch
        {
            "MathTest" => (testInstance, arguments) =>
            {
                ((SecondManualTestClass)testInstance).MathTest(
                    (string)arguments[0]!,
                    (int)arguments[1]!,
                    (int)arguments[2]!,
                    (int)arguments[3]!
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
/// Second test class to demonstrate multiple classes.
/// </summary>
public class SecondManualTestClass
{
    public void QuickTest()
    {
        Console.WriteLine("QuickTest executing");

        // Simple string test
        var text = "Hello, UXUnit!";
        if (text.Length != 14)
            throw new Exception($"Expected length 14, got {text.Length}");

        Console.WriteLine("QuickTest passed");
    }

    public async Task SlowAsyncTest()
    {
        Console.WriteLine("SlowAsyncTest starting...");

        // Simulate longer async operation
        await Task.Delay(100);

        var numbers = new[] { 1, 2, 3, 4, 5 };
        var sum = 0;

        foreach (var num in numbers)
        {
            sum += num;
            await Task.Delay(10); // Simulate async processing
        }

        if (sum != 15)
            throw new Exception($"Expected sum 15, got {sum}");

        Console.WriteLine("SlowAsyncTest completed");
    }

    public void MathTest(string operation, int a, int b, int expected)
    {
        Console.WriteLine($"MathTest: {operation} {a} and {b}, expecting {expected}");

        int result = operation switch
        {
            "multiply" => a * b,
            "divide" => a / b,
            "subtract" => a - b,
            "add" => a + b,
            _ => throw new ArgumentException($"Unknown operation: {operation}"),
        };

        if (result != expected)
            throw new Exception($"Operation {operation}({a}, {b}) = {result}, expected {expected}");

        Console.WriteLine($"MathTest {operation} passed");
    }
}
