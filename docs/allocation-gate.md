# AllocationGate

## Calls that complete synchronously

- `AllocationGate.AssertBudget(int budgetBytes, int iterations, Action action, string label)` runs `action` `iterations` times after a forced GC and then a warmup. It throws `InvalidOperationException` if total allocations exceed `budgetBytes * iterations`.
- `AllocationGate.AssertBudgetValueTask<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label)` is the same check for APIs that return `ValueTask<T>`. It throws if the supplied `ValueTask<T>` did not complete synchronously, because awaiter machinery would pollute the measurement.
- `AllocationGate.MeasureBytesPerCall(int iterations, Action action)` and `AllocationGate.MeasureBytesPerCallValueTask<T>(int iterations, Func<ValueTask<T>> action)` return the bytes one call allocates, after the same forced GC and warmup. The average is rounded up, so a call that allocates at all never reads as 0.
- The forced GC comes before the warmup, so a pool that a GC trims, such as `ArrayPool.Shared` under high memory load, is refilled by the warmup and not by the measured calls.
- The warmup is `AllocationGate.DefaultAsyncWarmup` (2000) calls, as for the asynchronous gates. Then each gate measures windows of `iterations` calls until the measurement is stable, and keeps the least window; see [How long a gate measures](#how-long-a-gate-measures). The counter only grows, so an allocation that happens once, such as a type initializer, a lazily filled cache or a pool growing on first use, adds to one window only and is not counted. An allocation that recurs, on every call or every hundredth, lands in every window and is counted to the byte, so a budget stays exact.
- A gate makes `2000 + n * iterations` calls, where `n` is the number of windows, at least 2. Do not assert on a call count: count the calls if a test needs the number.
- The JIT can stack-allocate an array or object that never leaves the method, and the gates measure the optimized code. A test that allocates on purpose and discards the result, such as `_ = new byte[16]`, or returns it from a `ValueTask<T>` the gate discards, can then measure 0 B. Store it in a field to keep it on the heap.
- The failure messages give the exact average, such as `0.752 B/call avg`, not a rounded one. A fraction means an allocation that does not happen on every call.
- `AllocationGate.AssertNoMoreThan(int iterations, Action baseline, Action candidate, string label)` and `AllocationGate.AssertNoMoreThanValueTask<T>(...)` measure both paths in the same run. They throw if `candidate` allocates more in total than `baseline`. Use them for relative gates, such as "a DI-resolved client allocates no more than a hand-built one".

These methods count with `GC.GetAllocatedBytesForCurrentThread`. The count is exact, but it only sees the calling thread, so the `ValueTask` variants require synchronous completion.

## How long a gate measures

Tiered compilation runs a method as unoptimized tier-0 code first, and tier-0 code can allocate where the optimized code does not. For example, it boxes a value-type argument that it formats through `ISpanFormattable`, 24 B per call. The runtime starts counting calls only once no new method has been called for 100 ms, and then compiles the optimized code on a background thread, in up to two steps with tiered PGO. That is a delay in time: a warmup of any number of calls can finish before it. Tier-0 windows also agree with each other, so agreeing windows alone do not show steady state.

So every gate, synchronous and asynchronous, measures windows of `iterations` calls after its warmup and stops when the last two windows agree and either:

- **the least window is within the budget.** Later windows can only lower the least, so they cannot turn a pass into a failure. A passing gate stops after two windows. For `AssertNoMoreThan`, the budget is the baseline measured in the same run.
- **or tiered compilation has settled.** The gate follows the runtime's tiering events: the call-counting delay is over, no background compile is running, and nothing has happened for `AllocationGate.TieringSettleMilliseconds` (100 ms). A measurement with no budget, `MeasureBytesPerCall` and the baseline of `AssertNoMoreThan`, and a gate that fails, always stop this way. That takes a few hundred milliseconds after the call is first made, longer while other threads keep calling new methods. While it waits, a synchronous gate pauses 10 ms between windows, so it makes tens of windows rather than hundreds of thousands of calls.

Two windows agree when they read the same to the byte for the synchronous gates, and when they differ by no more than the tolerance for the asynchronous ones. Under Native AOT there is no JIT, so two agreeing windows are enough.

If neither happens within `AllocationGate.StabilityTimeoutMilliseconds` (5000 ms) after the warmup, the gate stops and uses the least window it saw. A failure message then says `the measurement never stabilised within 5 s`. This happens when the call allocates a different amount in every window, or when the process never stops tiering up new code.

Failure messages give the number of windows, such as `the least of 59 windows`.

The gate watches tiering through an `EventListener` on the runtime's `CompilationKeyword` events, created at the first gate in the process. In a process where those events never arrive, because EventSource support is disabled, it treats tiering as settled one second after the listener started.

## Calls that complete asynchronously

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
using ZeroAlloc.TestHelpers;

[Collection("allocation gates")] // a collection with DisableParallelization = true
public sealed class ClientAllocationTests
{
    // Your client and a request for it; SendAsync returns ValueTask.
    private readonly OrderClient _client = new();
    private readonly OrderRequest _request = new();

    [Fact]
    public Task Send_allocates_nothing_when_the_server_answers_asynchronously() =>
        AllocationGate.AssertBudgetAsync(0, 1_000, () => _client.SendAsync(_request), "Send");
}
```

How a measurement runs:

1. It runs on a thread-pool thread, so a test framework's `SynchronizationContext` does not post, and allocate for, every continuation.
2. It settles the heap with a full GC, as the synchronous gates do.
3. It makes `warmup` calls, `AllocationGate.DefaultAsyncWarmup` (2000) by default. Pool threads fill their thread-local caches one at a time, so a short warmup measures their first rents.
4. It measures windows of `iterations` calls each with `GC.GetTotalAllocatedBytes(precise: true)` until the measurement is stable, as [How long a gate measures](#how-long-a-gate-measures) describes. Two windows agree when they differ by no more than the tolerance. Before each window it waits, for up to one second, until no thread has allocated for 10 ms.
5. It keeps the least window, averaged per call and rounded up. The counter only grows, so another thread's allocations can only add to a window. The quietest window is the closest to what the calls alone allocate.

How far to trust it:

- **It is an upper bound, not an exact count.** The process-wide counter cannot tell the call's allocations from anything else the process allocates during a window: other tests, the test runner reporting a result, timers, the finalizer, tiered compilation. The quiescence wait and the least window remove most of this, but noise that lands in every window is counted. While tiered compilation is active, that includes the gate's own listener for the tiering events.
- **Run asynchronous gates where nothing else runs.** Put them in an xUnit collection with `DisableParallelization = true`, or the equivalent in your framework. Tests running in parallel allocate in every window, and the gate reports their bytes as the call's.
- **The asserts allow a tolerance**, `AllocationGate.DefaultAsyncToleranceBytesPerCall` (8 B/call) by default, for noise that survives. The smallest object on a 64-bit runtime is 24 B, so a call that allocates on every call still fails. A 24 B allocation every third call or less often can pass; pass a tolerance of 0 to catch those too, and accept the occasional false failure.
- **What is counted includes everything the call causes**, on any thread: its own state machine boxes, the continuation of a yielding `async` method, and anything a background thread allocates on its behalf. A call that allocates only every so often is averaged over `iterations` calls.
- **A process that never goes quiet** still gets a measurement. Each window waits at most one second and then measures anyway.
- In this repository's tests, an allocation-free call that always completes on another thread measured 0 B/call in 120 out of 120 runs, a call whose completing thread allocates a 1 KiB array measured exactly 1048 B/call every time, and `async () => await Task.Yield()` measured 96 B/call, its state machine box.
