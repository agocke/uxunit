using System.Diagnostics;
using System.Text;
using Xunit;

namespace UXUnit.CompatibilityTests;

public class CompatibilityComparisonTests
{
    [Fact]
    public void CompareOutputs(ITestOutputHelper output)
    {
        var xbin = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "XUnitCompat",
            "debug",
            "XUnitCompat"
        );
        var uxbin = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "UXUnitCompat",
            "debug",
            "UXUnitCompat"
        );

        var xPsi = new ProcessStartInfo
        {
            FileName = xbin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        var xresult = Process.Start(xPsi)!;
        xresult.WaitForExit();
        var xout = xresult.StandardOutput.ReadToEnd();

        var uPsi = new ProcessStartInfo
        {
            FileName = uxbin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        var uresult = Process.Start(uPsi)!;
        uresult.WaitForExit();
        var uout = uresult.StandardOutput.ReadToEnd();

        output.WriteLine("XUnit Output:");
        output.WriteLine(xout);
        output.WriteLine("UXUnit Output:");
        output.WriteLine(uout);

        // Normalize outputs for comparison
        var xoutNormalized = NormalizeOutput(xout);
        var uoutNormalized = NormalizeOutput(uout);

        Assert.Equal(xoutNormalized, uoutNormalized);
    }

    private static string NormalizeOutput(string output)
    {
        // Split into lines
        var lines = output.Split('\n', StringSplitOptions.None);
        var normalizedLines = new List<string>();
        var headerSkipped = false;
        var inFailureDetails = false;

        foreach (var line in lines)
        {
            // Skip the first header line (contains version/runner info)
            if (!headerSkipped && (line.Contains("runner", StringComparison.OrdinalIgnoreCase) ||
                                   line.Contains("xUnit.net", StringComparison.OrdinalIgnoreCase) ||
                                   line.Contains("Testing.Platform", StringComparison.OrdinalIgnoreCase)))
            {
                headerSkipped = true;
                continue;
            }

            // Skip empty lines immediately after header until we see content
            if (headerSkipped && string.IsNullOrWhiteSpace(line) && normalizedLines.Count == 0)
                continue;

            // Detect start of failure details
            if (line.StartsWith("failed ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Failed ", StringComparison.OrdinalIgnoreCase))
            {
                inFailureDetails = true;
                // Normalize: extract test method name (last part after dots, before any parentheses)
                var testInfo = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (testInfo.Length >= 2)
                {
                    var fullTestName = testInfo[1];
                    // Remove timing info like "(0ms)" if present
                    var parenIndex = fullTestName.IndexOf('(');
                    if (parenIndex > 0)
                    {
                        fullTestName = fullTestName.Substring(0, parenIndex).Trim();
                    }
                    // Extract just the method name (last part after .)
                    var lastDot = fullTestName.LastIndexOf('.');
                    var methodName = lastDot >= 0 ? fullTestName.Substring(lastDot + 1) : fullTestName;
                    normalizedLines.Add($"Failed {methodName}");
                    continue;
                }
            }

            // Skip stack trace lines (start with "at " or contain file paths)
            if (inFailureDetails && (line.TrimStart().StartsWith("at ") ||
                                      line.Contains(".cs:line") ||
                                      line.Contains("/_/") ||
                                      line.Contains("/Users/") ||
                                      line.Contains("End of stack trace")))
            {
                continue;
            }

            // End of failure details when we hit summary
            if (line.Contains("Test run summary:", StringComparison.OrdinalIgnoreCase))
            {
                inFailureDetails = false;
            }

            // Normalize the "Test run summary" line - remove path and platform info
            if (line.Contains("Test run summary:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract just the status (Passed! or Failed!)
                var statusStart = line.IndexOf("Test run summary:", StringComparison.OrdinalIgnoreCase);
                if (statusStart >= 0)
                {
                    var afterSummary = line.Substring(statusStart + "Test run summary:".Length).Trim();
                    var status = afterSummary.Split(' ')[0]; // Get "Passed!" or "Failed!"
                    normalizedLines.Add($"Test run summary: {status}");
                    continue;
                }
            }

            // Normalize duration line - replace actual duration with placeholder
            if (line.Contains("duration:", StringComparison.OrdinalIgnoreCase))
            {
                normalizedLines.Add("  duration: XXXms");
                continue;
            }

            // Normalize assertion failure lines (Expected/Actual) - remove extra indentation
            var trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith("Expected:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmedLine.Substring("Expected:".Length).Trim();
                normalizedLines.Add($"Expected: {value}");
                continue;
            }
            if (trimmedLine.StartsWith("Actual:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmedLine.Substring("Actual:".Length).Trim();
                normalizedLines.Add($"Actual: {value}");
                continue;
            }

            // Keep all other lines as-is
            normalizedLines.Add(line);
        }

        var result = string.Join('\n', normalizedLines);

        // Trim leading and trailing whitespace/newlines
        result = result.Trim();

        // Remove blank lines immediately before summary totals
        result = result.Replace("\n\n  total:", "\n  total:");

        // Remove consecutive blank lines
        while (result.Contains("\n\n\n"))
        {
            result = result.Replace("\n\n\n", "\n\n");
        }

        return result;
    }

    private record ProcessResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }
}
