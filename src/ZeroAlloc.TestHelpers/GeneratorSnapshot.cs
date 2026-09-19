using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.TestHelpers;

/// <summary>
/// Snapshot comparison for source-generator output, replacing Verify.SourceGenerators.
///
/// Verify 33 introduced a SponsorCheck target that fails the build unless a sponsorship licence
/// or exemption is declared. Rather than carry a commercial declaration that expires annually
/// across fifteen repositories, this reproduces the only behaviour the suites used.
///
/// There are exactly two entry points, and snapshots always live in a <c>Snapshots</c> directory
/// beside the test file. Verify allowed a flat layout by omitting <c>UseDirectory</c>, and the
/// repos drifted into using both; the migration normalises them rather than teaching this helper
/// to reproduce the inconsistency.
///
/// The file format is byte-identical to Verify's — UTF-8 with BOM, LF endings, and for generator
/// output a <c>//HintName:</c> first line — so migrating a repo does not touch a single existing
/// snapshot, and those snapshots become the regression test for this code.
/// </summary>
internal static class GeneratorSnapshot
{
    /// <summary>Set to 1 to rewrite snapshots instead of failing on a mismatch.</summary>
    private const string UpdateEnvVar = "ZA_SNAPSHOT_UPDATE";

    private const string SnapshotDirectory = "Snapshots";

    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// Compares every source the driver emitted against its own snapshot, named
    /// <c>{TestClass}.{TestMethod}#{hintName}.verified.cs</c>.
    /// The caller arguments are compiler-supplied; the repos' MA0048 rule guarantees the file
    /// name matches the test class name, which is what Verify used for the prefix.
    /// </summary>
    public static void Verify(
        GeneratorDriver driver,
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
    {
        ArgumentNullException.ThrowIfNull(driver);

        var test = ResolveTest(testFilePath, testMethod);
        var dir = EnsureSnapshotDirectory(test.File);
        var prefix = Prefix(test.File, test.Method);
        var update = IsUpdate();

        var produced = new List<string>();
        var failures = new List<string>();

        // Nested loops rather than SelectMany: ZA0601 flags LINQ in a loop.
        foreach (var result in driver.GetRunResult().Results)
        foreach (var generated in result.GeneratedSources)
        {
            var hint = generated.HintName;
            var stem = hint.EndsWith(".cs", StringComparison.Ordinal) ? hint[..^3] : hint;
            var fileName = $"{prefix}#{stem}.verified.cs";
            produced.Add(fileName);

            var body = Normalise(generated.SourceText.ToString());
            Compare(Path.Combine(dir, fileName), $"//HintName: {hint}\n{body}", "cs", update, failures);
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

        Throw(prefix, failures);
    }

    /// <summary>
    /// Compares a single string against <c>{TestClass}.{TestMethod}.verified.{extension}</c>, for
    /// tests that assert emitted text directly rather than a whole generator run.
    /// </summary>
    public static void VerifyText(
        string content,
        string extension = "txt",
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
    {
        var test = ResolveTest(testFilePath, testMethod);
        var dir = EnsureSnapshotDirectory(test.File);
        var prefix = Prefix(test.File, test.Method);
        var fileName = $"{prefix}.verified.{extension}";

        var failures = new List<string>();
        Compare(Path.Combine(dir, fileName), Normalise(content ?? string.Empty), extension, IsUpdate(), failures);
        Throw(prefix, failures);
    }

    private static void Compare(string path, string expected, string extension, bool update, List<string> failures)
    {
        var fileName = Path.GetFileName(path);

        if (update)
        {
            File.WriteAllText(path, expected, Utf8Bom);
            return;
        }

        if (!File.Exists(path))
        {
            WriteReceived(path, expected, extension);
            failures.Add($"missing snapshot '{fileName}' (a .received.{extension} was written alongside it)");
            return;
        }

        var actual = Normalise(File.ReadAllText(path));
        if (string.Equals(actual, expected, StringComparison.Ordinal))
        {
            DeleteReceived(path, extension);
            return;
        }

        WriteReceived(path, expected, extension);
        failures.Add($"snapshot '{fileName}' differs:{Environment.NewLine}{Diff(actual, expected)}");
    }

    private static string EnsureSnapshotDirectory(string testFilePath)
    {
        var sourceDir = Path.GetDirectoryName(testFilePath);

        // Deterministic builds rewrite source paths to /_/... , so the compile-time path does
        // not exist at runtime and creating a directory under it fails with "access to the path
        // '/_' is denied". Repos that set ContinuousIntegrationBuild hit this on CI only.
        // Fall back to locating the test project on disk; snapshots live beside its .csproj.
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
            sourceDir = FindProjectDirectory();

        var dir = Path.Combine(sourceDir, SnapshotDirectory);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Walks up from the test binaries to the directory holding the .csproj.</summary>
    private static string FindProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the test project directory from " + AppContext.BaseDirectory +
            ". Snapshot paths cannot be resolved under a deterministic build without it.");
    }

    /// <summary>
    /// Finds the [Fact]/[Theory] frame rather than trusting the caller attributes.
    ///
    /// Several repos route every test through a shared private wrapper, and
    /// CallerMemberName then reports the wrapper's name for all of them, collapsing every
    /// snapshot onto one prefix. Verify read the test name from the framework context; this
    /// walks the stack for the equivalent.
    /// </summary>
    private static (string File, string Method) ResolveTest(string callerFile, string callerMember)
    {
        var trace = new StackTrace(fNeedFileInfo: true);
        for (var i = 0; i < trace.FrameCount; i++)
        {
            var frame = trace.GetFrame(i);
            var method = frame?.GetMethod();
            if (method is null) continue;

            // foreach over the array and an indexed loop over the List below: the two are not
            // interchangeable here. This file ships as source and compiles inside every consumer,
            // so it has to satisfy the union of their analyzers -- HLQ013 wants foreach for an
            // array, HLQ012 wants indexed access for a List.
            foreach (var attribute in method.GetCustomAttributes(inherit: false))
            {
                var name = attribute.GetType().Name;
                if (!string.Equals(name, "FactAttribute", StringComparison.Ordinal)
                    && !string.Equals(name, "TheoryAttribute", StringComparison.Ordinal))
                    continue;

                var file = frame!.GetFileName();
                return (string.IsNullOrEmpty(file) ? callerFile : file, method.Name);
            }
        }

        // No test frame (a helper invoked outside a test): the caller attributes are correct.
        return (callerFile, callerMember);
    }

    private static string Prefix(string testFilePath, string testMethod)
        => $"{Path.GetFileNameWithoutExtension(testFilePath)}.{testMethod}";

    /// <summary>Snapshots are stored with LF so they compare equal whatever wrote them.</summary>
    private static string Normalise(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static bool IsUpdate()
        => string.Equals(Environment.GetEnvironmentVariable(UpdateEnvVar), "1", StringComparison.Ordinal);

    private static void WriteReceived(string verifiedPath, string content, string extension)
        => File.WriteAllText(ReceivedPath(verifiedPath, extension), content, Utf8Bom);

    private static void DeleteReceived(string verifiedPath, string extension)
    {
        var received = ReceivedPath(verifiedPath, extension);
        if (File.Exists(received)) File.Delete(received);
    }

    private static string ReceivedPath(string verifiedPath, string extension)
        => verifiedPath.Replace($".verified.{extension}", $".received.{extension}", StringComparison.Ordinal);

    private static void Throw(string prefix, List<string> failures)
    {
        if (failures.Count == 0) return;

        throw new InvalidOperationException(
            $"Snapshot mismatch for {prefix}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures) +
            $"{Environment.NewLine}Re-run with {UpdateEnvVar}=1 to accept the current output.");
    }

    /// <summary>First differing line with a little context — enough to see what moved.</summary>
    private static string Diff(string expected, string actual)
    {
        var e = expected.Split('\n');
        var a = actual.Split('\n');
        for (var i = 0; i < Math.Max(e.Length, a.Length); i++)
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
