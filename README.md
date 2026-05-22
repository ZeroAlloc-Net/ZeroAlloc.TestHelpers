# ZeroAlloc.TestHelpers

Source-distributed test helpers for the ZeroAlloc.* ecosystem.

## Usage

```xml
<PackageReference Include="ZeroAlloc.TestHelpers" Version="1.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>contentfiles;build</IncludeAssets>
</PackageReference>
```

The single file `AllocationGate.cs` compiles directly into your assembly under namespace `ZeroAlloc.TestHelpers` as `internal static class AllocationGate`. No runtime DLL dependency.

## API

- `AllocationGate.AssertBudget(int budgetBytes, int iterations, Action action, string label)` — runs `action` `iterations` times after a warmup + forced GC, throws `InvalidOperationException` if total allocations exceed `budgetBytes * iterations`.
- `AllocationGate.AssertBudgetValueTask<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label)` — same shape for `ValueTask<T>`-returning APIs; throws if the supplied `ValueTask<T>` did not complete synchronously (awaiter machinery would pollute the measurement).

## Why source-only

Test infrastructure should never appear on a consumer package's public API surface. By compiling into the consumer's assembly as `internal`, the helper is per-consumer-scoped and zero-cost at consumer-runtime.
