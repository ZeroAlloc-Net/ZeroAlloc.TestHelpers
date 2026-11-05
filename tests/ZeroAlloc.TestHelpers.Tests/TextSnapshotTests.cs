using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// The string snapshot and the plumbing that <see cref="GeneratorSnapshot"/> builds on (#108).
/// Comparisons that fail or rewrite run against a temporary directory, so the committed
/// Snapshots folder only ever holds the verified files the passing tests read.
/// </summary>
[Collection(SnapshotCollection.Name)]
public sealed class TextSnapshotTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("za-textsnapshot-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void VerifyText_PassesWhenTheSnapshotMatches()
    {
        TextSnapshot.VerifyText("line one\r\nline two\n");
    }

    [Fact]
    public void GeneratorSnapshotVerifyText_ForwardsToTextSnapshot()
    {
        GeneratorSnapshot.VerifyText("{ \"forwarded\": true }\n", "json");
    }

    [Fact]
    public void Compare_UpdateMode_WritesUtf8WithBom()
    {
        var path = Path.Combine(_dir, "Update.verified.txt");
        var failures = new List<string>();

        TextSnapshot.Compare(path, "content\n", "txt", update: true, failures);

        Assert.Empty(failures);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal("content\n", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
    }

    [Fact]
    public void Compare_MissingSnapshot_WritesReceivedAndReportsIt()
    {
        var path = Path.Combine(_dir, "Missing.verified.txt");
        var failures = new List<string>();

        TextSnapshot.Compare(path, "new\n", "txt", update: false, failures);

        var failure = Assert.Single(failures);
        Assert.Contains("missing snapshot 'Missing.verified.txt'", failure, StringComparison.Ordinal);
        Assert.Equal("new\n", File.ReadAllText(Path.Combine(_dir, "Missing.received.txt")));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Compare_Match_DeletesAStaleReceivedFile()
    {
        var path = Path.Combine(_dir, "Match.verified.txt");
        var received = Path.Combine(_dir, "Match.received.txt");
        File.WriteAllText(path, "same\r\n");
        File.WriteAllText(received, "left over from an earlier failure");
        var failures = new List<string>();

        TextSnapshot.Compare(path, "same\n", "txt", update: false, failures);

        Assert.Empty(failures);
        Assert.False(File.Exists(received));
    }

    [Fact]
    public void Compare_Mismatch_WritesReceivedAndReportsTheFirstDifferingLine()
    {
        var path = Path.Combine(_dir, "Differs.verified.txt");
        File.WriteAllText(path, "a\nb\nc\n");
        var failures = new List<string>();

        TextSnapshot.Compare(path, "a\nB\nc\n", "txt", update: false, failures);

        var failure = Assert.Single(failures);
        Assert.Contains("snapshot 'Differs.verified.txt' differs:", failure, StringComparison.Ordinal);
        Assert.Contains("line 2:", failure, StringComparison.Ordinal);
        Assert.Contains("verified: b", failure, StringComparison.Ordinal);
        Assert.Contains("actual:   B", failure, StringComparison.Ordinal);
        Assert.Equal("a\nB\nc\n", File.ReadAllText(Path.Combine(_dir, "Differs.received.txt")));
    }

    [Fact]
    public void Compare_ShorterOutput_ReportsTheEndOfFile()
    {
        var path = Path.Combine(_dir, "Shorter.verified.txt");
        File.WriteAllText(path, "a\nb");
        var failures = new List<string>();

        TextSnapshot.Compare(path, "a", "txt", update: false, failures);

        Assert.Contains("actual:   <end of file>", Assert.Single(failures), StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureSnapshotDirectory_IsTheSnapshotsFolderOfTheTestProject()
    {
        var dir = TextSnapshot.EnsureSnapshotDirectory();

        Assert.Equal("Snapshots", Path.GetFileName(dir));
        Assert.True(Directory.Exists(dir));
        Assert.NotEmpty(Directory.GetFiles(Path.GetDirectoryName(dir)!, "*.csproj"));
    }

    [Fact]
    public void ResolveTest_FindsTheTestMethodThroughAWrapper()
    {
        var test = ResolveThroughWrapper();

        Assert.Equal(nameof(ResolveTest_FindsTheTestMethodThroughAWrapper), test.Method);
        Assert.Equal(nameof(TextSnapshotTests), Path.GetFileNameWithoutExtension(test.File));
    }

    [Fact]
    public async Task ResolveTest_WithoutATestFrame_UsesTheCallerArguments()
    {
        var test = await Task.Run(() => TextSnapshot.ResolveTest("/src/Caller.cs", "Helper"));

        Assert.Equal(("/src/Caller.cs", "Helper"), test);
    }

    [Fact]
    public void Prefix_IsTheFileNameAndMethod()
    {
        Assert.Equal("MyTests.It_Works", TextSnapshot.Prefix("/src/tests/MyTests.cs", "It_Works"));
    }

    [Fact]
    public void Normalise_ConvertsCrLfToLf()
    {
        Assert.Equal("a\nb\n", TextSnapshot.Normalise("a\r\nb\n"));
    }

    [Fact]
    public void IsUpdate_IsTrueOnlyForOne()
    {
        var original = Environment.GetEnvironmentVariable(TextSnapshot.UpdateEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(TextSnapshot.UpdateEnvVar, "1");
            Assert.True(TextSnapshot.IsUpdate());

            Environment.SetEnvironmentVariable(TextSnapshot.UpdateEnvVar, "true");
            Assert.False(TextSnapshot.IsUpdate());

            Environment.SetEnvironmentVariable(TextSnapshot.UpdateEnvVar, null);
            Assert.False(TextSnapshot.IsUpdate());
        }
        finally
        {
            Environment.SetEnvironmentVariable(TextSnapshot.UpdateEnvVar, original);
        }
    }

    [Fact]
    public void Throw_WithoutFailures_DoesNothing()
    {
        TextSnapshot.Throw("Prefix", new List<string>());
    }

    [Fact]
    public void Throw_WithFailures_ListsThemAndTheUpdateSwitch()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => TextSnapshot.Throw("MyTests.It", new List<string> { "first", "second" }));

        Assert.StartsWith("Snapshot mismatch for MyTests.It:", ex.Message, StringComparison.Ordinal);
        Assert.Contains("first", ex.Message, StringComparison.Ordinal);
        Assert.Contains("second", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ZA_SNAPSHOT_UPDATE=1", ex.Message, StringComparison.Ordinal);
    }

    private static (string File, string Method) ResolveThroughWrapper()
        => TextSnapshot.ResolveTest("/elsewhere/Wrapper.cs", nameof(ResolveThroughWrapper));
}
