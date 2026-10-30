using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// A call path of many methods (#94). Each method boxes its argument as tier-0 code and allocates
/// nothing once optimised, so a measurement of any method still at tier 0 reads 24 B too many.
/// The runtime promotes the methods in batches, and a quiet gap between two batches, or a delay
/// that other threads keep extending, must not be mistaken for the end of tiering. Every test uses
/// its own value type, so each starts with methods that have never been called.
/// </summary>
/// <remarks>
/// Not parallel with the other tests: they would hold tiering back, or let it through, at random.
/// </remarks>
[Collection(ProcessWideCollection.Name)]
public sealed class MultiMethodCallPathTests
{
    private const int Iterations = 1_000;
    private const int Methods = 24;

    // Gaps of up to this long between one thread's new methods. Tiering gets through in the
    // gaps where no thread calls a new method for 100 ms, and the gate has to wait for those.
    private const int NoiseGapMilliseconds = 300;

    private struct Quiet1 { } private struct Quiet2 { } private struct Quiet3 { }
    private struct Noisy1 { } private struct Noisy2 { }

    [Fact] public void MeasureBytesPerCall_ManyMethods_ReadsTheOptimisedCode() => Sync<Quiet1>();

    [Fact] public Task MeasureBytesPerCallAsync_ManyMethods_ReadsTheOptimisedCode() => Async<Quiet2>();

    [Fact]
    public void AssertNoMoreThan_BaselineOfManyMethods_IsNotMeasuredAtTierZero()
    {
        // The baseline runs first and has the most to wait for; the candidate calls the same
        // methods once more. A baseline measured at tier 0 reads more than the candidate (#94).
        AllocationGate.AssertNoMoreThan(
            Iterations, static () => Chain<Quiet3>.Run(1), static () => Chain<Quiet3>.Run(2), "baseline of many methods");
    }

    [Fact]
    public void MeasureBytesPerCall_WhileOtherThreadsCallNewMethods_ReadsTheOptimisedCode()
    {
        using var noise = TieringNoise.Start(NoiseGapMilliseconds);

        Sync<Noisy1>();
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_WhileOtherThreadsCallNewMethods_ReadsTheOptimisedCode()
    {
        using var noise = TieringNoise.Start(NoiseGapMilliseconds);

        await Async<Noisy2>();
    }

    private static void Sync<T>() where T : struct
    {
        var perCall = AllocationGate.MeasureBytesPerCall(Iterations, static () => Chain<T>.Run(1));
        Assert.True(perCall == 0, $"measured {perCall} B/call, tier-0 boxing is {Methods * 24} B/call");
    }

    private static async Task Async<T>() where T : struct
    {
        var perCall = await AllocationGate.MeasureBytesPerCallAsync(Iterations, static () => new ValueTask<int>(Chain<T>.Run(1)));
        Assert.True(perCall == 0, $"measured {perCall} B/call, tier-0 boxing is {Methods * 24} B/call");
    }

    private static class Chain<T> where T : struct
    {
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M0(int x) { object o = x; return o.GetHashCode() + M1(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M1(int x) { object o = x; return o.GetHashCode() + M2(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M2(int x) { object o = x; return o.GetHashCode() + M3(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M3(int x) { object o = x; return o.GetHashCode() + M4(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M4(int x) { object o = x; return o.GetHashCode() + M5(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M5(int x) { object o = x; return o.GetHashCode() + M6(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M6(int x) { object o = x; return o.GetHashCode() + M7(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M7(int x) { object o = x; return o.GetHashCode() + M8(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M8(int x) { object o = x; return o.GetHashCode() + M9(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M9(int x) { object o = x; return o.GetHashCode() + M10(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M10(int x) { object o = x; return o.GetHashCode() + M11(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M11(int x) { object o = x; return o.GetHashCode() + M12(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M12(int x) { object o = x; return o.GetHashCode() + M13(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M13(int x) { object o = x; return o.GetHashCode() + M14(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M14(int x) { object o = x; return o.GetHashCode() + M15(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M15(int x) { object o = x; return o.GetHashCode() + M16(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M16(int x) { object o = x; return o.GetHashCode() + M17(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M17(int x) { object o = x; return o.GetHashCode() + M18(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M18(int x) { object o = x; return o.GetHashCode() + M19(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M19(int x) { object o = x; return o.GetHashCode() + M20(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M20(int x) { object o = x; return o.GetHashCode() + M21(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M21(int x) { object o = x; return o.GetHashCode() + M22(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M22(int x) { object o = x; return o.GetHashCode() + M23(x); }
        [MethodImpl(MethodImplOptions.NoInlining)] private static int M23(int x) { object o = x; return o.GetHashCode(); }
        public static int Run(int x) => M0(x);
    }
}
