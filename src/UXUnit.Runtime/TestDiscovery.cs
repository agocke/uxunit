using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UXUnit.Runtime;

/// <summary>
/// Discovers test class runners in assemblies.
/// </summary>
public static class TestDiscovery
{
    /// <summary>
    /// Discovers all test class runners in the calling assembly.
    /// </summary>
    /// <returns>Collection of discovered test class runners.</returns>
    public static IEnumerable<ITestClassRunner> DiscoverTestRunners()
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        return DiscoverTestRunners(callingAssembly);
    }

    /// <summary>
    /// Discovers all test class runners in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to search for test runners.</param>
    /// <returns>Collection of discovered test class runners.</returns>
    public static IEnumerable<ITestClassRunner> DiscoverTestRunners(Assembly assembly)
    {
        var testRunners = new List<ITestClassRunner>();

        try
        {
            // Find all types that implement ITestClassRunner
            var testRunnerTypes = assembly.GetTypes()
                .Where(type => 
                    typeof(ITestClassRunner).IsAssignableFrom(type) &&
                    !type.IsInterface &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                .ToArray();

            foreach (var runnerType in testRunnerTypes)
            {
                try
                {
                    // Create instance of the test runner
                    var runner = (ITestClassRunner)Activator.CreateInstance(runnerType)!;
                    testRunners.Add(runner);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to create test runner for type {runnerType.FullName}: {ex.Message}");
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            Console.WriteLine($"Warning: Failed to load some types from assembly {assembly.FullName}");
            foreach (var loaderException in ex.LoaderExceptions)
            {
                if (loaderException != null)
                {
                    Console.WriteLine($"  Loader exception: {loaderException.Message}");
                }
            }

            // Try to process the types that did load successfully
            var loadedTypes = ex.Types.Where(t => t != null).Cast<Type>();
            var testRunnerTypes = loadedTypes
                .Where(type => 
                    typeof(ITestClassRunner).IsAssignableFrom(type) &&
                    !type.IsInterface &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                .ToArray();

            foreach (var runnerType in testRunnerTypes)
            {
                try
                {
                    var runner = (ITestClassRunner)Activator.CreateInstance(runnerType)!;
                    testRunners.Add(runner);
                }
                catch (Exception createEx)
                {
                    Console.WriteLine($"Warning: Failed to create test runner for type {runnerType.FullName}: {createEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test discovery in assembly {assembly.FullName}: {ex.Message}");
        }

        return testRunners;
    }

    /// <summary>
    /// Discovers all test class runners in multiple assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to search for test runners.</param>
    /// <returns>Collection of discovered test class runners.</returns>
    public static IEnumerable<ITestClassRunner> DiscoverTestRunners(params Assembly[] assemblies)
    {
        return assemblies.SelectMany(DiscoverTestRunners);
    }

    /// <summary>
    /// Discovers all test class runners in the current app domain.
    /// </summary>
    /// <returns>Collection of discovered test class runners.</returns>
    public static IEnumerable<ITestClassRunner> DiscoverAllTestRunners()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !IsSystemAssembly(assembly))
            .ToArray();

        return DiscoverTestRunners(assemblies);
    }

    /// <summary>
    /// Gets summary information about discovered test runners.
    /// </summary>
    /// <param name="testRunners">The test runners to summarize.</param>
    /// <returns>Discovery summary information.</returns>
    public static TestDiscoverySummary GetDiscoverySummary(IEnumerable<ITestClassRunner> testRunners)
    {
        var runners = testRunners.ToList();
        var totalTestMethods = runners.SelectMany(r => r.Metadata.TestMethods).ToList();

        return new TestDiscoverySummary
        {
            TotalClasses = runners.Count,
            TotalMethods = totalTestMethods.Count,
            TotalTestCases = totalTestMethods.SelectMany(m => m.TestCases.Any() ? m.TestCases : new[] { new TestCaseMetadata() }).Count(),
            SkippedClasses = runners.Count(r => r.Metadata.Skip),
            SkippedMethods = totalTestMethods.Count(m => m.Skip),
            AsyncMethods = totalTestMethods.Count(m => m.IsAsync),
            ParameterizedMethods = totalTestMethods.Count(m => m.TestCases.Any())
        };
    }

    /// <summary>
    /// Filters test runners based on various criteria.
    /// </summary>
    /// <param name="testRunners">The test runners to filter.</param>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>Filtered test runners.</returns>
    public static IEnumerable<ITestClassRunner> FilterTestRunners(IEnumerable<ITestClassRunner> testRunners, TestFilter filter)
    {
        return testRunners.Where(runner => filter.Matches(runner.Metadata));
    }

    /// <summary>
    /// Determines if an assembly is a system assembly that should be skipped during discovery.
    /// </summary>
    private static bool IsSystemAssembly(Assembly assembly)
    {
        var assemblyName = assembly.FullName ?? string.Empty;

        return assemblyName.StartsWith("System.") ||
               assemblyName.StartsWith("Microsoft.") ||
               assemblyName.StartsWith("netstandard") ||
               assemblyName.StartsWith("mscorlib") ||
               assemblyName.StartsWith("WindowsBase") ||
               assemblyName.StartsWith("PresentationCore") ||
               assemblyName.StartsWith("PresentationFramework");
    }
}

/// <summary>
/// Summary information about test discovery.
/// </summary>
public sealed class TestDiscoverySummary
{
    /// <summary>
    /// Gets the total number of test classes discovered.
    /// </summary>
    public int TotalClasses { get; init; }

    /// <summary>
    /// Gets the total number of test methods discovered.
    /// </summary>
    public int TotalMethods { get; init; }

    /// <summary>
    /// Gets the total number of individual test cases (including parameterized test cases).
    /// </summary>
    public int TotalTestCases { get; init; }

    /// <summary>
    /// Gets the number of skipped test classes.
    /// </summary>
    public int SkippedClasses { get; init; }

    /// <summary>
    /// Gets the number of skipped test methods.
    /// </summary>
    public int SkippedMethods { get; init; }

    /// <summary>
    /// Gets the number of async test methods.
    /// </summary>
    public int AsyncMethods { get; init; }

    /// <summary>
    /// Gets the number of parameterized test methods.
    /// </summary>
    public int ParameterizedMethods { get; init; }
}

/// <summary>
/// Criteria for filtering test runners during discovery.
/// </summary>
public sealed class TestFilter
{
    /// <summary>
    /// Gets or sets the class name pattern to match (supports wildcards).
    /// </summary>
    public string? ClassNamePattern { get; set; }

    /// <summary>
    /// Gets or sets the method name pattern to match (supports wildcards).
    /// </summary>
    public string? MethodNamePattern { get; set; }

    /// <summary>
    /// Gets or sets the category to match.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether to include skipped tests.
    /// </summary>
    public bool IncludeSkipped { get; set; } = true;

    /// <summary>
    /// Determines if the specified test class metadata matches this filter.
    /// </summary>
    /// <param name="metadata">The test class metadata to check.</param>
    /// <returns>True if the metadata matches the filter; otherwise, false.</returns>
    public bool Matches(TestClassMetadata metadata)
    {
        // Check if class should be skipped
        if (!IncludeSkipped && metadata.Skip)
            return false;

        // Check class name pattern
        if (!string.IsNullOrEmpty(ClassNamePattern) && !IsMatch(metadata.ClassName, ClassNamePattern))
            return false;

        // Check category
        if (!string.IsNullOrEmpty(Category) && !string.Equals(metadata.Category, Category, StringComparison.OrdinalIgnoreCase))
            return false;

        // If we have a method pattern, check if any method matches
        if (!string.IsNullOrEmpty(MethodNamePattern))
        {
            return metadata.TestMethods.Any(method => IsMatch(method.MethodName, MethodNamePattern));
        }

        return true;
    }

    /// <summary>
    /// Checks if a string matches a pattern (supports * wildcard).
    /// </summary>
    private static bool IsMatch(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        if (pattern == "*")
            return true;

        // Simple wildcard matching
        if (pattern.Contains('*'))
        {
            var parts = pattern.Split('*');
            var currentIndex = 0;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                var index = input.IndexOf(part, currentIndex, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return false;

                currentIndex = index + part.Length;
            }

            return true;
        }

        return input.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}