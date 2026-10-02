using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ZeroAlloc.TestHelpers;

/// <summary>
/// Snapshot comparison for a single string, and the plumbing every snapshot shares.
///
/// It needs only the BCL, so it compiles into every consumer, as AllocationGate does, including
/// test projects without a Roslyn reference. GeneratorSnapshot, which needs Roslyn, builds its
/// generator overloads on the members here, so both write the same file names, honour the same
/// <c>ZA_SNAPSHOT_UPDATE=1</c> and leave the same received files.
///
/// The file format is byte-identical to Verify's — UTF-8 with BOM and LF endings — so snapshots
/// written by Verify, by GeneratorSnapshot.VerifyText or by this class are interchangeable.
/// </summary>
internal static class TextSnapshot
{
    /// <summary>Set to 1 to rewrite snapshots instead of failing on a mismatch.</summary>
    internal const string UpdateEnvVar = "ZA_SNAPSHOT_UPDATE";

    private const string SnapshotDirectory = "Snapshots";

    /// <summary>
    /// Why the snapshot entry points carry <see cref="RequiresUnreferencedCodeAttribute"/>: the test
    /// is found by reading attributes off the stack frames' methods, and that metadata can be
    /// trimmed away. Declared rather than suppressed, so a consumer with trim analysis on sees it at
    /// its own call site, and one that never calls a snapshot, such as a Native AOT smoke app that
    /// only gates allocations, compiles this file without a warning.
    /// </summary>
    internal const string StackWalkReason =
        "Snapshot names come from the [Fact] or [Theory] method found by walking the stack, which reads method metadata that trimming may remove.";

    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// Compares a single string against <c>{TestClass}.{TestMethod}.verified.{extension}</c>, for
    /// tests that assert text directly rather than a whole generator run.
    /// </summary>
    [RequiresUnreferencedCode(StackWalkReason)]
    public static void VerifyText(
        string content,
        string extension = "txt",
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testMethod = "")
    {
        var test = ResolveTest(testFilePath, testMethod);
        var dir = EnsureSnapshotDirectory();
        var prefix = Prefix(test.File, test.Method);
        var fileName = $"{prefix}.verified.{extension}";

        var failures = new List<string>();
        Compare(Path.Combine(dir, fileName), Normalise(content ?? string.Empty), extension, IsUpdate(), failures);
        Throw(prefix, failures);
    }

    internal static void Compare(string path, string expected, string extension, bool update, List<string> failures)
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

    /// <summary>
    /// Snapshots live in a Snapshots folder directly under the test project, in every repo —
    /// including ones whose test classes sit in subfolders, where the test file's own directory
    /// would be wrong. Resolving from the project rather than from the caller path also means
    /// deterministic builds, which rewrite source paths to /_/... , need no special handling.
    /// </summary>
    internal static string EnsureSnapshotDirectory()
    {
        var dir = Path.Combine(FindProjectDirectory(), SnapshotDirectory);
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
    [RequiresUnreferencedCode(StackWalkReason)]
    internal static (string File, string Method) ResolveTest(string callerFile, string callerMember)
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

    internal static string Prefix(string testFilePath, string testMethod)
        => $"{Path.GetFileNameWithoutExtension(testFilePath)}.{testMethod}";

    /// <summary>Snapshots are stored with LF so they compare equal whatever wrote them.</summary>
    internal static string Normalise(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    internal static bool IsUpdate()
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

    internal static void Throw(string prefix, List<string> failures)
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
