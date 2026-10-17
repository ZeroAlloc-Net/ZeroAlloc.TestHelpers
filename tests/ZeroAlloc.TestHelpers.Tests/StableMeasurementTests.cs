using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// The gates measure until the measurement is stable (#89). Tiered compilation runs unoptimised
/// tier-0 code first and installs the optimised code after a delay in time, so a fixed number of
/// windows can measure only tier-0 code. These tests check that a gate keeps measuring while the
/// code may still change, that it stops early once it can, and that it still fails on an
/// allocation that stays.
/// </summary>
/// <remarks>
/// Not parallel with the other tests: the asynchronous cases count process-wide.
/// </remarks>
[Collection(ProcessWideCollection.Name)]
public sealed class StableMeasurementTests
{
    private const int Iterations = 1_000;

    // Shorter than AllocationGate.TieringSettleMilliseconds, which every gate without a passing
    // window waits for after its warmup, so the gate cannot stop before the allocations do.
    private static readonly TimeSpan AllocatesFor = TimeSpan.FromMilliseconds(AllocationGate.TieringSettleMilliseconds / 2);

    [Fact]
    public void AssertBudget_SteadyAllocation_FailsAndReportsItExactly()
    {
        // 40 B of payload, plus the 24 B header and length of an array on a 64-bit runtime.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudget(0, Iterations, static () => Escape.Sink = new byte[40], "steady allocator"));

        Assert.Contains("allocated 64000 B total over 1000 iterations (64 B/call avg, the least of ", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("never stabilised", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssertBudgetAsync_SteadyAllocation_FailsAndReportsIt()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertBudgetAsync(0, Iterations, static () =>
            {
                Escape.Sink = new byte[40];
                return default;
            }, "steady allocator"));

        Assert.Contains("(64 B/call avg, the least of ", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("never stabilised", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertBudget_AllocationFree_StopsAfterTwoWindows()
    {
        // Within its budget from the first window, so it does not wait for tiered compilation.
        long calls = 0;

        AllocationGate.AssertBudget(0, Iterations, () => calls++, "no-op");
        AllocationGate.AssertBudgetValueTask(0, Iterations, () => new ValueTask<long>(calls++), "no-op");

        Assert.Equal(2 * (AllocationGate.DefaultAsyncWarmup + (2 * Iterations)), calls);
    }

    [Fact]
    public void AssertBudget_SteadyWithinBudget_StopsAfterTwoWindows()
    {
        long calls = 0;

        AllocationGate.AssertBudget(64, Iterations, () =>
        {
            calls++;
            Escape.Sink = new byte[40];
        }, "steady allocator within budget");

        Assert.Equal(AllocationGate.DefaultAsyncWarmup + (2 * Iterations), calls);
    }

    [Fact]
    public void AssertBudget_AllocationThatStopsAfterADelay_Passes()
    {
        var call = new AllocatesUntil(AllocatesFor);

        AllocationGate.AssertBudget(0, Iterations, call.Call, "allocates for a while");

        Assert.True(call.AllocatedAfterWarmup, "the allocations stopped before the warmup ended");
    }

    [Fact]
    public void AssertBudgetValueTask_AllocationThatStopsAfterADelay_Passes()
    {
        var call = new AllocatesUntil(AllocatesFor);

        AllocationGate.AssertBudgetValueTask(0, Iterations, () =>
        {
            call.Call();
            return new ValueTask<int>(1);
        }, "allocates for a while");

        Assert.True(call.AllocatedAfterWarmup, "the allocations stopped before the warmup ended");
    }

    [Fact]
    public void MeasureBytesPerCall_AllocationThatStopsAfterADelay_ReadsZero()
    {
        var call = new AllocatesUntil(AllocatesFor);

        var perCall = AllocationGate.MeasureBytesPerCall(Iterations, call.Call);

        Assert.True(call.AllocatedAfterWarmup, "the allocations stopped before the warmup ended");
        Assert.Equal(0, perCall);
    }

    [Fact]
    public void AssertNoMoreThan_CandidateThatStopsAllocatingAfterADelay_Passes()
    {
        var call = new AllocatesUntil(AllocatesFor);

        AllocationGate.AssertNoMoreThan(Iterations, static () => { }, call.Call, "allocates for a while");

        Assert.True(call.AllocatedAfterWarmup, "the allocations stopped before the warmup ended");
    }

    [Fact]
    public async Task AssertBudgetAsync_AllocationThatStopsAfterADelay_Passes()
    {
        var call = new AllocatesUntil(AllocatesFor);

        await AllocationGate.AssertBudgetAsync(0, Iterations, () =>
        {
            call.Call();
            return default;
        }, "allocates for a while");

        Assert.True(call.AllocatedAfterWarmup, "the allocations stopped before the warmup ended");
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_AllocationThatStopsAfterADelay_ReadsZero()
    {
        var call = new AllocatesUntil(AllocatesFor);

        var perCall = await AllocationGate.MeasureBytesPerCallAsync(Iterations, () =>
        {
            call.Call();
            return default;
        });

        Assert.True(call.AllocatedAfterWarmup, "the allocations stopped before the warmup ended");
        Assert.Equal(0, perCall);
    }

    [Fact]
    public void AssertBudget_NeverStable_FailsAtTheTimeoutAndSaysSo()
    {
        var call = new AlternatesEveryWindow(AllocationGate.DefaultAsyncWarmup, Iterations);
        var started = Stopwatch.GetTimestamp();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudget(0, Iterations, call.Call, "unstable allocator"));

        Assert.True(
            Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(AllocationGate.StabilityTimeoutMilliseconds),
            "the gate stopped before the timeout");
        // The least window is the one with the smaller array: 32 B of payload plus its header.
        Assert.Contains("allocated 56000 B total over 1000 iterations (56 B/call avg, the least of ", ex.Message, StringComparison.Ordinal);
        Assert.Contains("the measurement never stabilised within 5 s", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssertBudgetAsync_NeverStable_FailsAtTheTimeoutAndSaysSo()
    {
        const int warmup = 100;
        var call = new AlternatesEveryWindow(warmup, Iterations);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertBudgetAsync(0, AllocationGate.DefaultAsyncToleranceBytesPerCall, Iterations, warmup, () =>
            {
                call.Call();
                return default;
            }, "unstable allocator"));

        Assert.Contains("the measurement never stabilised within 5 s", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertBudget_TierZeroBoxing_IsNotCounted()
    {
        // Tier-0 code boxes the value for the interface check and call, 24 B per call; the
        // optimised code calls TryFormat on the value directly. The fixed three windows of #87
        // read 24 B/call here: the JIT tiers up after a delay in time, after the whole gate had
        // run. With tiered compilation off, or under Native AOT, nothing is boxed to begin with.
        AllocationGate.AssertBudget(0, Iterations, static () => s_written += Format(42), "tier-0 boxing");
        Assert.Equal(0, AllocationGate.MeasureBytesPerCall(Iterations, static () => s_written += Format(42L)));
    }

    private static readonly char[] s_buffer = new char[32];
    private static long s_written;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Format<T>(T value) =>
        value is ISpanFormattable formattable && formattable.TryFormat(s_buffer, out var written, default, null)
            ? written
            : 0;

    /// <summary>
    /// A call that allocates a small array on every call for a while after its first call, then
    /// never again: what tier-0 code that tiers up looks like to the gate.
    /// </summary>
    private sealed class AllocatesUntil(TimeSpan duration)
    {
        private long _stopAt;
        private long _calls;

        public bool AllocatedAfterWarmup { get; private set; }

        public void Call()
        {
            var now = Stopwatch.GetTimestamp();
            if (_stopAt == 0)
                _stopAt = now + (long)(duration.TotalSeconds * Stopwatch.Frequency);

            if (now < _stopAt)
            {
                Escape.Sink = new byte[64];
                if (_calls >= AllocationGate.DefaultAsyncWarmup)
                    AllocatedAfterWarmup = true;
            }

            _calls++;
        }
    }

    /// <summary>
    /// A call that allocates a 32 B array in every other window and a 64 B array in the windows
    /// between, so no two consecutive windows ever agree.
    /// </summary>
    private sealed class AlternatesEveryWindow(int warmup, int iterations)
    {
        private long _calls;

        public void Call()
        {
            var window = Math.Max(0, _calls++ - warmup) / iterations;
            Escape.Sink = window % 2 == 0 ? new byte[32] : new byte[64];
        }
    }
}
