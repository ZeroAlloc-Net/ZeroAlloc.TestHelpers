using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.TestHelpers;

/// <summary>
/// Snapshot comparison for source-generator output, replacing Verify.SourceGenerators.
///
/// Verify 33 introduced a SponsorCheck target that fails the build unless a sponsorship licence
/// or exemption is declared. Rather than carry a commercial declaration that must be renewed
/// annually across fifteen repositories, this reproduces the only two things the test suite
/// actually used: split the driver's output into one snapshot per emitted file, and compare.
///
/// The on-disk format is deliberately byte-identical to Verify's, so every existing
/// <c>.verified.cs</c> file is kept untouched and acts as the regression test for this code:
/// UTF-8 with BOM, a <c>//HintName:</c> first line, LF endings, and a
/// <c>{TestClass}.{TestMethod}#{hint}.verified.cs</c> file name.
/// </summary>
internal static class GeneratorSnapshot
{
    /// <summary>Set to 1 to rewrite the .verified.cs files instead of failing on a mismatch.</summary>
    private const string UpdateEnvVar = "ZA_SNAPSHOT_UPDATE";

    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// Compares every source the driver emitted against its snapshot.
    /// <paramref name="testFilePath"/> and <paramref name="testMethod"/> are supplied by the
    /// compiler; the repo's MA0048 rule guarantees the file name matches the test class name,
    /// which is what Verify used for the snapshot prefix.
    /// </summary>
    public static void Verify(
        GeneratorDriver driver,
        string directory = "Snapshots",
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
    {
        var runResult = driver.GetRunResult();

        var snapshotDir = Path.Combine(Path.GetDirectoryName(testFilePath)!, directory);
        Directory.CreateDirectory(snapshotDir);

        var prefix = $"{Path.GetFileNameWithoutExtension(testFilePath)}.{testMethod}";
        bool update = string.Equals(Environment.GetEnvironmentVariable(UpdateEnvVar), "1", StringComparison.Ordinal);

        var produced = new List<string>();
        var failures = new List<string>();

        // Nested loops rather than SelectMany: ZA0601 flags LINQ in a loop, and this runs per test.
        foreach (var result in runResult.Results)
        foreach (var generated in result.GeneratedSources)
        {
            var hint = generated.HintName;
            var stem = hint.EndsWith(".cs", StringComparison.Ordinal)
                ? hint[..^3]
                : hint;
            var fileName = $"{prefix}#{stem}.verified.cs";
            produced.Add(fileName);

            // Generators build text with the host's newlines; snapshots are stored with LF so the
            // files compare equal regardless of the machine that wrote them.
            var body = generated.SourceText.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
            var expectedContent = $"//HintName: {hint}\n{body}";

            var path = Path.Combine(snapshotDir, fileName);

            if (update)
            {
                File.WriteAllText(path, expectedContent, Utf8Bom);
                continue;
            }

            if (!File.Exists(path))
            {
                WriteReceived(path, expectedContent);
                failures.Add($"missing snapshot '{fileName}' (received file written alongside it)");
                continue;
            }

            var actual = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
            if (!string.Equals(actual, expectedContent, StringComparison.Ordinal))
            {
                WriteReceived(path, expectedContent);
                failures.Add($"snapshot '{fileName}' differs:{Environment.NewLine}{Diff(actual, expectedContent)}");
            }
            else
            {
                DeleteReceived(path);
            }
        }

        // A snapshot left on disk that the generator no longer emits is a silent pass otherwise —
        // exactly the class of gap this repo has been fixing all day.
        var orphans = Directory
            .EnumerateFiles(snapshotDir, $"{prefix}#*.verified.cs")
            .Select(Path.GetFileName)
            .Where(f => !produced.Contains(f!, StringComparer.Ordinal))
            .ToList();

        if (orphans.Count > 0 && !update)
        {
            failures.Add($"snapshots exist for output no longer generated: {string.Join(", ", orphans)}");
        }
        else if (orphans.Count > 0)
        {
            foreach (var orphan in orphans)
                File.Delete(Path.Combine(snapshotDir, orphan!));
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"Generator snapshot mismatch for {prefix}:{Environment.NewLine}" +
                string.Join(Environment.NewLine, failures) +
                $"{Environment.NewLine}Re-run with {UpdateEnvVar}=1 to accept the current output.");
        }
    }

    private static void WriteReceived(string verifiedPath, string content)
        => File.WriteAllText(
            verifiedPath.Replace(".verified.cs", ".received.cs", StringComparison.Ordinal),
            content,
            Utf8Bom);

    private static void DeleteReceived(string verifiedPath)
    {
        var received = verifiedPath.Replace(".verified.cs", ".received.cs", StringComparison.Ordinal);
        if (File.Exists(received)) File.Delete(received);
    }

    /// <summary>First differing line with a little context — enough to see what moved.</summary>
    private static string Diff(string expected, string actual)
    {
        var e = expected.Split('\n');
        var a = actual.Split('\n');
        for (int i = 0; i < Math.Max(e.Length, a.Length); i++)
        {
            var el = i < e.Length ? e[i] : "<end of file>";
            var al = i < a.Length ? a[i] : "<end of file>";
            if (!string.Equals(el, al, StringComparison.Ordinal))
            {
                return $"  line {i + 1}:{Environment.NewLine}" +
                       $"    verified: {el}{Environment.NewLine}" +
                       $"    actual:   {al}";
            }
        }
        return "  (files differ only in trailing content)";
    }
}
