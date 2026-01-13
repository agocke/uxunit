using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Serde;
using StaticCs.Collections;
using Xunit;

namespace UXUnit.CompatibilityTests;

// TRX XML schema records for deserialization
[XmlRoot("TestRun", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")]
[GenerateDeserialize]
[SerdeTypeOptions(MemberFormat = MemberFormat.None)]
public partial record TrxTestRun
{
    [XmlArray("Results")]
    [XmlArrayItem("UnitTestResult")]
    public List<TrxUnitTestResult> Results { get; init; } = [];
}

[GenerateDeserialize]
public partial record TrxUnitTestResult
{
    [XmlAttribute("testName")]
    public string TestName { get; init; } = "";

    [XmlAttribute("outcome")]
    public string Outcome { get; init; } = "";

    [XmlElement("Output")]
    public TrxOutput? Output { get; init; }
}

[GenerateDeserialize]
public partial record TrxOutput
{
    [XmlElement("ErrorInfo")]
    public TrxErrorInfo? ErrorInfo { get; init; }
}

[GenerateDeserialize]
public partial record TrxErrorInfo
{
    [XmlElement("Message")]
    public string? Message { get; init; }
}

// Normalized records for comparison (without variable data like timing, paths, etc.)
public record NormalizedTestRun(EqArray<NormalizedTestResult> Results);

public record NormalizedTestResult(string TestName, string Outcome, string? ErrorMessage) : IComparable<NormalizedTestResult>
{
    public int CompareTo(NormalizedTestResult? other) => string.Compare(TestName, other?.TestName, StringComparison.Ordinal);
}

public class CompatibilityComparisonTests(ITestOutputHelper output)
{
    [Fact]
    public void CompareTrxOutputs()
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

        var xTrxPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "XUnitCompat",
            "debug",
            "TestResults",
            "xunit.trx"
        );
        var uxTrxPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "UXUnitCompat",
            "debug",
            "TestResults",
            "uxunit.trx"
        );

        // Delete existing TRX files to ensure fresh results
        if (File.Exists(xTrxPath)) File.Delete(xTrxPath);
        if (File.Exists(uxTrxPath)) File.Delete(uxTrxPath);

        var xPsi = new ProcessStartInfo
        {
            FileName = xbin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            ArgumentList = { "--no-ansi", "--report-xunit-trx", "--report-xunit-trx-filename", "xunit.trx" },
        };
        // Clear everything except DOTNET_ROOT
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        xPsi.Environment.Clear();
        if (dotnetRoot != null)
        {
            xPsi.Environment["DOTNET_ROOT"] = dotnetRoot;
        }

        var xresult = Process.Start(xPsi)!;
        xresult.WaitForExit();
        var xout = xresult.StandardOutput.ReadToEnd();
        var xerr = xresult.StandardError.ReadToEnd();

        var uPsi = new ProcessStartInfo
        {
            FileName = uxbin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            ArgumentList = { "--no-ansi", "--report-trx", "--report-trx-filename", "uxunit.trx" },
        };
        uPsi.Environment.Clear();
        if (dotnetRoot != null)
        {
            uPsi.Environment["DOTNET_ROOT"] = dotnetRoot;
        }

        var uresult = Process.Start(uPsi)!;
        uresult.WaitForExit();
        var uout = uresult.StandardOutput.ReadToEnd();
        var uerr = uresult.StandardError.ReadToEnd();

        output.WriteLine("XUnit Output:");
        output.WriteLine(xout);
        output.WriteLine("UXUnit Output:");
        output.WriteLine(uout);
        output.WriteLine("XUnit Error:");
        output.WriteLine(xerr);
        output.WriteLine("UXUnit Error:");
        output.WriteLine(uerr);

        // Read and normalize TRX files
        Assert.True(File.Exists(xTrxPath), $"XUnit TRX file not found at {xTrxPath}");
        Assert.True(File.Exists(uxTrxPath), $"UXUnit TRX file not found at {uxTrxPath}");

        var xTrx = File.ReadAllText(xTrxPath);
        var uxTrx = File.ReadAllText(uxTrxPath);

        output.WriteLine("XUnit TRX:");
        output.WriteLine(xTrx);
        output.WriteLine("UXUnit TRX:");
        output.WriteLine(uxTrx);

        var xTrxNormalized = ParseAndNormalizeTrx(xTrx);
        var uxTrxNormalized = ParseAndNormalizeTrx(uxTrx);

        output.WriteLine("XUnit Normalized:");
        output.WriteLine(FormatNormalizedRun(xTrxNormalized));
        output.WriteLine("UXUnit Normalized:");
        output.WriteLine(FormatNormalizedRun(uxTrxNormalized));

        Assert.Equal(xTrxNormalized, uxTrxNormalized);
    }

    private static NormalizedTestRun ParseAndNormalizeTrx(string trxContent)
    {
        var testRun = Serde.Xml.XmlSerializer.Deserialize<TrxTestRun>(trxContent);

        var normalizedResults = testRun.Results
            .Select(r => new NormalizedTestResult(
                r.TestName,
                r.Outcome,
                NormalizeErrorMessage(r.Output?.ErrorInfo?.Message)))
            .OrderBy(r => r.TestName)
            .ToEq();

        return new NormalizedTestRun(normalizedResults);
    }

    private static string? NormalizeErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return null;

        // Normalize whitespace
        var normalized = Regex.Replace(errorMessage, @"\s+", " ").Trim();
        return normalized;
    }

    private static string FormatNormalizedRun(NormalizedTestRun run)
    {
        var lines = new List<string>
        {
            $"Total: {run.Results.Length}",
            $"Passed: {run.Results.Count(r => r.Outcome == "Passed")}",
            $"Failed: {run.Results.Count(r => r.Outcome == "Failed")}",
            ""
        };

        foreach (var result in run.Results)
        {
            lines.Add($"[{result.Outcome}] {result.TestName}");
            if (result.ErrorMessage != null)
            {
                lines.Add($"  Error: {result.ErrorMessage}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
