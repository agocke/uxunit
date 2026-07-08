using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NXTest.Generators.Tests;

/// <summary>
/// Minimal, self-contained snapshot verifier for generator output. Reproduces the
/// on-disk format produced by Verify.SourceGenerators (one file per generated source,
/// prefixed with a <c>//HintName:</c> line) so the existing <c>Snapshots/*.verified.cs</c>
/// files continue to work without a dependency on Verify/xUnit.
/// </summary>
internal static class GeneratorSnapshotVerifier
{
    public static Task Verify(
        GeneratorDriver driver,
        string snapshotName,
        [CallerFilePath] string sourceFile = "")
    {
        var snapshotDirectory = Path.Combine(Path.GetDirectoryName(sourceFile)!, "Snapshots");

        var runResult = driver.GetRunResult();
        var generated = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => (s.HintName, Text: NormalizeLineEndings(s.SourceText.ToString())))
            .OrderBy(s => s.HintName, StringComparer.Ordinal)
            .ToList();

        var errors = new List<string>();
        var expectedFiles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (hintName, text) in generated)
        {
            var stem = hintName.EndsWith(".cs", StringComparison.Ordinal)
                ? hintName.Substring(0, hintName.Length - 3)
                : hintName;
            var verifiedFile = Path.Combine(snapshotDirectory, $"{snapshotName}#{stem}.verified.cs");
            var receivedFile = Path.Combine(snapshotDirectory, $"{snapshotName}#{stem}.received.cs");
            expectedFiles.Add(Path.GetFileName(verifiedFile));

            var expected = $"//HintName: {hintName}\n{text}";

            if (!File.Exists(verifiedFile))
            {
                File.WriteAllText(receivedFile, WithBom(expected));
                errors.Add($"Missing snapshot '{Path.GetFileName(verifiedFile)}' (received file written).");
                continue;
            }

            var actual = NormalizeLineEndings(StripBom(File.ReadAllText(verifiedFile)));
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                File.WriteAllText(receivedFile, WithBom(expected));
                errors.Add($"Snapshot mismatch '{Path.GetFileName(verifiedFile)}' (received file written).");
            }
            else if (File.Exists(receivedFile))
            {
                File.Delete(receivedFile);
            }
        }

        // Detect stale snapshots that were not produced by this run.
        var orphans = Directory
            .EnumerateFiles(snapshotDirectory, $"{snapshotName}#*.verified.cs")
            .Select(Path.GetFileName)
            .Where(name => !expectedFiles.Contains(name!));
        foreach (var orphan in orphans)
        {
            errors.Add($"Unexpected snapshot '{orphan}' was not produced by the generator.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Generator snapshot verification failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors));
        }

        return Task.CompletedTask;
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n").Replace("\r", "\n");

    private static string StripBom(string value) =>
        value.Length > 0 && value[0] == '\uFEFF' ? value.Substring(1) : value;

    private static string WithBom(string value) => "\uFEFF" + value;
}
