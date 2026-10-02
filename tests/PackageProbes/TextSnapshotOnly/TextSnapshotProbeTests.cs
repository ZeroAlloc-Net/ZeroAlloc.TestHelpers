using Xunit;
using ZeroAlloc.TestHelpers;

namespace TextSnapshotOnly;

public sealed class TextSnapshotProbeTests
{
    [Fact]
    public void VerifyText_compares_against_the_snapshot()
        => TextSnapshot.VerifyText("replay report\nline two\n");

    [Fact]
    public void VerifyText_takes_the_extension_for_the_file_name()
        => TextSnapshot.VerifyText("{ \"answers\": 3 }\n", "json");

    // Routed through a wrapper so CallerMemberName reports the wrapper. The snapshot name must
    // still come from this test method, which TextSnapshot finds by walking the stack.
    [Fact]
    public void VerifyText_names_the_snapshot_after_the_test_not_the_wrapper()
        => Check("hello from the wrapper");

    private static void Check(string content) => TextSnapshot.VerifyText(content);
}
