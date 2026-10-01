using System;
using System.Threading.Tasks;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

public sealed class AllocationGateTests
{
    [Fact]
    public void AssertBudget_NoAllocation_DoesNotThrow()
    {
        long counter = 0;
        AllocationGate.AssertBudget(
            budgetBytes: 0,
            iterations: 100,
            action: () => { counter++; },
            label: "zero-alloc counter increment");

        Assert.Equal(102, counter); // 2 warmup + 100 measured
    }

    [Fact]
    public void AssertBudget_OverBudget_ThrowsWithDiagnosticMessage()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudget(
                budgetBytes: 0,
                iterations: 10,
                action: () => _ = new byte[1024], // 1 KiB per call
                label: "deliberate allocator"));

        Assert.Contains("deliberate allocator", ex.Message, StringComparison.Ordinal);
        Assert.Contains("budget is 0 B/call", ex.Message, StringComparison.Ordinal);
        Assert.Contains("MemoryDiagnoser", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertBudgetValueTask_SyncCompleted_NoAllocation_DoesNotThrow()
    {
        long counter = 0;
        AllocationGate.AssertBudgetValueTask(
            budgetBytes: 0,
            iterations: 100,
            action: () => { counter++; return new ValueTask<int>(42); },
            label: "zero-alloc sync-completed ValueTask");

        Assert.Equal(102, counter);
    }

    [Fact]
    public void AssertBudgetValueTask_AsyncCompletion_ThrowsSyncCompletionRequired()
    {
        // A ValueTask backed by a TaskCompletionSource whose Task is never completed is
        // deterministically incomplete: IsCompletedSuccessfully is guaranteed false on the
        // very first check, unlike `await Task.Yield()`, whose continuation can race ahead
        // of the check and complete the ValueTask synchronously from the gate's viewpoint.
        static ValueTask<int> AsyncBody() =>
            new(new TaskCompletionSource<int>().Task);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudgetValueTask<int>(
                budgetBytes: 1024,
                iterations: 10,
                action: AsyncBody,
                label: "async ValueTask"));

        Assert.Contains("sync-completion-required", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MeasureBytesPerCall_NoAllocation_ReturnsZero()
    {
        long counter = 0;
        var perCall = AllocationGate.MeasureBytesPerCall(100, () => { counter++; });

        Assert.Equal(0, perCall);
        Assert.Equal(102, counter); // 2 warmup + 100 measured
    }

    [Fact]
    public void MeasureBytesPerCall_Allocating_ReturnsPerCallBytes()
    {
        var perCall = AllocationGate.MeasureBytesPerCall(10, () => _ = new byte[1024]);

        // The array's payload plus its object header.
        Assert.InRange(perCall, 1024, 1024 + 64);
    }

    [Fact]
    public void MeasureBytesPerCall_FractionalAllocation_RoundsUp()
    {
        // One small array every hundred calls is well under one byte per call on average,
        // which integer division would report as zero.
        long counter = 0;
        var perCall = AllocationGate.MeasureBytesPerCall(1000, () =>
        {
            if (++counter % 100 == 0)
                _ = new byte[16];
        });

        Assert.Equal(1, perCall);
    }

    [Fact]
    public void MeasureBytesPerCallValueTask_SyncCompleted_ReturnsPerCallBytes()
    {
        var perCall = AllocationGate.MeasureBytesPerCallValueTask(
            10, () => new ValueTask<byte[]>(new byte[1024]));

        Assert.InRange(perCall, 1024, 1024 + 64);
    }

    [Fact]
    public void MeasureBytesPerCallValueTask_AsyncCompletion_ThrowsSyncCompletionRequired()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.MeasureBytesPerCallValueTask<int>(
                10, () => new(new TaskCompletionSource<int>().Task)));

        Assert.Contains("sync-completion-required", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertBudget_OverBudget_ReportsRoundedUpPerCallAverage()
    {
        long counter = 0;
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertBudget(
                budgetBytes: 0,
                iterations: 1000,
                action: () =>
                {
                    if (++counter % 100 == 0)
                        _ = new byte[16];
                },
                label: "occasional allocator"));

        Assert.Contains("~1 B/call avg", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertNoMoreThan_CandidateAllocatesTheSame_DoesNotThrow()
    {
        AllocationGate.AssertNoMoreThan(
            iterations: 10,
            baseline: () => _ = new byte[256],
            candidate: () => _ = new byte[256],
            label: "same allocation");
    }

    [Fact]
    public void AssertNoMoreThan_CandidateAllocatesMore_ThrowsWithBothFigures()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThan(
                iterations: 10,
                baseline: () => { },
                candidate: () => _ = new byte[256],
                label: "DI-resolved vs hand-built"));

        Assert.Contains("DI-resolved vs hand-built", ex.Message, StringComparison.Ordinal);
        Assert.Contains("baseline allocated 0 B total", ex.Message, StringComparison.Ordinal);
        Assert.Contains("MemoryDiagnoser", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertNoMoreThanValueTask_CandidateAllocatesMore_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThanValueTask(
                iterations: 10,
                baseline: () => new ValueTask<int>(1),
                candidate: () => new ValueTask<int>(new byte[256].Length),
                label: "ValueTask paths"));

        Assert.Contains("ValueTask paths", ex.Message, StringComparison.Ordinal);
    }
}
