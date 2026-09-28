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
}
