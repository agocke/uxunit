using NXTest;
using NXTest.Runtime;
using NXTest.Generated;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("nxtest runner v0.0.1");
        Console.WriteLine();
        Console.WriteLine();

        var allTests = TestRegistry.GetAllTests();

        var options = new TestExecutionOptions
        {
            Mode = ParallelMode.None, // Run sequentially for easier comparison with XUnit
        };

        return await TestFramework.RunAsync(args, allTests, options);
    }
}
