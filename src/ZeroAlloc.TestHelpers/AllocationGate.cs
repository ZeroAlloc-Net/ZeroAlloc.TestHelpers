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
    /// <paramref name="iterations"/> calls after the same warmup and GC as the asserts. The
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

    private static long MeasureTotal(int iterations, Action action)
    {
        // Warmup — JIT-compile, populate type-handle caches, allocate one-time fixtures.
        action();
        action();

        FlushWarmupGarbage();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++) action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long MeasureTotalValueTask<T>(int iterations, Func<ValueTask<T>> action)
    {
        // Warmup.
        Drain(action());
        Drain(action());

        FlushWarmupGarbage();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++) Drain(action());
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static T Drain<T>(ValueTask<T> t)
    {
        if (!t.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException(
                "AllocationGate: sync-completion-required — the supplied ValueTask did not " +
                "complete synchronously. Awaiter machinery would pollute the measurement; " +
                "the API under test must return an already-completed ValueTask.");
        }
        return t.Result;
    }

    // Flush warmup garbage so it can't leak into the measurement.
    private static void FlushWarmupGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    // Rounded up: integer division would report an occasional small allocation as 0 B/call.
    private static long PerCall(long allocated, int iterations) =>
        (allocated + iterations - 1) / iterations;

    private static void ThrowIfOverBudget(int budgetBytes, int iterations, long allocated, string label)
    {
        var totalBudget = (long)budgetBytes * iterations;
        if (allocated > totalBudget)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} allocated {allocated} B total over {iterations} iterations " +
                $"(~{PerCall(allocated, iterations)} B/call avg), budget is {budgetBytes} B/call ({totalBudget} B total). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }

    private static void ThrowIfOverBaseline(int iterations, long baselineTotal, long candidateTotal, string label)
    {
        if (candidateTotal > baselineTotal)
        {
            throw new InvalidOperationException(
                $"AllocationGate: {label} candidate allocated {candidateTotal} B total over {iterations} iterations " +
                $"(~{PerCall(candidateTotal, iterations)} B/call avg), baseline allocated {baselineTotal} B total " +
                $"(~{PerCall(baselineTotal, iterations)} B/call avg). " +
                "Use BenchmarkDotNet [MemoryDiagnoser] locally to find the culprit.");
        }
    }
}
