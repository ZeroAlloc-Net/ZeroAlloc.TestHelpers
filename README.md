# ZeroAlloc.TestHelpers

Source-distributed test helpers for the ZeroAlloc.* ecosystem.

## Usage

```xml
<PackageReference Include="ZeroAlloc.TestHelpers" Version="1.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>contentfiles;build</IncludeAssets>
</PackageReference>
```

The package has no runtime DLL. It ships two source files that compile directly into your assembly as `internal static` classes in the `ZeroAlloc.TestHelpers` namespace:

| File | Needs | Compiled into your project |
| --- | --- | --- |
| `AllocationGate.cs` | the BCL only | always |
| `GeneratorSnapshot.cs` | `Microsoft.CodeAnalysis` | only when your project references Roslyn |

Keep `build` in `IncludeAssets`. The package's `build/ZeroAlloc.TestHelpers.targets` decides whether `GeneratorSnapshot.cs` is compiled. Without it, `GeneratorSnapshot.cs` is always compiled, and a project with no Roslyn reference fails with `CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft'`.

### Choosing whether GeneratorSnapshot is compiled

By default the targets file checks your resolved references. `GeneratorSnapshot.cs` is compiled only when `Microsoft.CodeAnalysis.dll` is among them, whether it comes from a direct `Microsoft.CodeAnalysis.CSharp` reference or arrives transitively. Source-generator test projects already reference Roslyn, so they need no configuration. A Native AOT smoke app or a plain unit-test project gets `AllocationGate` alone and does not pull in the compiler.

To override the check, set `ZeroAllocTestHelpersIncludeGeneratorSnapshot`:

```xml
<PropertyGroup>
  <!-- true: always compile GeneratorSnapshot.cs. false: never compile it. Unset: detect Roslyn. -->
  <ZeroAllocTestHelpersIncludeGeneratorSnapshot>false</ZeroAllocTestHelpersIncludeGeneratorSnapshot>
</PropertyGroup>
```

## API

### AllocationGate

- `AllocationGate.AssertBudget(int budgetBytes, int iterations, Action action, string label)` runs `action` `iterations` times after a warmup and a forced GC. It throws `InvalidOperationException` if total allocations exceed `budgetBytes * iterations`.
- `AllocationGate.AssertBudgetValueTask<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label)` is the same check for APIs that return `ValueTask<T>`. It throws if the supplied `ValueTask<T>` did not complete synchronously, because awaiter machinery would pollute the measurement.

### GeneratorSnapshot

Snapshot testing for source-generator output. The file format matches Verify.SourceGenerators, so existing `*.verified.cs` files are reused unchanged.

- `GeneratorSnapshot.Verify(GeneratorDriver driver)` and `GeneratorSnapshot.Verify(GeneratorDriverRunResult runResult)` compare every generated source against `{TestClass}.{TestMethod}#{hintName}.verified.cs`. They fail on a mismatch, on a missing snapshot, and on a snapshot for output the generator no longer emits.
- `GeneratorSnapshot.VerifyText(string content, string extension = "txt")` compares one string against `{TestClass}.{TestMethod}.verified.{extension}`.

Snapshots live in a `Snapshots` directory beside the test project's `.csproj`. The test name comes from the `[Fact]` or `[Theory]` method on the call stack, so calls routed through a shared helper still get per-test names. On a mismatch, a `.received` file is written next to the snapshot. Run the tests with `ZA_SNAPSHOT_UPDATE=1` to accept the current output.

## Why source-only

Test infrastructure should never appear on a consumer package's public API surface. By compiling into the consumer's assembly as `internal`, the helpers are scoped to each consumer and cost nothing at the consumer's runtime. `GeneratorSnapshot` also depends on this: `[CallerFilePath]` has to resolve to the consumer's test file.

## Package probes

`tests/PackageProbes/run.sh` packs the package and builds two consumer projects against the nupkg. One has no Roslyn reference and uses only `AllocationGate`. The other references Roslyn and runs `GeneratorSnapshot` tests. CI runs it with `--aot`, which also publishes the first project with Native AOT.
