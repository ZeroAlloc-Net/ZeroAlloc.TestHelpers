using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroAlloc.TestHelpers;

internal static class AllocationGate
{
    public static void AssertBudget(int budgetBytes, int iterations, Action action, string label)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        ThrowIfOverBudget(budgetBytes, iterations, MeasureTotal(iterations, action), label);
    }

    public static void AssertBudgetValueTask<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        ThrowIfOverBudget(budgetBytes, iterations, MeasureTotalValueTask(iterations, action), label);
    }

    /// <summary>
    /// The bytes one call of <paramref name="action"/> allocates, averaged over
    /// <paramref name="iterations"/> calls after the same GC and warmup as the asserts: the least
    /// of three windows of <paramref name="iterations"/> calls, after
    /// <see cref="DefaultAsyncWarmup"/> warmup calls, so a one-off allocation is not counted. The
    /// average is rounded up, so a call that allocates at all never reads as zero.
    /// </summary>
    public static long MeasureBytesPerCall(int iterations, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        return PerCall(MeasureTotal(iterations, action), iterations);
    }

    /// <summary>
    /// <see cref="MeasureBytesPerCall"/> for APIs that return <see cref="ValueTask{TResult}"/>.
    /// Throws if the supplied <see cref="ValueTask{TResult}"/> did not complete synchronously.
    /// </summary>
    public static long MeasureBytesPerCallValueTask<T>(int iterations, Func<ValueTask<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        return PerCall(MeasureTotalValueTask(iterations, action), iterations);
    }

    /// <summary>
    /// Throws if <paramref name="candidate"/> allocates more than <paramref name="baseline"/>
    /// over the same number of calls, both measured in this run.
    /// </summary>
    public static void AssertNoMoreThan(int iterations, Action baseline, Action candidate, string label)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        var baselineTotal = MeasureTotal(iterations, baseline);
        var candidateTotal = MeasureTotal(iterations, candidate);
        ThrowIfOverBaseline(iterations, baselineTotal, candidateTotal, label);
    }

    /// <summary><see cref="AssertNoMoreThan"/> for APIs that return <see cref="ValueTask{TResult}"/>.</summary>
    public static void AssertNoMoreThanValueTask<T>(
        int iterations, Func<ValueTask<T>> baseline, Func<ValueTask<T>> candidate, string label)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        var baselineTotal = MeasureTotalValueTask(iterations, baseline);
        var candidateTotal = MeasureTotalValueTask(iterations, candidate);
        ThrowIfOverBaseline(iterations, baselineTotal, candidateTotal, label);
    }

    // ---------------------------------------------------------------------------------------
    // Calls that complete asynchronously (#65).
    //
    // The methods above count with GC.GetAllocatedBytesForCurrentThread, which is exact but sees
    // only the calling thread, so they insist on synchronous completion. Work that resumes on a
    // thread-pool thread after an await allocates on that thread instead. The methods below count
    // with GC.GetTotalAllocatedBytes(precise: true), which sees every thread in the process, and
    // therefore also sees what other threads allocate meanwhile: other tests running in
    // parallel, the finalizer, timers, background tiered compilation. To keep that noise out:
    //
    //   1. the measurement runs on a thread-pool thread with no SynchronizationContext, so a test
    //      framework's context does not post, and allocate for, every continuation;
    //   2. the heap is settled first, as for the synchronous gates (#62);
    //   3. a long warmup, DefaultAsyncWarmup calls by default, lets every pool thread the call
    //      touches fill its thread-local caches and lets the methods involved tier up;
    //   4. before each window, it waits until no thread has allocated for a short quiet period,
    //      so a window does not start inside another thread's burst of allocations;
    //   5. MeasuredWindows windows of `iterations` calls each are measured, and the least is kept.
    //      The counter only grows, so another thread's allocations can only add to a window,
    //      never subtract: the quietest window is the closest to what the calls alone allocate;
    //   6. the asserts allow a per-call tolerance, DefaultAsyncToleranceBytesPerCall by default,
    //      for noise that lands in every window.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The number of warmup calls the asynchronous gates make when none is given, and the number
    /// the synchronous gates always make. Thread-pool threads fill their thread-local caches one
    /// at a time, so a short warmup measures their first rents.
    /// </summary>
    public const int DefaultAsyncWarmup = 2_000;

    /// <summary>
    /// The per-call allowance the asynchronous asserts add to a budget or a baseline when none is
    /// given, for allocations by other threads that the process-wide counter cannot tell apart
    /// from the call's own. It is below the 24 B of the smallest object on a 64-bit runtime, so an
    /// allocation on every call still fails the gate.
    /// </summary>
    public const int DefaultAsyncToleranceBytesPerCall = 8;

    // The number of measured windows, for the synchronous gates too; the least is reported.
    private const int MeasuredWindows = 3;

    // Before each window: how long no thread may allocate, and how long to wait for that.
    private const int QuietPeriodMilliseconds = 10;
    private const int QuiescenceTimeoutMilliseconds = 1_000;

    /// <summary>
    /// The bytes one call of <paramref name="action"/> allocates on any thread, for calls that may
    /// complete asynchronously. Every call is awaited. Counts process-wide, so it is an upper
    /// bound that can include other threads' allocations; see the remarks.
    /// </summary>
    /// <remarks>
    /// Settles the heap, makes <see cref="DefaultAsyncWarmup"/> warmup calls, then measures three
    /// windows of <paramref name="iterations"/> calls with
    /// <see cref="GC.GetTotalAllocatedBytes(bool)"/> and returns the least, averaged per call and
    /// rounded up. Before each window it waits, for up to a second, until no thread has allocated
    /// for 10 ms. Runs on a thread-pool thread, so the caller's SynchronizationContext is not
    /// used. Run it where no other test runs concurrently.
    /// </remarks>
    public static Task<long> MeasureBytesPerCallAsync(int iterations, Func<ValueTask> action) =>
        MeasureBytesPerCallAsync(iterations, DefaultAsyncWarmup, action);

    /// <summary>
    /// <see cref="MeasureBytesPerCallAsync(int, Func{ValueTask})"/> with
    /// <paramref name="warmup"/> warmup calls.
    /// </summary>
    public static Task<long> MeasureBytesPerCallAsync(int iterations, int warmup, Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);

        return PerCallAsync(() => MeasureLeastWindowAsync(iterations, warmup, action), iterations);
    }

    /// <summary>
    /// <see cref="MeasureBytesPerCallAsync(int, Func{ValueTask})"/> for APIs that return
    /// <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public static Task<long> MeasureBytesPerCallAsync<T>(int iterations, Func<ValueTask<T>> action) =>
        MeasureBytesPerCallAsync(iterations, DefaultAsyncWarmup, action);

    /// <summary>
    /// <see cref="MeasureBytesPerCallAsync(int, int, Func{ValueTask})"/> for APIs that return
    /// <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public static Task<long> MeasureBytesPerCallAsync<T>(int iterations, int warmup, Func<ValueTask<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);

        return PerCallAsync(() => MeasureLeastWindowAsync(iterations, warmup, action), iterations);
    }

    /// <summary>
    /// Throws if one call of <paramref name="action"/> allocates more than
    /// <paramref name="budgetBytes"/> plus <see cref="DefaultAsyncToleranceBytesPerCall"/>, on any
    /// thread, measured as <see cref="MeasureBytesPerCallAsync(int, Func{ValueTask})"/> does.
    /// </summary>
    public static Task AssertBudgetAsync(int budgetBytes, int iterations, Func<ValueTask> action, string label) =>
        AssertBudgetAsync(budgetBytes, DefaultAsyncToleranceBytesPerCall, iterations, DefaultAsyncWarmup, action, label);

    /// <summary>
    /// <see cref="AssertBudgetAsync(int, int, Func{ValueTask}, string)"/> with an explicit
    /// per-call tolerance and warmup.
    /// </summary>
    public static Task AssertBudgetAsync(
        int budgetBytes, int toleranceBytesPerCall, int iterations, int warmup, Func<ValueTask> action, string label)
    {
        ArgumentNullException.ThrowIfNull(action);
        ValidateAsyncGate(toleranceBytesPerCall, iterations, warmup);

        return AssertBudgetCoreAsync(
            budgetBytes, toleranceBytesPerCall, iterations,
            () => MeasureLeastWindowAsync(iterations, warmup, action), label);
    }

    /// <summary>
    /// <see cref="AssertBudgetAsync(int, int, Func{ValueTask}, string)"/> for APIs that return
    /// <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public static Task AssertBudgetAsync<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label) =>
        AssertBudgetAsync(budgetBytes, DefaultAsyncToleranceBytesPerCall, iterations, DefaultAsyncWarmup, action, label);

    /// <summary>
    /// <see cref="AssertBudgetAsync(int, int, int, int, Func{ValueTask}, string)"/> for APIs that
    /// return <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public static Task AssertBudgetAsync<T>(
        int budgetBytes, int toleranceBytesPerCall, int iterations, int warmup, Func<ValueTask<T>> action, string label)
    {
        ArgumentNullException.ThrowIfNull(action);
        ValidateAsyncGate(toleranceBytesPerCall, iterations, warmup);

        return AssertBudgetCoreAsync(
            budgetBytes, toleranceBytesPerCall, iterations,
            () => MeasureLeastWindowAsync(iterations, warmup, action), label);
    }

    /// <summary>
    /// Throws if <paramref name="candidate"/> allocates more per call than
    /// <paramref name="baseline"/> plus <see cref="DefaultAsyncToleranceBytesPerCall"/>, on any
    /// thread, both measured in this run as
    /// <see cref="MeasureBytesPerCallAsync(int, Func{ValueTask})"/> does.
    /// </summary>
    public static Task AssertNoMoreThanAsync(
        int iterations, Func<ValueTask> baseline, Func<ValueTask> candidate, string label) =>
        AssertNoMoreThanAsync(iterations, DefaultAsyncToleranceBytesPerCall, DefaultAsyncWarmup, baseline, candidate, label);

    /// <summary>
    /// <see cref="AssertNoMoreThanAsync(int, Func{ValueTask}, Func{ValueTask}, string)"/> with an
    /// explicit per-call tolerance and warmup.
    /// </summary>
    public static Task AssertNoMoreThanAsync(
        int iterations, int toleranceBytesPerCall, int warmup,
        Func<ValueTask> baseline, Func<ValueTask> candidate, string label)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateAsyncGate(toleranceBytesPerCall, iterations, warmup);

        return AssertNoMoreThanCoreAsync(
            iterations, toleranceBytesPerCall,
            () => MeasureLeastWindowAsync(iterations, warmup, baseline),
            () => MeasureLeastWindowAsync(iterations, warmup, candidate),
            label);
    }

    /// <summary>
    /// <see cref="AssertNoMoreThanAsync(int, Func{ValueTask}, Func{ValueTask}, string)"/> for APIs
    /// that return <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public static Task AssertNoMoreThanAsync<T>(
        int iterations, Func<ValueTask<T>> baseline, Func<ValueTask<T>> candidate, string label) =>
        AssertNoMoreThanAsync(iterations, DefaultAsyncToleranceBytesPerCall, DefaultAsyncWarmup, baseline, candidate, label);

    /// <summary>
    /// <see cref="AssertNoMoreThanAsync(int, int, int, Func{ValueTask}, Func{ValueTask}, string)"/>
    /// for APIs that return <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public static Task AssertNoMoreThanAsync<T>(
        int iterations, int toleranceBytesPerCall, int warmup,
        Func<ValueTask<T>> baseline, Func<ValueTask<T>> candidate, string label)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateAsyncGate(toleranceBytesPerCall, iterations, warmup);

        return AssertNoMoreThanCoreAsync(
            iterations, toleranceBytesPerCall,
            () => MeasureLeastWindowAsync(iterations, warmup, baseline),
            () => MeasureLeastWindowAsync(iterations, warmup, candidate),
            label);
    }

    private static void ValidateAsyncGate(int toleranceBytesPerCall, int iterations, int warmup)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceBytesPerCall);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);
    }

    // Each measurement runs through Task.Run: on a thread-pool thread, away from the caller's
    // SynchronizationContext.
    private static async Task<long> PerCallAsync(Func<Task<long>> measure, int iterations) =>
        PerCall(await Task.Run(measure).ConfigureAwait(false), iterations);

    private static async Task AssertBudgetCoreAsync(
        int budgetBytes, int toleranceBytesPerCall, int iterations, Func<Task<long>> measure, string label)
    {
        var allocated = await Task.Run(measure).ConfigureAwait(false);
        var totalBudget = ((long)budgetBytes + toleranceBytesPerCall) * iterations;
        if (allocated > totalBudget)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} allocated {allocated} B total over {iterations} iterations " +
                $"({FormatPerCall(allocated, iterations)} B/call avg, the least of {MeasuredWindows} windows, " +
                $"counted on every thread), budget is {budgetBytes} B/call plus a tolerance of " +
                $"{toleranceBytesPerCall} B/call ({totalBudget} B total). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }

    private static async Task AssertNoMoreThanCoreAsync(
        int iterations, int toleranceBytesPerCall,
        Func<Task<long>> measureBaseline, Func<Task<long>> measureCandidate, string label)
    {
        // One after the other, never concurrently: each would count the other's allocations.
        var baselineTotal = await Task.Run(measureBaseline).ConfigureAwait(false);
        var candidateTotal = await Task.Run(measureCandidate).ConfigureAwait(false);

        var allowed = baselineTotal + ((long)toleranceBytesPerCall * iterations);
        if (candidateTotal > allowed)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} candidate allocated {candidateTotal} B total over {iterations} iterations " +
                $"({FormatPerCall(candidateTotal, iterations)} B/call avg), baseline allocated {baselineTotal} B total " +
                $"({FormatPerCall(baselineTotal, iterations)} B/call avg), with a tolerance of {toleranceBytesPerCall} B/call. " +
                $"Each is the least of {MeasuredWindows} windows, counted on every thread. " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }

    // Waits until no thread in the process has allocated for QuietPeriodMilliseconds, so a
    // window does not start in the middle of someone else's burst, such as a test runner
    // reporting the previous test's result. Gives up after QuiescenceTimeoutMilliseconds and
    // measures anyway: a process that never goes quiet still gets a measurement, and the least of
    // the windows and the tolerance absorb what remains. Sleeping is safe here: no call of the
    // action is in flight, and this runs on a thread-pool thread, never the caller's.
    private static void WaitForQuietProcess()
    {
        var deadline = Environment.TickCount64 + QuiescenceTimeoutMilliseconds;
        var last = GC.GetTotalAllocatedBytes(precise: true);
        while (Environment.TickCount64 < deadline)
        {
            Thread.Sleep(QuietPeriodMilliseconds);
            var now = GC.GetTotalAllocatedBytes(precise: true);
            if (now == last)
                return;
            last = now;
        }
    }

    // Warmup and measurement share one async method, so its own state machine box is allocated
    // during the warmup, at the first call that yields, and not inside a measured window.
    private static async Task<long> MeasureLeastWindowAsync(int iterations, int warmup, Func<ValueTask> action)
    {
        SettleHeap();

        for (int i = 0; i < warmup; i++) await action().ConfigureAwait(false);

        var least = long.MaxValue;
        for (int window = 0; window < MeasuredWindows; window++)
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) await action().ConfigureAwait(false);
            least = Math.Min(least, GC.GetTotalAllocatedBytes(precise: true) - before);
        }
        return least;
    }

    // The same as above for ValueTask<T>. Adapting the action to Func<ValueTask> instead would
    // need a wrapper that allocates whenever the call yields.
    private static async Task<long> MeasureLeastWindowAsync<T>(int iterations, int warmup, Func<ValueTask<T>> action)
    {
        SettleHeap();

        for (int i = 0; i < warmup; i++) _ = await action().ConfigureAwait(false);

        var least = long.MaxValue;
        for (int window = 0; window < MeasuredWindows; window++)
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await action().ConfigureAwait(false);
            least = Math.Min(least, GC.GetTotalAllocatedBytes(precise: true) - before);
        }
        return least;
    }

    // The synchronous gates warm up and measure as the asynchronous ones do (#84): the same
    // DefaultAsyncWarmup warmup calls, then MeasuredWindows windows of `iterations` calls, keeping
    // the least. They need no quiescence wait, because the thread-local counter does not see what
    // other threads allocate, but the call itself can still allocate once inside a window: a type
    // or static initializer, a lazily filled cache or fixture, a pool growing on first use. The
    // counter only grows, so such a one-off can only add to the window it lands in. An allocation
    // that recurs every call, or every few calls, lands in every window and is still counted, so
    // an exact budget stays exact.
    private static long MeasureTotal(int iterations, Action action)
    {
        SettleHeap();

        // Warmup: JIT-compile and tier up, populate type-handle caches and pools, allocate
        // one-time fixtures.
        for (int i = 0; i < DefaultAsyncWarmup; i++) action();

        var least = long.MaxValue;
        for (int window = 0; window < MeasuredWindows; window++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) action();
            least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return least;
    }

    private static long MeasureTotalValueTask<T>(int iterations, Func<ValueTask<T>> action)
    {
        SettleHeap();

        for (int i = 0; i < DefaultAsyncWarmup; i++) Drain(action());

        var least = long.MaxValue;
        for (int window = 0; window < MeasuredWindows; window++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) Drain(action());
            least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return least;
    }

    private static T Drain<T>(ValueTask<T> t)
    {
        if (!t.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException(
                "AllocationGate: sync-completion-required — the supplied ValueTask did not " +
                "complete synchronously. Awaiter machinery would pollute the measurement; " +
                "the API under test must return an already-completed ValueTask. For a call that " +
                "completes asynchronously, use MeasureBytesPerCallAsync, AssertBudgetAsync or " +
                "AssertNoMoreThanAsync, which await it and count on every thread.");
        }
        return t.Result;
    }

    // Runs before the warmup, never between it and the measurement (#62). A full GC triggers the
    // pool trims that run from finalizers, such as ArrayPool.Shared's Gen2GcCallback, and under
    // high memory load they drop every pooled array. Collecting after the warmup emptied the pools
    // it had just filled, so the measurement counted their refill. Each collection's finalizers are
    // awaited, so no trim is still running on the finalizer thread once the warmup starts.
    //
    // Warmup garbage needs no collecting: the allocation counter counts what is allocated, not
    // what survives.
    private static void SettleHeap()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // Rounded up: integer division would report an occasional small allocation as 0 B/call.
    // Only the measurements return this; the asserts compare totals and never round.
    private static long PerCall(long allocated, int iterations) =>
        (allocated + iterations - 1) / iterations;

    // The exact average, for messages. A total that is not a whole number of bytes per call comes
    // from an allocation that does not happen on every call, and rounding it up would hide that:
    // 752 B over 1000 calls is 0.752 B/call, not the 1 B/call that no object is small enough to
    // be. Truncated to three decimals, so a fraction never reads as the next whole number.
    private static string FormatPerCall(long allocated, int iterations)
    {
        var perCall = Math.Truncate((decimal)allocated * 1000 / iterations) / 1000;
        return perCall == 0 && allocated > 0
            ? "<0.001"
            : perCall.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void ThrowIfOverBudget(int budgetBytes, int iterations, long allocated, string label)
    {
        var totalBudget = (long)budgetBytes * iterations;
        if (allocated > totalBudget)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} allocated {allocated} B total over {iterations} iterations " +
                $"({FormatPerCall(allocated, iterations)} B/call avg, the least of {MeasuredWindows} windows), " +
                $"budget is {budgetBytes} B/call ({totalBudget} B total). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }

    private static void ThrowIfOverBaseline(int iterations, long baselineTotal, long candidateTotal, string label)
    {
        if (candidateTotal > baselineTotal)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} candidate allocated {candidateTotal} B total over {iterations} iterations " +
                $"({FormatPerCall(candidateTotal, iterations)} B/call avg), baseline allocated {baselineTotal} B total " +
                $"({FormatPerCall(baselineTotal, iterations)} B/call avg). " +
                $"Each is the least of {MeasuredWindows} windows. " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }
}
