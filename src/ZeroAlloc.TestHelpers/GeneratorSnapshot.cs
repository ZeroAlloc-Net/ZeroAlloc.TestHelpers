using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.TestHelpers;

/// <summary>
/// Snapshot comparison for source-generator output, replacing Verify.SourceGenerators.
///
/// Verify 33 introduced a SponsorCheck target that fails the build unless a sponsorship licence
/// or exemption is declared. Rather than carry a commercial declaration that expires annually
/// across fifteen repositories, this reproduces the only behaviour the suites used.
///
/// Generator output has two entry points, the <c>Verify</c> overloads, and snapshots always live in
/// a <c>Snapshots</c> directory under the test project. The plumbing they share with string
/// snapshots lives in <see cref="TextSnapshot"/>, which compiles without Roslyn. Verify allowed a
/// flat layout by omitting <c>UseDirectory</c>, and the repos drifted into using both; the
/// migration normalises them rather than teaching this helper to reproduce the inconsistency.
///
/// The file format is byte-identical to Verify's — UTF-8 with BOM, LF endings, and for generator
/// output a <c>//HintName:</c> first line — so migrating a repo does not touch a single existing
/// snapshot, and those snapshots become the regression test for this code.
/// </summary>
internal static class GeneratorSnapshot
{
    /// <summary>
    /// Compares every source the driver emitted against its own snapshot, named
    /// <c>{TestClass}.{TestMethod}#{hintName}.verified.cs</c>.
    /// The caller arguments are compiler-supplied; the repos' MA0048 rule guarantees the file
    /// name matches the test class name, which is what Verify used for the prefix.
    /// </summary>
    [RequiresUnreferencedCode(TextSnapshot.StackWalkReason)]
    public static void Verify(
        GeneratorDriver driver,
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
    {
        ArgumentNullException.ThrowIfNull(driver);
        Verify(driver.GetRunResult(), testFilePath, testMethod);
    }

    /// <summary>
    /// Overload for harnesses that hand back the run result rather than the driver.
    /// ZeroAlloc.ORM's does, and converting forty call sites to re-expose the driver would be
    /// churn for no benefit.
    /// </summary>
    [RequiresUnreferencedCode(TextSnapshot.StackWalkReason)]
    public static void Verify(
        GeneratorDriverRunResult runResult,
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
    {
        var test = TextSnapshot.ResolveTest(testFilePath, testMethod);
        var dir = TextSnapshot.EnsureSnapshotDirectory();
        var prefix = TextSnapshot.Prefix(test.File, test.Method);
        var update = TextSnapshot.IsUpdate();

        var produced = new List<string>();
        var failures = new List<string>();

        // Nested loops rather than SelectMany: ZA0601 flags LINQ in a loop.
        foreach (var result in runResult.Results)
        foreach (var generated in result.GeneratedSources)
        {
            var hint = generated.HintName;
            var stem = hint.EndsWith(".cs", StringComparison.Ordinal) ? hint[..^3] : hint;
            var fileName = $"{prefix}#{stem}.verified.cs";
            produced.Add(fileName);

            var body = TextSnapshot.Normalise(generated.SourceText.ToString());
            TextSnapshot.Compare(Path.Combine(dir, fileName), $"//HintName: {hint}\n{body}", "cs", update, failures);
        }

        // A snapshot for output the generator no longer emits would otherwise pass silently —
        // a test that quietly stopped testing.
        var orphans = Directory
            .EnumerateFiles(dir, $"{prefix}#*.verified.cs")
            .Select(Path.GetFileName)
            .Where(f => !produced.Contains(f!, StringComparer.Ordinal))
            .ToList();

        if (orphans.Count > 0)
        {
            if (update)
            {
                for (var i = 0; i < orphans.Count; i++)
                    File.Delete(Path.Combine(dir, orphans[i]!));
            }
            else
            {
                failures.Add($"snapshots exist for output no longer generated: {string.Join(", ", orphans)}");
            }
        }

        TextSnapshot.Throw(prefix, failures);
    }

    /// <summary>
    /// Compares a single string against <c>{TestClass}.{TestMethod}.verified.{extension}</c>.
    /// Kept for existing callers; it forwards to <see cref="TextSnapshot.VerifyText"/>, which needs
    /// no Roslyn reference and is the one to use in new code.
    /// </summary>
    [RequiresUnreferencedCode(TextSnapshot.StackWalkReason)]
    public static void VerifyText(
        string content,
        string extension = "txt",
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
        => TextSnapshot.VerifyText(content, extension, testFilePath, testMethod);
}
