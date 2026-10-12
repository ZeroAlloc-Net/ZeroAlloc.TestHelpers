using System;
using System.Threading.Tasks;
using ZeroAlloc.TestHelpers;

AllocationGate.AssertBudget(0, 1_000, static () => { }, "no-op");
AllocationGate.AssertBudgetValueTask(0, 1_000, static () => new ValueTask<int>(42), "synchronous ValueTask");
AllocationGate.AssertNoMoreThan(1_000, static () => { }, static () => { }, "no-op vs no-op");
AllocationGate.AssertNoMoreThanValueTask(
    1_000, static () => new ValueTask<int>(1), static () => new ValueTask<int>(2), "ValueTask vs ValueTask");

if (AllocationGate.MeasureBytesPerCall(1_000, static () => { }) != 0)
    throw new InvalidOperationException("MeasureBytesPerCall reported allocations for a no-op");
if (AllocationGate.MeasureBytesPerCallValueTask(1_000, static () => new ValueTask<int>(42)) != 0)
    throw new InvalidOperationException("MeasureBytesPerCallValueTask reported allocations for a synchronous ValueTask");

// The asynchronous gates (#65): process-wide counting, awaited calls.
await AllocationGate.AssertBudgetAsync(0, 1_000, static () => default, "completed ValueTask");
await AllocationGate.AssertBudgetAsync(0, 1_000, static () => new ValueTask<int>(42), "completed ValueTask<int>");
await AllocationGate.AssertNoMoreThanAsync(
    1_000, static () => new ValueTask<int>(1), static () => new ValueTask<int>(2), "ValueTask vs ValueTask");

// An async method that really yields boxes its state machine on every call, on whichever thread
// resumes it, so the process-wide counter must see it.
if (await AllocationGate.MeasureBytesPerCallAsync(1_000, static async () => await Task.Yield()) == 0)
    throw new InvalidOperationException("MeasureBytesPerCallAsync missed the state machine of a yielding call");

Console.WriteLine("AllocationGate probe passed");
