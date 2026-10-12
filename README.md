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

- `AllocationGate.AssertBudget(int budgetBytes, int iterations, Action action, string label)` runs `action` `iterations` times after a forced GC and then a warmup. It throws `InvalidOperationException` if total allocations exceed `budgetBytes * iterations`.
- `AllocationGate.AssertBudgetValueTask<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label)` is the same check for APIs that return `ValueTask<T>`. It throws if the supplied `ValueTask<T>` did not complete synchronously, because awaiter machinery would pollute the measurement.
- `AllocationGate.MeasureBytesPerCall(int iterations, Action action)` and `AllocationGate.MeasureBytesPerCallValueTask<T>(int iterations, Func<ValueTask<T>> action)` return the bytes one call allocates, after the same forced GC and warmup. The average is rounded up, so a call that allocates at all never reads as 0.
- The forced GC comes before the warmup, so a pool that a GC trims, such as `ArrayPool.Shared` under high memory load, is refilled by the warmup and not by the measured calls.
- `AllocationGate.AssertNoMoreThan(int iterations, Action baseline, Action candidate, string label)` and `AllocationGate.AssertNoMoreThanValueTask<T>(...)` measure both paths in the same run. They throw if `candidate` allocates more in total than `baseline`. Use them for relative gates, such as "a DI-resolved client allocates no more than a hand-built one".

These methods count with `GC.GetAllocatedBytesForCurrentThread`. The count is exact, but it only sees the calling thread, so the `ValueTask` variants require synchronous completion.

#### Calls that complete asynchronously

For a call that really yields, such as one that awaits I/O, use the `Async` methods. They await every call and count allocations on every thread, so work that resumes on a thread-pool thread is included.

| Method | Throws when |
| --- | --- |
| `Task<long> MeasureBytesPerCallAsync(int iterations, Func<ValueTask> action)` | never; returns bytes per call |
| `Task<long> MeasureBytesPerCallAsync(int iterations, int warmup, Func<ValueTask> action)` | never; returns bytes per call |
| `Task AssertBudgetAsync(int budgetBytes, int iterations, Func<ValueTask> action, string label)` | a call allocates more than `budgetBytes` + 8 B |
| `Task AssertBudgetAsync(int budgetBytes, int toleranceBytesPerCall, int iterations, int warmup, Func<ValueTask> action, string label)` | a call allocates more than `budgetBytes + toleranceBytesPerCall` |
| `Task AssertNoMoreThanAsync(int iterations, Func<ValueTask> baseline, Func<ValueTask> candidate, string label)` | `candidate` allocates more per call than `baseline` + 8 B |
| `Task AssertNoMoreThanAsync(int iterations, int toleranceBytesPerCall, int warmup, Func<ValueTask> baseline, Func<ValueTask> candidate, string label)` | `candidate` allocates more per call than `baseline + toleranceBytesPerCall` |

Each method also has an overload that takes `Func<ValueTask<T>>`. For an API that returns `Task`, pass `() => new ValueTask(client.SendAsync())`; wrapping a `Task` in a `ValueTask` allocates nothing.

```csharp
[Collection("allocation gates")] // a collection with DisableParallelization = true
public sealed class ClientAllocationTests
{
    [Fact]
    public Task Send_allocates_nothing_when_the_server_answers_asynchronously() =>
        AllocationGate.AssertBudgetAsync(0, 1_000, () => _client.SendAsync(_request), "Send");
}
```

How a measurement runs:

1. It runs on a thread-pool thread, so a test framework's `SynchronizationContext` does not post, and allocate for, every continuation.
2. It settles the heap with a full GC, as the synchronous gates do.
3. It makes `warmup` calls, `AllocationGate.DefaultAsyncWarmup` (2000) by default. Pool threads fill their thread-local caches one at a time, so a short warmup measures their first rents. The warmup also lets the methods involved tier up.
4. It measures three windows of `iterations` calls each with `GC.GetTotalAllocatedBytes(precise: true)`. Before each window it waits, for up to one second, until no thread has allocated for 10 ms.
5. It keeps the least of the three windows, averaged per call and rounded up. The counter only grows, so another thread's allocations can only add to a window. The quietest window is the closest to what the calls alone allocate.

How far to trust it:

- **It is an upper bound, not an exact count.** The process-wide counter cannot tell the call's allocations from anything else the process allocates during a window: other tests, the test runner reporting a result, timers, the finalizer, tiered compilation. The quiescence wait and the least of three windows remove most of this, but noise that lands in all three windows is counted.
- **Run asynchronous gates where nothing else runs.** Put them in an xUnit collection with `DisableParallelization = true`, or the equivalent in your framework. Tests running in parallel allocate in every window, and the gate reports their bytes as the call's.
- **The asserts allow a tolerance**, `AllocationGate.DefaultAsyncToleranceBytesPerCall` (8 B/call) by default, for noise that survives. The smallest object on a 64-bit runtime is 24 B, so a call that allocates on every call still fails. A 24 B allocation every third call or less often can pass; pass a tolerance of 0 to catch those too, and accept the occasional false failure.
- **What is counted includes everything the call causes**, on any thread: its own state machine boxes, the continuation of a yielding `async` method, and anything a background thread allocates on its behalf. A call that allocates only every so often is averaged over `iterations` calls.
- **A process that never goes quiet** still gets a measurement. Each window waits at most one second and then measures anyway.
- In this repository's tests, an allocation-free call that always completes on another thread measured 0 B/call in 120 out of 120 runs, a call whose completing thread allocates a 1 KiB array measured exactly 1048 B/call every time, and `async () => await Task.Yield()` measured 96 B/call, its state machine box.

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
