using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// Generator-output snapshots (#108), run against small generators defined here. The tests read
/// committed files in Snapshots; the orphan tests add a stale file for their own prefix and
/// remove it again, and the update-mode test removes the snapshot it writes.
/// </summary>
[Collection(SnapshotCollection.Name)]
public sealed class GeneratorSnapshotTests
{
    [Fact]
    public void Verify_Driver_PassesWhenEverySourceMatches()
    {
        GeneratorSnapshot.Verify(Run(new HelloGenerator()));
    }

    [Fact]
    public void Verify_RunResult_PassesWhenEverySourceMatches()
    {
        GeneratorSnapshot.Verify(Run(new HelloGenerator()).GetRunResult());
    }

    [Fact]
    public void Verify_NullDriver_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GeneratorSnapshot.Verify((GeneratorDriver)null!));
    }

    [Fact]
    public void Verify_NoSources_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(Run(new SilentGenerator())));

        Assert.Contains("the generator produced no sources", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("error diagnostics", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_NoSources_ListsTheErrorDiagnostics()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(Run(new FailingGenerator())));

        Assert.Contains("The run reported error diagnostics:", ex.Message, StringComparison.Ordinal);
        Assert.Contains(FailingGenerator.Id, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_OrphanedSnapshot_Fails()
    {
        var orphan = WriteOrphan(nameof(Verify_OrphanedSnapshot_Fails));
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(Run(new HelloGenerator())));

            Assert.Contains("snapshots exist for output no longer generated", ex.Message, StringComparison.Ordinal);
            Assert.Contains(Path.GetFileName(orphan), ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("missing snapshot", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(orphan);
        }
    }

    [Fact]
    public void Verify_UpdateMode_WritesSourcesAndDeletesOrphans()
    {
        var test = nameof(Verify_UpdateMode_WritesSourcesAndDeletesOrphans);
        var orphan = WriteOrphan(test);
        var written = Path.Combine(TextSnapshot.EnsureSnapshotDirectory(), $"{nameof(GeneratorSnapshotTests)}.{test}#Hello.g.verified.cs");
        var original = Environment.GetEnvironmentVariable(TextSnapshot.UpdateEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(TextSnapshot.UpdateEnvVar, "1");

            GeneratorSnapshot.Verify(Run(new HelloGenerator()));

            Assert.False(File.Exists(orphan));
            Assert.StartsWith("//HintName: Hello.g.cs\n", File.ReadAllText(written), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TextSnapshot.UpdateEnvVar, original);
            File.Delete(orphan);
            File.Delete(written);
        }
    }

    private static GeneratorDriver Run(IIncrementalGenerator generator)
        => CSharpGeneratorDriver.Create(generator).RunGenerators(CSharpCompilation.Create("SnapshotTests"));

    private static string WriteOrphan(string testMethod)
    {
        var path = Path.Combine(
            TextSnapshot.EnsureSnapshotDirectory(),
            $"{nameof(GeneratorSnapshotTests)}.{testMethod}#Removed.verified.cs");
        File.WriteAllText(path, "//HintName: Removed.cs\n");
        return path;
    }

    private sealed class HelloGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
            => context.RegisterPostInitializationOutput(static output =>
                output.AddSource("Hello.g.cs", "namespace Generated;\r\n\r\ninternal static class Hello\r\n{\r\n}\r\n"));
    }

    private sealed class SilentGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
        }
    }

    private sealed class FailingGenerator : IIncrementalGenerator
    {
        public const string Id = "SNAP001";

        private static readonly DiagnosticDescriptor Failure = new(
            Id, "Generator failed", "The generator failed", "Tests", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
            => context.RegisterSourceOutput(context.CompilationProvider, static (output, _) =>
                output.ReportDiagnostic(Diagnostic.Create(Failure, Location.None)));
    }
}
