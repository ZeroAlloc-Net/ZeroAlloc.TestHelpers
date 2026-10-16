using System;
using System.Threading.Tasks;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// The synchronous gates measure what a call allocates in steady state (#84). A one-off
/// allocation inside the measured calls, such as a type initializer, a cache fill or a lazily
/// created fixture, must not fail an exact budget. An allocation on every call must, by as little
/// as one byte.
/// </summary>
public sealed class SteadyStateAllocationGateTests
{
    private const int Iterations = 4_000;

    // Where the one-off lands, counted in calls after the warmup: the first measured call, the
    // middle of the first window, and the middle of the whole measured run.
    public static TheoryData<int> OneOffPositions => new() { 0, Iterations / 2, Iterations + (Iterations / 2) };

    [Theory]
    [MemberData(nameof(OneOffPositions))]
    public void AssertBudget_OneOffAllocation_PassesAZeroBudget(int afterWarmup)
    {
        var oneOff = new OneOff(AllocationGate.DefaultAsyncWarmup + afterWarmup);

        AllocationGate.AssertBudget(0, Iterations, oneOff.Call, "one-off allocator");

        Assert.True(oneOff.Allocated, "the one-off allocation never ran");
    }

    [Theory]
    [MemberData(nameof(OneOffPositions))]
    public void AssertBudgetValueTask_OneOffAllocation_PassesAZeroBudget(int afterWarmup)
    {
        var oneOff = new OneOff(AllocationGate.DefaultAsyncWarmup + afterWarmup);

        AllocationGate.AssertBudgetValueTask(
            0, Iterations, () => { oneOff.Call(); return new ValueTask<int>(1); }, "one-off allocator");

        Assert.True(oneOff.Allocated, "the one-off allocation never ran");
    }

    [Theory]
    [MemberData(nameof(OneOffPositions))]
    public void MeasureBytesPerCall_OneOffAllocation_IsNotCounted(int afterWarmup)
    {
        // The case from ZeroAlloc.Scheduling: a call that allocates the same every time, plus one
        // stray allocation, must not read one byte more than the call alone.
        static void Steady() => Escape.Sink = new byte[32];
        var steady = AllocationGate.MeasureBytesPerCall(Iterations, Steady);

        var oneOff = new OneOff(AllocationGate.DefaultAsyncWarmup + afterWarmup);
        var withOneOff = AllocationGate.MeasureBytesPerCall(Iterations, () =>
        {
            Steady();
            oneOff.Call();
        });

        Assert.True(oneOff.Allocated, "the one-off allocation never ran");
        Assert.Equal(steady, withOneOff);
    }

    [Theory]
    [MemberData(nameof(OneOffPositions))]
    public void AssertNoMoreThan_OneOffAllocationInTheCandidate_DoesNotThrow(int afterWarmup)
    {
        var oneOff = new OneOff(AllocationGate.DefaultAsyncWarmup + afterWarmup);

        AllocationGate.AssertNoMoreThan(Iterations, static () => { }, oneOff.Call, "one-off candidate");

        Assert.True(oneOff.Allocated, "the one-off allocation never ran");
    }

    [Fact]
    public void AssertBudget_SteadyAllocation_FailsOneByteUnderAndPassesExactly()
    {
        static void Allocate() => Escape.Sink = new byte[40];
        var perCall = (int)AllocationGate.MeasureBytesPerCall(Iterations, Allocate);
        Assert.True(perCall > 40, $"expected the array payload plus its header, got {perCall} B/call");

        AllocationGate.AssertBudget(perCall, Iterations, Allocate, "steady allocator");
        _ = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudget(perCall - 1, Iterations, Allocate, "steady allocator"));
    }

    [Fact]
    public void AssertBudget_AllocationEveryHundredCalls_StillFailsAZeroBudget()
    {
        // Lands in every window, so the least window still holds it.
        long calls = 0;
        _ = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudget(0, Iterations, () =>
            {
                if (++calls % 100 == 0)
                    Escape.Sink = new byte[16];
            }, "occasional allocator"));
    }

    [Fact]
    public void AssertNoMoreThan_CandidateAllocatesOneObjectMorePerCall_Throws()
    {
        _ = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThan(
                Iterations,
                static () => Escape.Sink = new byte[32],
                static () => { Escape.Sink = new byte[32]; Escape.Sink = new object(); },
                "one object more"));
    }

    /// <summary>A call that allocates 1 KiB on exactly one call, the given one, and nothing on any other.</summary>
    private sealed class OneOff(int callIndex)
    {
        private int _calls;
        private byte[]? _kept;

        public bool Allocated => _kept is not null;

        public void Call()
        {
            if (_calls++ == callIndex)
                _kept = new byte[1024];
        }
    }
}
