namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// Somewhere for a test's deliberate allocations to escape to. Since .NET 10 the JIT can put an
/// array or object that never leaves its method on the stack, and the gates warm up long enough
/// to run that optimized code, so an allocation that is only discarded can measure 0 B.
/// </summary>
internal static class Escape
{
    public static object? Sink;
}
