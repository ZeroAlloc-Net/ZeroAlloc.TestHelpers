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
    public async Task AssertNoMoreThanAsync_EqualCalls_AreMeasuredOverTheSameWindows()
    {
        // The two sides run in alternating windows and stop together (#76). One after the other,
        // the baseline kept measuring until its own windows agreed, the candidate stopped at the
        // first window within the baseline, and the side with more windows kept the lower least:
        // equal calls then differed by more than the tolerance about one run in a hundred.
        long baselineCalls = 0;
        long candidateCalls = 0;

        await AllocationGate.AssertNoMoreThanAsync(
            Iterations,
            () =>
            {
                baselineCalls++;
                Escape.Sink = new byte[40];
                return default;
            },
            () =>
            {
                candidateCalls++;
                Escape.Sink = new byte[40];
                return default;
            },
            "equal calls");

        Assert.Equal(baselineCalls, candidateCalls);
    }

    [Fact]
    public async Task AssertNoMoreThanAsync_EqualValueTaskCalls_AreMeasuredOverTheSameWindows()
    {
        long baselineCalls = 0;
        long candidateCalls = 0;

        await AllocationGate.AssertNoMoreThanAsync(
            Iterations,
            () =>
            {
                baselineCalls++;
                Escape.Sink = new byte[40];
                return new ValueTask<int>(1);
            },
            () =>
            {
                candidateCalls++;
                Escape.Sink = new byte[40];
                return new ValueTask<int>(2);
            },
            "equal calls");

        Assert.Equal(baselineCalls, candidateCalls);
    }

    [Fact]
    public async Task AssertNoMoreThanAsync_BaselineWithAFewLuckyWindows_DoesNotFailAnEqualCandidate()
    {
        // A call that completes on another thread now and then finds its work already done and
        // allocates less, for a window or two (#76). The least window of the baseline then lay
        // several bytes per call under the candidate's. The median is not moved by a few windows.
        const int warmup = 100;
        var baseline = new LuckyWindows(warmup, Iterations, lucky: [3, 7, 11]);
        var candidate = new LuckyWindows(warmup, Iterations, lucky: []);

        await AllocationGate.AssertNoMoreThanAsync(
            Iterations, AllocationGate.DefaultAsyncToleranceBytesPerCall, warmup, baseline.Call, candidate.Call, "lucky baseline");

        Assert.True(baseline.LuckyCalls > 0, "the lucky windows never ran");
    }

    [Fact]
    public async Task MeasureBytesPerCallInterleavedAsync_ThreeSides_ReturnsEachExactly()
    {
        // Arrays of 40 B and 64 B of payload, plus the 24 B header and length of an array.
        long first = 0, second = 0, third = 0;

        var (a, b, c) = await AllocationGate.MeasureBytesPerCallInterleavedAsync(
            Iterations,
            () =>
            {
                first++;
                Escape.Sink = new byte[40];
                return new ValueTask<int>(1);
            },
            () =>
            {
                second++;
                return new ValueTask<string>("none");
            },
            () =>
            {
                third++;
                Escape.Sink = new byte[64];
                return new ValueTask<long>(3);
            });

        Assert.Equal((64, 0, 88), (a, b, c));
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public async Task MeasureBytesPerCallInterleavedAsync_TwoSides_ReturnsEachExactly()
    {
        long first = 0, second = 0;

        var (a, b) = await AllocationGate.MeasureBytesPerCallInterleavedAsync(
            Iterations,
            () =>
            {
                first++;
                Escape.Sink = new byte[40];
                return new ValueTask<int>(1);
            },
            () =>
            {
                second++;
                Escape.Sink = new byte[64];
                return new ValueTask<string>("two");
            });

        Assert.Equal((64, 88), (a, b));
        Assert.Equal(first, second);
    }

    [Fact]
    public void MeasureBytesPerCallInterleavedAsync_InvalidArguments_ThrowBeforeMeasuring()
    {
        // Thrown by the call itself, as the other gates do, not by the task it returns.
        Func<ValueTask<int>> call = static () => new ValueTask<int>(1);

        _ = Assert.Throws<ArgumentNullException>(() => { _ = AllocationGate.MeasureBytesPerCallInterleavedAsync(Iterations, call, (Func<ValueTask<int>>)null!); });
        _ = Assert.Throws<ArgumentNullException>(() => { _ = AllocationGate.MeasureBytesPerCallInterleavedAsync(Iterations, call, call, (Func<ValueTask<int>>)null!); });
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = AllocationGate.MeasureBytesPerCallInterleavedAsync(0, call, call); });
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = AllocationGate.MeasureBytesPerCallInterleavedAsync(Iterations, -1, call, call, call); });
    }

    [Fact]
    public async Task MeasureBytesPerCallInterleavedAsync_SideWithAFewLuckyWindows_ReadsItsSteadyState()
    {
        // ZeroAlloc.ORM#434: a call that completes on another thread now and then allocates less
        // for a window, and the least window of one side then lay below its steady state. The
        // median of each side is not moved by a few windows, in either direction.
        const int warmup = 100;
        var lucky = new LuckyWindows(warmup, Iterations, lucky: [3, 7, 11]);
        var steady = new LuckyWindows(warmup, Iterations, lucky: []);

        var (luckyBytes, steadyBytes) = await AllocationGate.MeasureBytesPerCallInterleavedAsync(
            Iterations, warmup, lucky.CallValue, steady.CallValue);

        Assert.True(lucky.LuckyCalls > 0, "the lucky windows never ran");
        Assert.Equal((64, 64), (luckyBytes, steadyBytes));
    }

    [Fact]
    public async Task MeasureBytesPerCallInterleavedAsync_ThreeSides_SideWithAFewLuckyWindows_ReadsItsSteadyState()
    {
        const int warmup = 100;
        var lucky = new LuckyWindows(warmup, Iterations, lucky: [3, 7, 11]);
        var first = new LuckyWindows(warmup, Iterations, lucky: []);
        var third = new LuckyWindows(warmup, Iterations, lucky: []);

        var bytes = await AllocationGate.MeasureBytesPerCallInterleavedAsync(
            Iterations, warmup, first.CallValue, lucky.CallValue, third.CallValue);

        Assert.True(lucky.LuckyCalls > 0, "the lucky windows never ran");
        Assert.Equal((64, 64, 64), bytes);
    }

    [Fact]
    public async Task AssertNoMoreThanAsync_CandidateAllocatesOneSmallObjectMore_Fails()
    {
        // 24 B per call, the smallest object, is three times the default tolerance.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThanAsync(
                Iterations,
                static () =>
                {
                    Escape.Sink = new byte[40];
                    return default;
                },
                static () =>
                {
                    Escape.Sink = new byte[40];
                    Escape.Sink = new object();
                    return default;
                },
                "one object more"));

        Assert.Contains("candidate allocated", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssertNoMoreThanAsync_CandidateAllocatesMore_StillFails()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThanAsync(
                Iterations,
                static () =>
                {
                    Escape.Sink = new byte[40];
                    return default;
                },
                static () =>
                {
                    Escape.Sink = new byte[40];
                    Escape.Sink = new byte[40];
                    return default;
                },
                "twice the allocations"));

        Assert.Contains("candidate allocated", ex.Message, StringComparison.Ordinal);
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
    /// <summary>A call that allocates 40 B, except in the windows given, where it allocates nothing.</summary>
    private sealed class LuckyWindows(int warmup, int iterations, int[] lucky)
    {
        private long _calls;

        public long LuckyCalls { get; private set; }

        public ValueTask Call()
        {
            var window = (_calls++ - warmup) / iterations;
            if (_calls > warmup && Array.IndexOf(lucky, (int)window) >= 0)
            {
                LuckyCalls++;
                return default;
            }

            Escape.Sink = new byte[40];
            return default;
        }

        public async ValueTask<int> CallValue()
        {
            await Call().ConfigureAwait(false);
            return 1;
        }
    }

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
