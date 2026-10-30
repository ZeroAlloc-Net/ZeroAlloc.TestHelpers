using System;
using System.Collections.Generic;
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
    /// Stable means two consecutive windows read the same, and tiered compilation has settled, so
    /// the code measured is the optimised code that keeps running, not the tier-0 code the JIT
    /// compiles first. That takes about a second after the call is first made. Windows that
    /// disagree stop the measurement after <see cref="StabilityTimeoutMilliseconds"/>. Tiered
    /// compilation that has not settled after four times as long makes it throw
    /// <see cref="InvalidOperationException"/>, because the figure could be the tier-0 one.
    /// </remarks>
    public static long MeasureBytesPerCall(int iterations, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        return PerCall(ThrowIfTieringUnsettled(MeasureTotal(iterations, action, NoBudget), nameof(MeasureBytesPerCall)).Least, iterations);
    }

    /// <summary>
    /// <see cref="MeasureBytesPerCall"/> for APIs that return <see cref="ValueTask{TResult}"/>.
    /// Throws if the supplied <see cref="ValueTask{TResult}"/> did not complete synchronously.
    /// </summary>
    public static long MeasureBytesPerCallValueTask<T>(int iterations, Func<ValueTask<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        return PerCall(
            ThrowIfTieringUnsettled(MeasureTotalValueTask(iterations, action, NoBudget), nameof(MeasureBytesPerCallValueTask)).Least,
            iterations);
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
        _ = ThrowIfTieringUnsettled(baselineTotal, label + " baseline");
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
        _ = ThrowIfTieringUnsettled(baselineTotal, label + " baseline");
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
    /// How long, in milliseconds, the JIT must have compiled nothing before a measurement that is
    /// not yet within its budget counts as stable. Tiered compilation first runs a method as
    /// unoptimised tier-0 code, which can allocate where the optimised code does not, for example
    /// by boxing. It delays counting calls until no new method has been called for 100 ms, then
    /// compiles the optimised code on a background thread, in up to two steps with tiered PGO,
    /// a delay apart. The gate follows that through the runtime's tiering events, which reach it
    /// late, and also waits three times this long after the last of them.
    /// </summary>
    public const int TieringSettleMilliseconds = 100;

    /// <summary>
    /// The longest, in milliseconds, a gate keeps measuring windows after its warmup while
    /// tiered compilation is settled. A measurement whose windows do not agree by then reports the
    /// least window seen, and a failure message says that it never stabilised. While tiered
    /// compilation is not settled a gate keeps measuring for four times as long, and then throws
    /// rather than report a figure that may be the tier-0 one.
    /// </summary>
    public const int StabilityTimeoutMilliseconds = 5_000;

    // A comparison of two calls that cost the same keeps measuring both for at least this many
    // windows each, and compares the median window of each (#76). The window of a call that
    // completes on another thread varies with how the scheduler happened to run it: every so
    // often a window is a few percent lower than all the others, because some calls found their
    // work already done. That is not noise from another thread, which could only add. The least
    // window of a side that had such a dip was then several bytes per call below the other side's
    // least, and two equal calls differed by more than the tolerance about one run in thirty. The
    // median is not moved by a minority of windows, in either direction.
    private const int ComparisonWindows = 15;

    /// <summary>
    /// How many windows of each call <c>MeasureBytesPerCallInterleavedAsync</c> counts call by
    /// call, once the windows before them are stable.
    /// </summary>
    public const int ModeWindows = 5;

    // How long tiering must have shown no sign of activity before a measurement counts as settled.
    // Three tiering delays: a batch of promotions, and the batch after it, are at least one delay
    // apart, and the events that report them arrive late. See Stability.JitHasSettled.
    private const int TieringQuietMilliseconds = 3 * TieringSettleMilliseconds;

    // How long before a window began the runtime's last report must have been, for the window to
    // count as measured after it. The runtime starts the background compile just after it resumes
    // call counting, and the report of either reaches the gate a few milliseconds late.
    private const int TieringEventLagMilliseconds = 25;

    // How long the least window must have held for a measurement to count as settled although the
    // process never went quiet, see Stability.JitHasSettled.
    private const int TieringConvergenceMilliseconds = 5 * TieringQuietMilliseconds;

    // The longest a measurement whose windows agree waits for tiering to settle. Tiering can be
    // held back as long as any thread keeps calling methods for the first time, as at the start of
    // a large test run, so this is longer than StabilityTimeoutMilliseconds.
    private const int TieringTimeoutMilliseconds = 4 * StabilityTimeoutMilliseconds;

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
    /// concurrently. To compare the figures of two or three calls with each other, measure them
    /// with MeasureBytesPerCallInterleavedAsync instead.
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
    /// The bytes one call of each of <paramref name="first"/> and <paramref name="second"/>
    /// allocates on any thread on the path most of its calls take, measured side by side, for
    /// figures that are compared with each other. Every call is awaited. Counts process-wide, as
    /// <see cref="MeasureBytesPerCallAsync(int, Func{ValueTask})"/> does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After the warmup of each, the two run in alternating windows of
    /// <paramref name="iterations"/> calls until both are stable and tiered compilation has
    /// settled, as for <see cref="AssertNoMoreThanAsync(int, Func{ValueTask}, Func{ValueTask}, string)"/>.
    /// Then each runs <see cref="ModeWindows"/> more alternating windows in which every call is
    /// counted on its own, and each returns the figure most of its calls read. It throws if no
    /// figure holds more than half of a side's calls.
    /// </para>
    /// <para>
    /// A call that awaits real I/O allocates a different, exact amount on each path it can take:
    /// every await that yields boxes a state machine, and one that finds its result ready does
    /// not. Which path a call takes depends on timing, so on a loaded machine averages, the least
    /// window and the median window all move. The figure most calls read does not, and it stays
    /// exact, so one more object per call still shows. It does not see an allocation that only
    /// some calls make, such as a buffer rented every hundredth call: gate such a call with
    /// <see cref="AssertBudgetAsync(int, int, Func{ValueTask}, string)"/>. Run it where no other
    /// test runs concurrently.
    /// </para>
    /// </remarks>
    public static Task<(long First, long Second)> MeasureBytesPerCallInterleavedAsync<T1, T2>(
        int iterations, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second) =>
        MeasureBytesPerCallInterleavedAsync(iterations, DefaultAsyncWarmup, first, second);

    /// <summary>
    /// <see cref="MeasureBytesPerCallInterleavedAsync{T1, T2}(int, Func{ValueTask{T1}}, Func{ValueTask{T2}})"/>
    /// with <paramref name="warmup"/> warmup calls of each.
    /// </summary>
    public static Task<(long First, long Second)> MeasureBytesPerCallInterleavedAsync<T1, T2>(
        int iterations, int warmup, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);

        return Task.Run(() => MeasureModesAsync(iterations, warmup, first, second));
    }

    /// <summary>
    /// <see cref="MeasureBytesPerCallInterleavedAsync{T1, T2}(int, Func{ValueTask{T1}}, Func{ValueTask{T2}})"/>
    /// for three calls, which run in turn, one window each.
    /// </summary>
    public static Task<(long First, long Second, long Third)> MeasureBytesPerCallInterleavedAsync<T1, T2, T3>(
        int iterations, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second, Func<ValueTask<T3>> third) =>
        MeasureBytesPerCallInterleavedAsync(iterations, DefaultAsyncWarmup, first, second, third);

    /// <summary>
    /// <see cref="MeasureBytesPerCallInterleavedAsync{T1, T2, T3}(int, Func{ValueTask{T1}}, Func{ValueTask{T2}}, Func{ValueTask{T3}})"/>
    /// with <paramref name="warmup"/> warmup calls of each.
    /// </summary>
    public static Task<(long First, long Second, long Third)> MeasureBytesPerCallInterleavedAsync<T1, T2, T3>(
        int iterations, int warmup, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second, Func<ValueTask<T3>> third)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);

        return Task.Run(() => MeasureModesAsync(iterations, warmup, first, second, third));
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
            () => MeasureInterleavedAsync(iterations, warmup, toleranceBytesPerCall, baseline, candidate),
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
            () => MeasureInterleavedAsync(iterations, warmup, toleranceBytesPerCall, baseline, candidate),
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
        PerCall(ThrowIfTieringUnsettled(await Task.Run(measure).ConfigureAwait(false), nameof(MeasureBytesPerCallAsync)).Least, iterations);

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
        Func<Task<(Measurement Baseline, Measurement Candidate)>> measure, string label)
    {
        var (baseline, candidate) = await Task.Run(measure).ConfigureAwait(false);
        var allowed = baseline.Compared + ((long)toleranceBytesPerCall * iterations);

        if (candidate.Compared > allowed)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} candidate allocated {candidate.Compared} B total over {iterations} iterations " +
                $"({FormatPerCall(candidate.Compared, iterations)} B/call avg, {DescribeMedian(candidate)}), " +
                $"baseline allocated {baseline.Compared} B total " +
                $"({FormatPerCall(baseline.Compared, iterations)} B/call avg, {DescribeMedian(baseline)}), " +
                $"with a tolerance of {toleranceBytesPerCall} B/call. Both are counted on every thread. " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }

        // A baseline measured while the code was still tier 0 reads too high, so a candidate
        // within it proves nothing.
        _ = ThrowIfTieringUnsettled(baseline, label + " baseline");
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
        var begun = Stopwatch.GetTimestamp();

        for (int i = 0; i < warmup; i++) await action().ConfigureAwait(false);

        var stability = new Stability(totalBudget, (long)toleranceBytesPerCall * iterations, begun);
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
        var begun = Stopwatch.GetTimestamp();

        for (int i = 0; i < warmup; i++) _ = await action().ConfigureAwait(false);

        var stability = new Stability(totalBudget, (long)toleranceBytesPerCall * iterations, begun);
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

    // The two sides of AssertNoMoreThanAsync are measured in alternating windows, baseline,
    // candidate, baseline, candidate, until both are stable, and each keeps the same number of
    // windows (#76). Measured one after the other, the baseline and the candidate each ran until
    // their own windows agreed. Noise from other threads can keep that from happening for the
    // whole timeout, so one side could keep the least of hundreds of windows and the other the
    // least of ten, and the least of more windows is lower: equal calls then differed by more than
    // the tolerance about one run in a hundred. Alternating also gives both sides the same thread
    // pool, the same heap and the same background activity at every moment.
    private static async Task<(Measurement Baseline, Measurement Candidate)> MeasureInterleavedAsync(
        int iterations, int warmup, int toleranceBytesPerCall, Func<ValueTask> baseline, Func<ValueTask> candidate)
    {
        SettleHeap();
        var begun = Stopwatch.GetTimestamp();

        for (int i = 0; i < warmup; i++) await baseline().ConfigureAwait(false);
        var candidateBegun = Stopwatch.GetTimestamp();
        for (int i = 0; i < warmup; i++) await candidate().ConfigureAwait(false);

        var baselineStability = new Stability(NoBudget, (long)toleranceBytesPerCall * iterations, begun, keepWindows: true);
        var candidateStability = new Stability(NoBudget, (long)toleranceBytesPerCall * iterations, candidateBegun, keepWindows: true);
        while (true)
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) await baseline().ConfigureAwait(false);
            baselineStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            WaitForQuietProcess();
            before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) await candidate().ConfigureAwait(false);
            candidateStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            if (Stability.AllDone(baselineStability, candidateStability))
                return (baselineStability.Result, candidateStability.Result);
        }
    }

    private static async Task<(Measurement Baseline, Measurement Candidate)> MeasureInterleavedAsync<TBaseline, TCandidate>(
        int iterations, int warmup, int toleranceBytesPerCall, Func<ValueTask<TBaseline>> baseline, Func<ValueTask<TCandidate>> candidate)
    {
        SettleHeap();
        var begun = Stopwatch.GetTimestamp();

        for (int i = 0; i < warmup; i++) _ = await baseline().ConfigureAwait(false);
        var candidateBegun = Stopwatch.GetTimestamp();
        for (int i = 0; i < warmup; i++) _ = await candidate().ConfigureAwait(false);

        var baselineStability = new Stability(NoBudget, (long)toleranceBytesPerCall * iterations, begun, keepWindows: true);
        var candidateStability = new Stability(NoBudget, (long)toleranceBytesPerCall * iterations, candidateBegun, keepWindows: true);
        while (true)
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await baseline().ConfigureAwait(false);
            baselineStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            WaitForQuietProcess();
            before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await candidate().ConfigureAwait(false);
            candidateStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            if (Stability.AllDone(baselineStability, candidateStability))
                return (baselineStability.Result, candidateStability.Result);
        }
    }

    // MeasureBytesPerCallInterleavedAsync (#434). The windows run as in MeasureInterleavedAsync
    // until all sides are stable, so the code counted is the optimised code. Then each side runs
    // ModeWindows more windows in which every call is counted on its own, into a histogram of
    // bytes per call.
    private static async Task<(long First, long Second)> MeasureModesAsync<T1, T2>(
        int iterations, int warmup, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second)
    {
        var (a, b) = await MeasureInterleavedAsync(iterations, warmup, DefaultAsyncToleranceBytesPerCall, first, second).ConfigureAwait(false);
        _ = ThrowIfTieringUnsettled(a, ModeSide(1));
        _ = ThrowIfTieringUnsettled(b, ModeSide(2));
        return await CountModesAsync(iterations, first, second).ConfigureAwait(false);
    }

    private static async Task<(long First, long Second, long Third)> MeasureModesAsync<T1, T2, T3>(
        int iterations, int warmup, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second, Func<ValueTask<T3>> third)
    {
        var (a, b, c) = await MeasureInterleavedAsync(iterations, warmup, DefaultAsyncToleranceBytesPerCall, first, second, third).ConfigureAwait(false);
        _ = ThrowIfTieringUnsettled(a, ModeSide(1));
        _ = ThrowIfTieringUnsettled(b, ModeSide(2));
        _ = ThrowIfTieringUnsettled(c, ModeSide(3));
        return await CountModesAsync(iterations, first, second, third).ConfigureAwait(false);
    }

    // The per-call windows of MeasureModesAsync. One uncounted call of each comes first, so this
    // method's own state machine is boxed at its first yield, outside a counted call. A histogram
    // grows between two calls, never inside one.
    private static async Task<(long First, long Second)> CountModesAsync<T1, T2>(
        int iterations, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second)
    {
        _ = await first().ConfigureAwait(false);
        _ = await second().ConfigureAwait(false);

        var firstCalls = new CallHistogram();
        var secondCalls = new CallHistogram();
        for (int window = 0; window < ModeWindows; window++)
        {
            WaitForQuietProcess();
            for (int i = 0; i < iterations; i++)
            {
                var before = GC.GetTotalAllocatedBytes(precise: true);
                _ = await first().ConfigureAwait(false);
                firstCalls.Add(GC.GetTotalAllocatedBytes(precise: true) - before);
            }

            WaitForQuietProcess();
            for (int i = 0; i < iterations; i++)
            {
                var before = GC.GetTotalAllocatedBytes(precise: true);
                _ = await second().ConfigureAwait(false);
                secondCalls.Add(GC.GetTotalAllocatedBytes(precise: true) - before);
            }
        }

        return (firstCalls.Mode(ModeSide(1)), secondCalls.Mode(ModeSide(2)));
    }

    private static async Task<(long First, long Second, long Third)> CountModesAsync<T1, T2, T3>(
        int iterations, Func<ValueTask<T1>> first, Func<ValueTask<T2>> second, Func<ValueTask<T3>> third)
    {
        _ = await first().ConfigureAwait(false);
        _ = await second().ConfigureAwait(false);
        _ = await third().ConfigureAwait(false);

        var firstCalls = new CallHistogram();
        var secondCalls = new CallHistogram();
        var thirdCalls = new CallHistogram();
        for (int window = 0; window < ModeWindows; window++)
        {
            WaitForQuietProcess();
            for (int i = 0; i < iterations; i++)
            {
                var before = GC.GetTotalAllocatedBytes(precise: true);
                _ = await first().ConfigureAwait(false);
                firstCalls.Add(GC.GetTotalAllocatedBytes(precise: true) - before);
            }

            WaitForQuietProcess();
            for (int i = 0; i < iterations; i++)
            {
                var before = GC.GetTotalAllocatedBytes(precise: true);
                _ = await second().ConfigureAwait(false);
                secondCalls.Add(GC.GetTotalAllocatedBytes(precise: true) - before);
            }

            WaitForQuietProcess();
            for (int i = 0; i < iterations; i++)
            {
                var before = GC.GetTotalAllocatedBytes(precise: true);
                _ = await third().ConfigureAwait(false);
                thirdCalls.Add(GC.GetTotalAllocatedBytes(precise: true) - before);
            }
        }

        return (firstCalls.Mode(ModeSide(1)), secondCalls.Mode(ModeSide(2)), thirdCalls.Mode(ModeSide(3)));
    }

    // Three calls in turn, one window each, as MeasureInterleavedAsync runs two.
    private static async Task<(Measurement First, Measurement Second, Measurement Third)> MeasureInterleavedAsync<T1, T2, T3>(
        int iterations, int warmup, int toleranceBytesPerCall,
        Func<ValueTask<T1>> first, Func<ValueTask<T2>> second, Func<ValueTask<T3>> third)
    {
        SettleHeap();
        var begun = Stopwatch.GetTimestamp();

        for (int i = 0; i < warmup; i++) _ = await first().ConfigureAwait(false);
        var secondBegun = Stopwatch.GetTimestamp();
        for (int i = 0; i < warmup; i++) _ = await second().ConfigureAwait(false);
        var thirdBegun = Stopwatch.GetTimestamp();
        for (int i = 0; i < warmup; i++) _ = await third().ConfigureAwait(false);

        var agreementBytes = (long)toleranceBytesPerCall * iterations;
        var firstStability = new Stability(NoBudget, agreementBytes, begun, keepWindows: true);
        var secondStability = new Stability(NoBudget, agreementBytes, secondBegun, keepWindows: true);
        var thirdStability = new Stability(NoBudget, agreementBytes, thirdBegun, keepWindows: true);
        while (true)
        {
            WaitForQuietProcess();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await first().ConfigureAwait(false);
            firstStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            WaitForQuietProcess();
            before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await second().ConfigureAwait(false);
            secondStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            WaitForQuietProcess();
            before = GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < iterations; i++) _ = await third().ConfigureAwait(false);
            thirdStability.Record(GC.GetTotalAllocatedBytes(precise: true) - before);

            if (Stability.AllDone(firstStability, secondStability, thirdStability))
                return (firstStability.Result, secondStability.Result, thirdStability.Result);
        }
    }

    private static string ModeSide(int side) =>
        $"{nameof(MeasureBytesPerCallInterleavedAsync)}, {(side == 1 ? "first" : side == 2 ? "second" : "third")} call";

    // How many calls read each number of bytes. The dictionary grows between two calls, never
    // inside one.
    private sealed class CallHistogram
    {
        private readonly Dictionary<long, int> _calls = new Dictionary<long, int>(capacity: 64);
        private int _total;

        public void Add(long bytes)
        {
            _calls[bytes] = _calls.GetValueOrDefault(bytes) + 1;
            _total++;
        }

        // The bytes most calls read, if more than half did. Otherwise no path is the call's
        // usual one, and any figure would depend on timing, so it throws with the histogram.
        public long Mode(string what)
        {
            var mode = 0L;
            var count = 0;
            foreach (var (bytes, calls) in _calls)
            {
                if (calls > count)
                    (mode, count) = (bytes, calls);
            }

            if (count * 2 > _total)
                return mode;

            var paths = new List<KeyValuePair<long, int>>(_calls);
            paths.Sort(static (a, b) => b.Value.CompareTo(a.Value));
            var shown = string.Join(", ", paths.GetRange(0, Math.Min(paths.Count, 6)).ConvertAll(p =>
                string.Create(CultureInfo.InvariantCulture, $"{p.Key} B in {p.Value}")));
            throw new InvalidOperationException(
                $"AllocationGate: {what} took no one path in most of its {_total} calls: {shown}. " +
                "A call allocates a different amount on each path, for example when an await does or does not " +
                "yield, and here which path it takes depends on timing. Run it where the machine is less busy, " +
                "or gate an allocation that only some calls make with AssertBudgetAsync.");
        }
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
        var begun = Stopwatch.GetTimestamp();

        // Warmup: JIT-compile, populate type-handle caches and pools, allocate one-time fixtures.
        for (int i = 0; i < DefaultAsyncWarmup; i++) action();

        var stability = new Stability(totalBudget, agreementBytes: 0, begun);
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
        var begun = Stopwatch.GetTimestamp();

        for (int i = 0; i < DefaultAsyncWarmup; i++) Drain(action());

        var stability = new Stability(totalBudget, agreementBytes: 0, begun);
        while (true)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) Drain(action());
            if (stability.IsDone(GC.GetAllocatedBytesForCurrentThread() - before))
                return stability.Result;

            PauseWhileTiering(stability);
        }
    }

    /// <summary>
    /// The least window a gate measured, how many windows it took, whether they settled, and
    /// whether the measurement ended because tiered compilation never did.
    /// </summary>
    private readonly record struct Measurement(
        long Least, int Windows, bool Stable, bool TieringUnsettled = false, long? Median = null)
    {
        // What two measurements are compared by: the median when the gate kept the windows, the
        // least otherwise.
        public long Compared => Median ?? Least;
    }

    // When to stop measuring windows, shared by every gate (#89, #94).
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
    //   - or tiered compilation has settled, see JitHasSettled. Without a JIT, as in a Native AOT
    //     app, the code never changes and two agreeing windows are enough.
    //
    // The runtime promotes the methods of a call path in batches, with a gap of at least the
    // tiering delay between two of them, and the signals that say a promotion is under way reach
    // the gate late. Allocation only ever goes down as code is promoted, so the measurement itself
    // is the last word: a window that is lower than the least so far shows that the code changed,
    // and settling starts again from there.
    //
    // A measurement whose windows do not agree within StabilityTimeoutMilliseconds stops there with
    // the least window seen, marked as never stable. One whose tiering has not settled keeps
    // waiting, whether its windows agree or not, up to TieringTimeoutMilliseconds and is then
    // marked as such: its figure can be the tier-0 one, so the gates that return a figure throw
    // instead of returning it. It always measures at least two windows.
    private sealed class Stability
    {
        private readonly long _totalBudget;
        private readonly long _agreementBytes;
        private readonly long _begun; // before the warmup, so before the first call of the code measured
        private readonly long _started;
        private readonly List<long> _all = new List<long>();
        private readonly bool _keepWindows;
        private long _compiledMethods;
        private long _lastCompile;
        private long _lastDecrease;
        private long _lastRecord;
        private long _windowStarted;
        private long _previous;
        private long _least = long.MaxValue;
        private int _windows;
        private bool _agrees;

        // keepWindows: remember every window, for the median.
        public Stability(long totalBudget, long agreementBytes, long begun, bool keepWindows = false)
        {
            _keepWindows = keepWindows;
            _totalBudget = totalBudget;
            _agreementBytes = agreementBytes;
            _begun = begun;
            _started = Stopwatch.GetTimestamp();
            _lastCompile = _started;
            _lastDecrease = _started;
            _lastRecord = _started;
            _windowStarted = _started;
            _compiledMethods = JitInfo.GetCompiledMethodCount();
        }

        public Measurement Result { get; private set; }

        // The last two windows agree and are over the budget, so only tiered compilation is
        // awaited.
        public bool WaitingForTiering { get; private set; }

        // Records one window and returns whether to stop. Runs between windows, never inside one.
        public bool IsDone(long window)
        {
            Record(window);
            return Evaluate();
        }

        public void Record(long window)
        {
            var now = Stopwatch.GetTimestamp();
            var compiledMethods = JitInfo.GetCompiledMethodCount();
            if (compiledMethods != _compiledMethods)
            {
                _compiledMethods = compiledMethods;
                _lastCompile = now;
            }

            _agrees = _windows > 0 && Math.Abs(window - _previous) <= _agreementBytes;
            // A decrease within twice the agreement is the spread of the windows, not the code
            // changing: a call that completes on another thread varies by a few percent.
            if (_windows > 0 && window < _least - (2 * _agreementBytes))
                _lastDecrease = now;

            if (_keepWindows)
                _all.Add(window);
            _previous = window;
            _least = Math.Min(_least, window);
            _windows++;
            _windowStarted = _lastRecord;
            _lastRecord = now;
        }

        // Whether to stop now, after the windows recorded so far. Sets Result when it returns true.
        public bool Evaluate()
        {
            var now = Stopwatch.GetTimestamp();
            var withinBudget = _least <= _totalBudget;
            var settled = withinBudget || JitHasSettled(now);
            var stable = _agrees && settled;
            WaitingForTiering = _agrees && !stable;
            if (stable)
            {
                Result = new Measurement(_least, _windows, Stable: true);
                return true;
            }

            if (_windows < 2)
                return false;

            // Windows that disagree while tiering is still going on are not an allocation that
            // varies: the code is changing under the measurement. So the wait for tiering has its
            // own, longer limit, and a figure from it is not returned.
            var elapsed = Stopwatch.GetElapsedTime(_started, now).TotalMilliseconds;
            if (elapsed >= TieringTimeoutMilliseconds || (elapsed >= StabilityTimeoutMilliseconds && settled))
            {
                Result = new Measurement(_least, _windows, Stable: false, TieringUnsettled: !settled);
                return true;
            }

            return false;
        }

        // All sides of a comparison stop together, on the same number of windows, when each is
        // stable and they have ComparisonWindows each, or when each has stopped and one has run
        // out of time. The result then has the median of the windows.
        public static bool AllDone(params ReadOnlySpan<Stability> sides)
        {
            // Every side is evaluated, so each one's Result is current.
            var allDone = true;
            var allStable = true;
            foreach (var side in sides)
            {
                allDone &= side.Evaluate();
                allStable &= side.Result.Stable;
            }

            if (!allDone || (allStable && sides[0]._windows < ComparisonWindows))
                return false;

            foreach (var side in sides)
                side.Result = side.Result with { Median = side.Median() };
            return true;
        }

        private long Median()
        {
            var sorted = _all.ToArray();
            Array.Sort(sorted);
            return sorted[(sorted.Length - 1) / 2];
        }

        // Tiering has settled when the runtime reports no tier-up under way, see Tiering, and
        // either of two things holds.
        //
        // Quiet. Nothing has hinted at a tier-up for long enough:
        //
        //   - no method was JIT-compiled for TieringSettleMilliseconds. The JIT's own counter is
        //     exact and has no delay, which the runtime events have;
        //   - no tiering event, and no window lower than the least, for TieringQuietMilliseconds,
        //     and not before that long after the measurement began. The events reach the gate late,
        //     and the runtime reports the start of a delay after it has started it, so a delay that
        //     the call path's own first calls began can be unreported for a while. Methods are
        //     promoted in batches, and the second batch of a method, from instrumented tier 0 to
        //     tier 1, can wait for a delay of its own.
        //
        // Converged. In a process where other threads keep calling new methods, the first
        // condition may never hold, because every new method restarts the delay. Then the
        // measurement itself decides: the runtime has been seen to start call counting after the
        // gate began, which it does for the methods the call path first called, and the least
        // window has not gone down for TieringConvergenceMilliseconds. Allocation only goes down
        // as code is promoted, so a method still waiting for its turn would have shown by now. And
        // the runtime reports no delay and no compile at this moment, so nothing is holding the
        // call path back right now either.
        private bool JitHasSettled(long now)
        {
            if (!RuntimeFeature.IsDynamicCodeCompiled || s_tieringOff)
                return true;

            // The last window began a while after the runtime's last report. A window that ran
            // while a batch of promotions was being installed, or just before one began, says
            // nothing about the code after it.
            if (Stopwatch.GetElapsedTime(Tiering.LastEvent, _windowStarted).TotalMilliseconds < TieringEventLagMilliseconds)
                return false;

            var compileQuiet = Stopwatch.GetElapsedTime(_lastCompile, now).TotalMilliseconds;
            var lastChange = Math.Max(_started, _lastDecrease);
            var lastActivity = Math.Max(lastChange, Tiering.LastEvent);
            if (compileQuiet >= TieringSettleMilliseconds && Tiering.IsIdle(now, compileQuiet) &&
                Stopwatch.GetElapsedTime(lastActivity, now).TotalMilliseconds >= TieringQuietMilliseconds)
            {
                return true;
            }

            return Tiering.IsCountingAndIdle && Tiering.LastResume > _begun &&
                Stopwatch.GetElapsedTime(lastChange, now).TotalMilliseconds >= TieringConvergenceMilliseconds;
        }
    }

    // With tiered compilation off the code is optimised from the first call and never changes.
    private static readonly bool s_tieringOff = DetectTieringOff();

    private static bool DetectTieringOff() =>
        (AppContext.TryGetSwitch("System.Runtime.TieredCompilation", out var enabled) && !enabled) ||
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"), "0", StringComparison.Ordinal) ||
        string.Equals(Environment.GetEnvironmentVariable("COMPlus_TieredCompilation"), "0", StringComparison.Ordinal);

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
    //   284 TieredCompilationBackgroundJitStop    it has finished, or yielded for a moment.
    //
    // The delay restarts whenever any thread calls a method for the first time, precompiled
    // ReadyToRun code included, which compiles nothing. So a JIT that has been quiet for a while
    // does not show that no tier-up is on its way: in a test host the delay can last several
    // hundred milliseconds, and only the pause and resume events show it.
    //
    // The events are not a precise clock. They reach the listener on the runtime's dispatch
    // thread, in bursts, and the runtime sends the pause after it has started the delay, so the
    // state below is what was true a little while ago. It says that tiering is active; it cannot
    // say that tiering is not, which the quiet period in Stability.JitHasSettled covers.
    //
    // One listener serves the whole process; it is created at the first gate and never disposed.
    // Its state starts unknown, because a delay can already be running, and is treated as paused:
    // the first gate waits for the runtime to report where it is. A process where no event
    // arrives is treated as idle a second after the listener started, when the JIT has also
    // compiled nothing for a while: EventSource support can be disabled, or a process that
    // has run everything already has no tier-up left to report. While the JIT keeps compiling, the
    // delay is running and the events will come. The listener's own work happens on the runtime's
    // event dispatch thread, so the synchronous gates never count it, and the asynchronous ones
    // only while tiering is active.
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
        private long _lastResume;

        // Starts the listener, ahead of the warmup, so the warmup's first calls are seen.
        public static void EnsureStarted() => _ = Instance;

        public static long LastEvent => Volatile.Read(ref Instance._lastEvent);

        public static long LastResume => Volatile.Read(ref Instance._lastResume);

        // The runtime has reported that call counting is on and nothing is compiling.
        public static bool IsCountingAndIdle =>
            Volatile.Read(ref Instance._state) == Counting && Volatile.Read(ref Instance._backgroundJit) == 0;

        // compileQuietMilliseconds: how long the JIT has compiled nothing.
        public static bool IsIdle(long now, double compileQuietMilliseconds)
        {
            if (Volatile.Read(ref Instance._backgroundJit) != 0)
                return false;

            return Volatile.Read(ref Instance._state) switch
            {
                Counting => true,
                Unknown => Stopwatch.GetElapsedTime(Instance._created, now).TotalMilliseconds >= NoEventsTimeoutMilliseconds &&
                    compileQuietMilliseconds >= TieringQuietMilliseconds,
                _ => false,
            };
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
            var now = Stopwatch.GetTimestamp();
            switch (eventData.EventId)
            {
                case 281:
                    Volatile.Write(ref _state, Paused);
                    break;
                case 282:
                    Volatile.Write(ref _state, Counting);
                    Volatile.Write(ref _lastResume, now);
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

            Volatile.Write(ref _lastEvent, now);
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
        if (RuntimeFeature.IsDynamicCodeCompiled && !s_tieringOff)
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
            : measured.TieringUnsettled
            ? $"the least of {measured.Windows} windows; tiered compilation was still active after {TieringTimeoutMilliseconds / 1000} s, " +
              "so the code measured may still be unoptimised"
            : $"the least of {measured.Windows} windows; the measurement never stabilised within " +
              $"{StabilityTimeoutMilliseconds / 1000} s, so the windows still disagreed or tiered compilation was still active";

    private static string DescribeMedian(Measurement measured) =>
        $"the median of {measured.Windows} windows, the least {measured.Least} B" +
        (measured.Stable ? "" : "; the measurement never stabilised, so the windows still disagreed or tiered compilation was still active");

    // A figure measured while tiered compilation never settled can be the one of tier-0 code, which
    // allocates where the code that keeps running does not. Returning it would be a wrong answer
    // that looks like a right one.
    private static Measurement ThrowIfTieringUnsettled(Measurement measured, string what)
    {
        if (measured.TieringUnsettled)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {what} could not be measured in steady state: tiered compilation was still active " +
                $"{TieringTimeoutMilliseconds / 1000} s after the warmup, so the code measured may still be unoptimised and " +
                $"allocate more than it does once it is (the least of {measured.Windows} windows read {measured.Least} B in total). " +
                "The runtime holds tiering back while any thread keeps calling methods for the " +
                "first time, as a large test run does at its start. Run the test where nothing else is starting up, " +
                "for example in a collection of its own, or call the code under test before the gate.");
        }

        return measured;
    }

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
