using UXUnit;
using UXUnit.Runtime;
using UXUnit.Generated;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("uxunit runner v0.0.1");
        Console.WriteLine();
        Console.WriteLine();

        var allTests = TestRegistry.GetAllTests();

        var options = new TestExecutionOptions
        {
            MaxDegreeOfParallelism = 1, // Run sequentially for easier comparison with XUnit
            ParallelExecution = false
        };

        return await TestFramework.RunAsync(args, allTests, options);
    }
}
