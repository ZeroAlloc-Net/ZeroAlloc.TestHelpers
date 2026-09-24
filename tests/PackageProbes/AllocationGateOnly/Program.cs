using ZeroAlloc.TestHelpers;

AllocationGate.AssertBudget(0, 1_000, static () => { }, "no-op");
AllocationGate.AssertBudgetValueTask(0, 1_000, static () => new ValueTask<int>(42), "synchronous ValueTask");

Console.WriteLine("AllocationGate probe passed");
