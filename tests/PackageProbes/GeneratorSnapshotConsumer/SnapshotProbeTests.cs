using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    // Routed through a wrapper so CallerMemberName reports the wrapper. The snapshot name must
    // still come from this test method, which GeneratorSnapshot finds by walking the stack.
    [Fact]
    public void VerifyText_names_the_snapshot_after_the_test_not_the_wrapper()
        => Check("hello from the wrapper");

    private static void Check(string content) => GeneratorSnapshot.VerifyText(content);
}
