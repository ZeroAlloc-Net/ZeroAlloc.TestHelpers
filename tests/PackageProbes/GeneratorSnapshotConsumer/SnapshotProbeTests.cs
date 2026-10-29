using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace GeneratorSnapshotConsumer;

public sealed class SnapshotProbeTests
{
    [Fact]
    public void Verify_compares_generator_output_against_the_snapshot()
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new HelloGenerator());
        driver = driver.RunGenerators(CSharpCompilation.Create("Probe"));

        GeneratorSnapshot.Verify(driver);
    }

    [Fact]
    public void Verify_fails_when_the_generator_emits_nothing()
    {
        var result = Run(new SilentGenerator());

        var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(result));
        Assert.Contains("produced no sources", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_driver_overload_fails_when_the_generator_emits_nothing()
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SilentGenerator());
        driver = driver.RunGenerators(CSharpCompilation.Create("Probe"));

        var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(driver));
        Assert.Contains("produced no sources", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_fails_in_update_mode_when_the_generator_emits_nothing()
    {
        var result = Run(new SilentGenerator());
        var previous = Environment.GetEnvironmentVariable("ZA_SNAPSHOT_UPDATE");
        Environment.SetEnvironmentVariable("ZA_SNAPSHOT_UPDATE", "1");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(result));
            Assert.Contains("produced no sources", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ZA_SNAPSHOT_UPDATE", previous);
        }
    }

    [Fact]
    public void Verify_names_error_diagnostics_when_the_generator_emits_nothing()
    {
        var result = Run(new FailingGenerator());

        var ex = Assert.Throws<InvalidOperationException>(() => GeneratorSnapshot.Verify(result));
        Assert.Contains("PROBE001", ex.Message, StringComparison.Ordinal);
    }

    private static GeneratorDriverRunResult Run(IIncrementalGenerator generator)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(CSharpCompilation.Create("Probe")).GetRunResult();
    }

    // Routed through a wrapper so CallerMemberName reports the wrapper. The snapshot name must
    // still come from this test method, which GeneratorSnapshot finds by walking the stack.
    [Fact]
    public void VerifyText_names_the_snapshot_after_the_test_not_the_wrapper()
        => Check("hello from the wrapper");

    private static void Check(string content) => GeneratorSnapshot.VerifyText(content);
}
