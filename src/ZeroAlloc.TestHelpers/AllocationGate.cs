using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroAlloc.TestHelpers;

internal static class AllocationGate
{
    public static void AssertBudget(int budgetBytes, int iterations, Action action, string label)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        ThrowIfOverBudget(budgetBytes, iterations, MeasureTotal(iterations, action, TotalBudget(budgetBytes, 0, iterations)), label);
    }

    public static void AssertBudgetValueTask<T>(int budgetBytes, int iterations, Func<ValueTask<T>> action, string label)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        ThrowIfOverBudget(budgetBytes, iterations, MeasureTotalValueTask(iterations, action, TotalBudget(budgetBytes, 0, iterations)), label);
    }

    /// <summary>
    /// The bytes one call of <paramref name="action"/> allocates, averaged over
    /// <paramref name="iterations"/> calls after the same GC and warmup as the asserts: after
    /// <see cref="DefaultAsyncWarmup"/> warmup calls it measures windows of
    /// <paramref name="iterations"/> calls until the measurement is stable, and returns the least
    /// window, so a one-off allocation is not counted. The average is rounded up, so a call that
    /// allocates at all never reads as zero.
    /// </summary>
    /// <remarks>
    /// Stable means two consecutive windows read the same, and tiered compilation has settled for
    /// <see cref="TieringSettleMilliseconds"/>, so the code measured is the optimised code that
    /// keeps running, not the tier-0 code the JIT compiles first. That takes a few hundred
    /// milliseconds after the call is first made. Measuring stops after
    /// <see cref="StabilityTimeoutMilliseconds"/> regardless.
    /// </remarks>
    public static long MeasureBytesPerCall(int iterations, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        return PerCall(MeasureTotal(iterations, action, NoBudget).Least, iterations);
    }

    /// <summary>
    /// <see cref="MeasureBytesPerCall"/> for APIs that return <see cref="ValueTask{TResult}"/>.
    /// Throws if the supplied <see cref="ValueTask{TResult}"/> did not complete synchronously.
    /// </summary>
    public static long MeasureBytesPerCallValueTask<T>(int iterations, Func<ValueTask<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        return PerCall(MeasureTotalValueTask(iterations, action, NoBudget).Least, iterations);
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

        var baselineTotal = MeasureTotal(iterations, baseline, NoBudget);
        var candidateTotal = MeasureTotal(iterations, candidate, baselineTotal.Least);
        ThrowIfOverBaseline(iterations, baselineTotal, candidateTotal, label);
    }

    /// <summary><see cref="AssertNoMoreThan"/> for APIs that return <see cref="ValueTask{TResult}"/>.</summary>
    public static void AssertNoMoreThanValueTask<T>(
        int iterations, Func<ValueTask<T>> baseline, Func<ValueTask<T>> candidate, string label)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        var baselineTotal = MeasureTotalValueTask(iterations, baseline, NoBudget);
        var candidateTotal = MeasureTotalValueTask(iterations, candidate, baselineTotal.Least);
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
    //      touches fill its thread-local caches;
    //   4. before each window, it waits until no thread has allocated for a short quiet period,
    //      so a window does not start inside another thread's burst of allocations;
    //   5. windows of `iterations` calls each are measured until the measurement is stable, see
    //      Stability, and the least is kept. The counter only grows, so another thread's
    //      allocations can only add to a window, never subtract: the quietest window is the
    //      closest to what the calls alone allocate. Two windows agree when they differ by no
    //      more than the tolerance;
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

    /// <summary>
    /// How long, in milliseconds, tiered compilation must have been idle before a measurement that
    /// is not yet within its budget counts as stable. Tiered compilation first runs a method as
    /// unoptimised tier-0 code, which can allocate where the optimised code does not, for example
    /// by boxing. It delays counting calls until no new method has been called for 100 ms, then
    /// compiles the optimised code on a background thread, in up to two steps with tiered PGO.
    /// The gate follows that through the runtime's tiering events, which reach it about 10 ms
    /// late; this margin covers that lag.
    /// </summary>
    public const int TieringSettleMilliseconds = 100;

    /// <summary>
    /// The longest, in milliseconds, a gate keeps measuring windows after its warmup. A
    /// measurement that is not stable by then reports the least window seen, and a failure
    /// message says that it never stabilised.
    /// </summary>
    public const int StabilityTimeoutMilliseconds = 5_000;

    // Before each window: how long no thread may allocate, and how long to wait for that.
    private const int QuietPeriodMilliseconds = 10;
    private const int QuiescenceTimeoutMilliseconds = 1_000;

    // Between windows of a synchronous gate that waits only for tiered compilation.
    private const int TieringPauseMilliseconds = 10;

    // The budget of a measurement without one: no window is ever within it.
    private const long NoBudget = -1;

    /// <summary>
    /// The bytes one call of <paramref name="action"/> allocates on any thread, for calls that may
    /// complete asynchronously. Every call is awaited. Counts process-wide, so it is an upper
    /// bound that can include other threads' allocations; see the remarks.
    /// </summary>
    /// <remarks>
    /// Settles the heap, makes <see cref="DefaultAsyncWarmup"/> warmup calls, then measures
    /// windows of <paramref name="iterations"/> calls with
    /// <see cref="GC.GetTotalAllocatedBytes(bool)"/> until two consecutive windows agree within
    /// <see cref="DefaultAsyncToleranceBytesPerCall"/> per call and tiered compilation has settled
    /// for <see cref="TieringSettleMilliseconds"/>, or <see cref="StabilityTimeoutMilliseconds"/>
    /// passes. Returns the least window, averaged per call and rounded up. Before each window it
    /// waits, for up to a second, until no thread has allocated for 10 ms. Runs on a thread-pool
    /// thread, so the caller's SynchronizationContext is not used. Run it where no other test runs
    /// concurrently.
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

        return PerCallAsync(
            () => MeasureLeastWindowAsync(iterations, warmup, NoBudget, DefaultAsyncToleranceBytesPerCall, action),
            iterations);
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

        return PerCallAsync(
            () => MeasureLeastWindowAsync(iterations, warmup, NoBudget, DefaultAsyncToleranceBytesPerCall, action),
            iterations);
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

        var totalBudget = TotalBudget(budgetBytes, toleranceBytesPerCall, iterations);
        return AssertBudgetCoreAsync(
            budgetBytes, toleranceBytesPerCall, iterations,
            () => MeasureLeastWindowAsync(iterations, warmup, totalBudget, toleranceBytesPerCall, action), label);
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

        var totalBudget = TotalBudget(budgetBytes, toleranceBytesPerCall, iterations);
        return AssertBudgetCoreAsync(
            budgetBytes, toleranceBytesPerCall, iterations,
            () => MeasureLeastWindowAsync(iterations, warmup, totalBudget, toleranceBytesPerCall, action), label);
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
            () => MeasureLeastWindowAsync(iterations, warmup, NoBudget, toleranceBytesPerCall, baseline),
            allowed => MeasureLeastWindowAsync(iterations, warmup, allowed, toleranceBytesPerCall, candidate),
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
            () => MeasureLeastWindowAsync(iterations, warmup, NoBudget, toleranceBytesPerCall, baseline),
            allowed => MeasureLeastWindowAsync(iterations, warmup, allowed, toleranceBytesPerCall, candidate),
            label);
    }

    private static void ValidateAsyncGate(int toleranceBytesPerCall, int iterations, int warmup)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceBytesPerCall);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);
    }

    private static long TotalBudget(int budgetBytes, int toleranceBytesPerCall, int iterations) =>
        ((long)budgetBytes + toleranceBytesPerCall) * iterations;

    // Each measurement runs through Task.Run: on a thread-pool thread, away from the caller's
    // SynchronizationContext.
    private static async Task<long> PerCallAsync(Func<Task<Measurement>> measure, int iterations) =>
        PerCall((await Task.Run(measure).ConfigureAwait(false)).Least, iterations);

    private static async Task AssertBudgetCoreAsync(
        int budgetBytes, int toleranceBytesPerCall, int iterations, Func<Task<Measurement>> measure, string label)
    {
        var measured = await Task.Run(measure).ConfigureAwait(false);
        var totalBudget = TotalBudget(budgetBytes, toleranceBytesPerCall, iterations);
        if (measured.Least > totalBudget)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} allocated {measured.Least} B total over {iterations} iterations " +
                $"({FormatPerCall(measured.Least, iterations)} B/call avg, {Describe(measured)}, " +
                $"counted on every thread), budget is {budgetBytes} B/call plus a tolerance of " +
                $"{toleranceBytesPerCall} B/call ({totalBudget} B total). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }

    private static async Task AssertNoMoreThanCoreAsync(
        int iterations, int toleranceBytesPerCall,
        Func<Task<Measurement>> measureBaseline, Func<long, Task<Measurement>> measureCandidate, string label)
    {
        // One after the other, never concurrently: each would count the other's allocations.
        var baseline = await Task.Run(measureBaseline).ConfigureAwait(false);
        var allowed = baseline.Least + ((long)toleranceBytesPerCall * iterations);
        var candidate = await Task.Run(() => measureCandidate(allowed)).ConfigureAwait(false);

        if (candidate.Least > allowed)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} candidate allocated {candidate.Least} B total over {iterations} iterations " +
                $"({FormatPerCall(candidate.Least, iterations)} B/call avg, {Describe(candidate)}), " +
                $"baseline allocated {baseline.Least} B total " +
                $"({FormatPerCall(baseline.Least, iterations)} B/call avg, {Describe(baseline)}), " +
                $"with a tolerance of {toleranceBytesPerCall} B/call. Both are counted on every thread. " +
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
    private static async Task<Measurement> MeasureLeastWindowAsync(
        int iterations, int warmup, long totalBudget, int toleranceBytesPerCall, Func<ValueTask> action)
    {
        SettleHeap();

        for (int i = 0; i < warmup; i++) await action().ConfigureAwait(false);

        var stability = new Stability(totalBudget, (long)toleranceBytesPerCall * iterations);
        long window;
        do
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) await action().ConfigureAwait(false);
            window = GC.GetTotalAllocatedBytes(precise: true) - before;
        }
        while (!stability.IsDone(window));
        return stability.Result;
    }

    // The same as above for ValueTask<T>. Adapting the action to Func<ValueTask> instead would
    // need a wrapper that allocates whenever the call yields.
    private static async Task<Measurement> MeasureLeastWindowAsync<T>(
        int iterations, int warmup, long totalBudget, int toleranceBytesPerCall, Func<ValueTask<T>> action)
    {
        SettleHeap();

        for (int i = 0; i < warmup; i++) _ = await action().ConfigureAwait(false);

        var stability = new Stability(totalBudget, (long)toleranceBytesPerCall * iterations);
        long window;
        do
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await action().ConfigureAwait(false);
            window = GC.GetTotalAllocatedBytes(precise: true) - before;
        }
        while (!stability.IsDone(window));
        return stability.Result;
    }

    // The synchronous gates warm up and measure as the asynchronous ones do (#84, #89): the same
    // DefaultAsyncWarmup warmup calls, then windows of `iterations` calls until the measurement
    // is stable, keeping the least. They need no quiescence wait, because the thread-local
    // counter does not see what other threads allocate, but the call itself can still allocate
    // once inside a window: a type or static initializer, a lazily filled cache or fixture, a
    // pool growing on first use. The counter only grows, so such a one-off can only add to the
    // window it lands in. An allocation that recurs every call, or every few calls, lands in every
    // window and is still counted, so an exact budget stays exact. The counter is exact, so two
    // windows agree only when they read the same to the byte.
    private static Measurement MeasureTotal(int iterations, Action action, long totalBudget)
    {
        SettleHeap();

        // Warmup: JIT-compile, populate type-handle caches and pools, allocate one-time fixtures.
        for (int i = 0; i < DefaultAsyncWarmup; i++) action();

        var stability = new Stability(totalBudget, agreementBytes: 0);
        while (true)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) action();
            if (stability.IsDone(GC.GetAllocatedBytesForCurrentThread() - before))
                return stability.Result;

            PauseWhileTiering(stability);
        }
    }

    // While the windows already agree and only tiered compilation is awaited, the next window
    // can wait: tier-up needs time, and only about 30 calls once the runtime starts counting
    // them. Pausing keeps a gate from making hundreds of thousands of calls, burning a core and
    // growing whatever state the call touches, while it waits. The asynchronous gates pause
    // already, in WaitForQuietProcess.
    private static void PauseWhileTiering(Stability stability)
    {
        if (stability.WaitingForTiering)
            Thread.Sleep(TieringPauseMilliseconds);
    }

    private static Measurement MeasureTotalValueTask<T>(int iterations, Func<ValueTask<T>> action, long totalBudget)
    {
        SettleHeap();

        for (int i = 0; i < DefaultAsyncWarmup; i++) Drain(action());

        var stability = new Stability(totalBudget, agreementBytes: 0);
        while (true)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) Drain(action());
            if (stability.IsDone(GC.GetAllocatedBytesForCurrentThread() - before))
                return stability.Result;

            PauseWhileTiering(stability);
        }
    }

    /// <summary>The least window a gate measured, how many windows it took, and whether they settled.</summary>
    private readonly record struct Measurement(long Least, int Windows, bool Stable);

    // When to stop measuring windows, shared by every gate (#89).
    //
    // Tiered compilation runs a method as tier-0 code first, which can allocate where the
    // optimised code does not: it boxes value types the optimiser keeps unboxed, and it does not
    // stack-allocate. The runtime tiers up after a delay in time, not after a number of calls,
    // and compiles the optimised code on a background thread that installs it later. A warmup of
    // any number of calls can finish before that. Tier-0 windows also agree with each other, so
    // two agreeing windows alone do not prove that the code measured is the code that keeps
    // running. Measuring stops when the last two windows agree and either
    //
    //   - the least window is already within the budget. Later windows can only lower the least,
    //     so they cannot change a pass into a failure, and a passing gate stops after two windows;
    //   - or tiered compilation has settled, see Tiering: it is not waiting to count calls, it is
    //     not compiling in the background, and neither it nor the JIT has done anything for
    //     TieringSettleMilliseconds. Without a JIT, as in a Native AOT app, the code never
    //     changes and two agreeing windows are enough.
    //
    // A measurement that does neither within StabilityTimeoutMilliseconds stops there with the
    // least window seen, marked as never stable. It always measures at least two windows.
    private sealed class Stability
    {
        private readonly long _totalBudget;
        private readonly long _agreementBytes;
        private readonly long _started;
        private long _compiledMethods;
        private long _lastCompile;
        private long _previous;
        private long _least;
        private int _windows;

        public Stability(long totalBudget, long agreementBytes)
        {
            _totalBudget = totalBudget;
            _agreementBytes = agreementBytes;
            _started = Stopwatch.GetTimestamp();
            _lastCompile = _started;
            _compiledMethods = JitInfo.GetCompiledMethodCount();
            _least = long.MaxValue;
        }

        public Measurement Result { get; private set; }

        // The last two windows agree and are over the budget, so only tiered compilation is
        // awaited.
        public bool WaitingForTiering { get; private set; }

        // Records one window and returns whether to stop. Runs between windows, never inside one.
        public bool IsDone(long window)
        {
            var now = Stopwatch.GetTimestamp();
            var compiledMethods = JitInfo.GetCompiledMethodCount();
            if (compiledMethods != _compiledMethods)
            {
                _compiledMethods = compiledMethods;
                _lastCompile = now;
            }

            var agrees = _windows > 0 && Math.Abs(window - _previous) <= _agreementBytes;
            _previous = window;
            _least = Math.Min(_least, window);
            _windows++;

            var stable = agrees && (_least <= _totalBudget || JitHasSettled(now));
            WaitingForTiering = agrees && !stable;
            if (stable || (_windows >= 2 && Stopwatch.GetElapsedTime(_started, now).TotalMilliseconds >= StabilityTimeoutMilliseconds))
            {
                Result = new Measurement(_least, _windows, stable);
                return true;
            }

            return false;
        }

        private bool JitHasSettled(long now)
        {
            if (!RuntimeFeature.IsDynamicCodeCompiled)
                return true;

            var lastActivity = Math.Max(Math.Max(_lastCompile, _started), Tiering.LastEvent);
            return Tiering.IsIdle(now) &&
                Stopwatch.GetElapsedTime(lastActivity, now).TotalMilliseconds >= TieringSettleMilliseconds;
        }
    }

    // Follows tiered compilation through the runtime's own events (#89), on the runtime event
    // source's CompilationKeyword, which carries only these:
    //
    //   281 TieredCompilationPause                call counting is delayed: a method was called
    //                                             for the first time, and the runtime waits until
    //                                             no new method has been called for 100 ms;
    //   282 TieredCompilationResume               the delay is over and call counting starts;
    //   283 TieredCompilationBackgroundJitStart   the background thread starts compiling
    //                                             optimised code for methods that were called
    //                                             often enough;
    //   284 TieredCompilationBackgroundJitStop    it has finished.
    //
    // The delay restarts whenever any thread calls a method for the first time, precompiled
    // ReadyToRun code included, which compiles nothing. So a JIT that has been quiet for a while
    // does not show that no tier-up is on its way: in a test host the delay can last several
    // hundred milliseconds, and only the pause and resume events show it.
    //
    // One listener serves the whole process; it is created at the first gate and never disposed.
    // Its state starts unknown and is treated as paused, so the first gate waits for the runtime
    // to report where it is. A process where the events never arrive, because EventSource
    // support is disabled, is treated as idle a second after the listener started. The
    // listener's own work happens on the runtime's event dispatch thread, so the synchronous gates
    // never count it, and the asynchronous ones only while tiering is active.
    private sealed class Tiering : EventListener
    {
        private const int Unknown = 0;
        private const int Paused = 1;
        private const int Counting = 2;
        private const int NoEventsTimeoutMilliseconds = 1_000;
        private const EventKeywords CompilationKeyword = (EventKeywords)0x10_0000_0000;

        private static readonly Tiering Instance = new();

        private readonly long _created = Stopwatch.GetTimestamp();
        private int _state;
        private int _backgroundJit;
        private long _lastEvent;

        // Starts the listener, ahead of the warmup, so the warmup's first calls are seen.
        public static void EnsureStarted() => _ = Instance;

        public static long LastEvent => Volatile.Read(ref Instance._lastEvent);

        public static bool IsIdle(long now)
        {
            var state = Volatile.Read(ref Instance._state);
            if (state == Unknown)
                return Volatile.Read(ref Instance._lastEvent) == 0 &&
                    Stopwatch.GetElapsedTime(Instance._created, now).TotalMilliseconds >= NoEventsTimeoutMilliseconds;

            return state == Counting && Volatile.Read(ref Instance._backgroundJit) == 0;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Also runs from the base constructor, for the sources that already exist. Field
            // initialisers run before it, and it touches no field.
            if (string.Equals(eventSource.Name, "Microsoft-Windows-DotNETRuntime", StringComparison.Ordinal))
                EnableEvents(eventSource, EventLevel.Informational, CompilationKeyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            switch (eventData.EventId)
            {
                case 281:
                    Volatile.Write(ref _state, Paused);
                    break;
                case 282:
                    Volatile.Write(ref _state, Counting);
                    break;
                case 283:
                    Volatile.Write(ref _backgroundJit, 1);
                    break;
                case 284:
                    Volatile.Write(ref _backgroundJit, 0);
                    break;
                default:
                    return;
            }

            Volatile.Write(ref _lastEvent, Stopwatch.GetTimestamp());
        }
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
        if (RuntimeFeature.IsDynamicCodeCompiled)
            Tiering.EnsureStarted();

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

    // How a figure in a message was measured, and whether it ever settled.
    private static string Describe(Measurement measured) =>
        measured.Stable
            ? $"the least of {measured.Windows} windows"
            : $"the least of {measured.Windows} windows; the measurement never stabilised within " +
              $"{StabilityTimeoutMilliseconds / 1000} s, so the windows still disagreed or tiered compilation was still active";

    private static void ThrowIfOverBudget(int budgetBytes, int iterations, Measurement measured, string label)
    {
        var totalBudget = TotalBudget(budgetBytes, 0, iterations);
        if (measured.Least > totalBudget)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} allocated {measured.Least} B total over {iterations} iterations " +
                $"({FormatPerCall(measured.Least, iterations)} B/call avg, {Describe(measured)}), " +
                $"budget is {budgetBytes} B/call ({totalBudget} B total). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }

    private static void ThrowIfOverBaseline(int iterations, Measurement baseline, Measurement candidate, string label)
    {
        if (candidate.Least > baseline.Least)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} candidate allocated {candidate.Least} B total over {iterations} iterations " +
                $"({FormatPerCall(candidate.Least, iterations)} B/call avg, {Describe(candidate)}), " +
                $"baseline allocated {baseline.Least} B total " +
                $"({FormatPerCall(baseline.Least, iterations)} B/call avg, {Describe(baseline)}). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }
}
