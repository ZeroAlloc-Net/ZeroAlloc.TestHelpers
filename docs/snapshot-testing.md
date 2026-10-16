# Snapshot testing

## TextSnapshot

Snapshot testing for a string. It needs no Roslyn reference, so a plain test project can use it.

- `TextSnapshot.VerifyText(string content, string extension = "txt")` compares one string against `{TestClass}.{TestMethod}.verified.{extension}`.

## GeneratorSnapshot

Snapshot testing for source-generator output. The file format matches Verify.SourceGenerators, so existing `*.verified.cs` files are reused unchanged.

- `GeneratorSnapshot.Verify(GeneratorDriver driver)` and `GeneratorSnapshot.Verify(GeneratorDriverRunResult runResult)` compare every generated source against `{TestClass}.{TestMethod}#{hintName}.verified.cs`. They fail on a mismatch, on a missing snapshot, and on a snapshot for output the generator no longer emits.
- `GeneratorSnapshot.VerifyText(string content, string extension = "txt")` forwards to `TextSnapshot.VerifyText` and is kept for existing callers.

Both write the same snapshots. Snapshots live in a `Snapshots` directory beside the test project's `.csproj`. The test name comes from the `[Fact]` or `[Theory]` method on the call stack, so calls routed through a shared helper still get per-test names. On a mismatch, a `.received` file is written next to the snapshot. Run the tests with `ZA_SNAPSHOT_UPDATE=1` to accept the current output.

Finding the test reads attributes off the methods on the stack, and trimming can remove that metadata. So the snapshot methods are marked `[RequiresUnreferencedCode]`. That matters only in a project with trim analysis on, and only where it calls them. `TextSnapshot.cs` still compiles cleanly into a Native AOT app that never calls it.
