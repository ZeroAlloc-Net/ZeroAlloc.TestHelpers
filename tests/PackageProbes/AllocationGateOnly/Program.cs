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

Console.WriteLine("AllocationGate probe passed");
