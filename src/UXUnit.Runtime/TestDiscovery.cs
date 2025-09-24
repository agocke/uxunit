using System;
using System.Collections.Generic;
using System.Linq;

namespace UXUnit.Runtime;

/// <summary>
/// Discovers test class runners through explicit registration.
/// </summary>
public static class TestDiscovery
{
    private static readonly List<ITestClassRunner> _registeredRunners = new();

    /// <summary>
    /// Registers a test class runner for discovery.
    /// </summary>
    /// <param name="runner">The test runner to register.</param>
    public static void RegisterTestRunner(ITestClassRunner runner)
    {
        if (runner == null)
            throw new ArgumentNullException(nameof(runner));

        _registeredRunners.Add(runner);
    }

    /// <summary>
    /// Registers multiple test class runners for discovery.
    /// </summary>
    /// <param name="runners">The test runners to register.</param>
    public static void RegisterTestRunners(IEnumerable<ITestClassRunner> runners)
    {
        if (runners == null)
            throw new ArgumentNullException(nameof(runners));

        _registeredRunners.AddRange(runners);
    }

    /// <summary>
    /// Clears all registered test runners.
    /// </summary>
    public static void ClearRegisteredRunners()
    {
        _registeredRunners.Clear();
    }

    /// <summary>
    /// Gets all registered test class runners.
    /// </summary>
    /// <returns>Collection of registered test class runners.</returns>
    public static IEnumerable<ITestClassRunner> DiscoverTestRunners()
    {
        return _registeredRunners.ToList(); // Return a copy to prevent external modification
    }

    /// <summary>
    /// Gets all registered test class runners (for backward compatibility).
    /// </summary>
    /// <param name="runners">Test runners to use directly instead of registered ones.</param>
    /// <returns>Collection of test class runners.</returns>
    public static IEnumerable<ITestClassRunner> DiscoverTestRunners(IEnumerable<ITestClassRunner> runners)
    {
        // For explicit test runners, use them directly
        return runners?.ToList() ?? DiscoverTestRunners();
    }

    /// <summary>
    /// Gets a discovery summary for the provided test runners.
    /// </summary>
    /// <param name="runners">The test runners to analyze.</param>
    /// <returns>A summary of the test discovery results.</returns>
    public static TestDiscoverySummary GetDiscoverySummary(IEnumerable<ITestClassRunner> runners)
    {
        var runnerList = runners.ToList();
        var totalClasses = runnerList.Count;
        var totalMethods = runnerList.Sum(r => r.Metadata.TestMethods.Count);
        var totalTestCases = runnerList.Sum(r => r.Metadata.TestMethods.Sum(m =>
            m.TestCases.Count > 0 ? m.TestCases.Count : 1));

        return new TestDiscoverySummary
        {
            TotalClasses = totalClasses,
            TotalMethods = totalMethods,
            TotalTestCases = totalTestCases,
            Runners = runnerList
        };
    }
}

/// <summary>
/// Summary information about test discovery results.
/// </summary>
public class TestDiscoverySummary
{
    /// <summary>
    /// Gets or sets the total number of test classes discovered.
    /// </summary>
    public int TotalClasses { get; set; }

    /// <summary>
    /// Gets or sets the total number of test methods discovered.
    /// </summary>
    public int TotalMethods { get; set; }

    /// <summary>
    /// Gets or sets the total number of test cases discovered (including parameterized test variations).
    /// </summary>
    public int TotalTestCases { get; set; }

    /// <summary>
    /// Gets or sets the collection of discovered test runners.
    /// </summary>
    public IEnumerable<ITestClassRunner> Runners { get; set; } = Enumerable.Empty<ITestClassRunner>();
}