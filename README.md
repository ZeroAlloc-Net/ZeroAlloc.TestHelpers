# ZeroAlloc.TestHelpers

Source-distributed test helpers for the ZeroAlloc.* ecosystem.

## Usage

```xml
<PackageReference Include="ZeroAlloc.TestHelpers" Version="1.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>contentfiles;build</IncludeAssets>
</PackageReference>
```

The package has no runtime DLL. It ships three source files that compile directly into your assembly as `internal static` classes in the `ZeroAlloc.TestHelpers` namespace:

| File | Needs | Compiled into your project |
| --- | --- | --- |
| `AllocationGate.cs` | the BCL only | always |
| `TextSnapshot.cs` | the BCL only | always |
| `GeneratorSnapshot.cs` | `Microsoft.CodeAnalysis` | only when your project references Roslyn |

Keep `build` in `IncludeAssets`. The package's `build/ZeroAlloc.TestHelpers.targets` decides whether `GeneratorSnapshot.cs` is compiled. Without it, `GeneratorSnapshot.cs` is always compiled, and a project with no Roslyn reference fails with `CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft'`.

### Choosing whether GeneratorSnapshot is compiled

By default the targets file checks your resolved references. `GeneratorSnapshot.cs` is compiled only when `Microsoft.CodeAnalysis.dll` is among them, whether it comes from a direct `Microsoft.CodeAnalysis.CSharp` reference or arrives transitively. Source-generator test projects already reference Roslyn, so they need no configuration. A Native AOT smoke app or a plain unit-test project gets `AllocationGate` and `TextSnapshot` without pulling in the compiler.

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
- `AllocationGate.MeasureBytesPerCall(int iterations, Action action)` and `AllocationGate.MeasureBytesPerCallValueTask<T>(int iterations, Func<ValueTask<T>> action)` return the bytes one call allocates, after the same warmup and forced GC. The average is rounded up, so a call that allocates at all never reads as 0.
- `AllocationGate.AssertNoMoreThan(int iterations, Action baseline, Action candidate, string label)` and `AllocationGate.AssertNoMoreThanValueTask<T>(...)` measure both paths in the same run. They throw if `candidate` allocates more in total than `baseline`. Use them for relative gates, such as "a DI-resolved client allocates no more than a hand-built one".

### TextSnapshot

Snapshot testing for a string. It needs no Roslyn reference, so a plain test project can use it.

- `TextSnapshot.VerifyText(string content, string extension = "txt")` compares one string against `{TestClass}.{TestMethod}.verified.{extension}`.

### GeneratorSnapshot

Snapshot testing for source-generator output. The file format matches Verify.SourceGenerators, so existing `*.verified.cs` files are reused unchanged.

- `GeneratorSnapshot.Verify(GeneratorDriver driver)` and `GeneratorSnapshot.Verify(GeneratorDriverRunResult runResult)` compare every generated source against `{TestClass}.{TestMethod}#{hintName}.verified.cs`. They fail on a mismatch, on a missing snapshot, and on a snapshot for output the generator no longer emits.
- `GeneratorSnapshot.VerifyText(string content, string extension = "txt")` forwards to `TextSnapshot.VerifyText` and is kept for existing callers.

Both write the same snapshots. Snapshots live in a `Snapshots` directory beside the test project's `.csproj`. The test name comes from the `[Fact]` or `[Theory]` method on the call stack, so calls routed through a shared helper still get per-test names. On a mismatch, a `.received` file is written next to the snapshot. Run the tests with `ZA_SNAPSHOT_UPDATE=1` to accept the current output.

Finding the test reads attributes off the methods on the stack, and trimming can remove that metadata. So the snapshot methods are marked `[RequiresUnreferencedCode]`. That matters only in a project with trim analysis on, and only where it calls them. `TextSnapshot.cs` still compiles cleanly into a Native AOT app that never calls it.

## Why source-only

Test infrastructure should never appear on a consumer package's public API surface. By compiling into the consumer's assembly as `internal`, the helpers are scoped to each consumer and cost nothing at the consumer's runtime. `GeneratorSnapshot` also depends on this: `[CallerFilePath]` has to resolve to the consumer's test file.

## Package probes

`tests/PackageProbes/run.sh` packs the package and builds three consumer projects against the nupkg:

- an app with no Roslyn reference that uses only `AllocationGate`;
- a test project with no Roslyn reference that runs `TextSnapshot` tests;
- a test project that references Roslyn and runs `GeneratorSnapshot` tests.

CI runs it with `--aot`, which also publishes the first project with Native AOT.
