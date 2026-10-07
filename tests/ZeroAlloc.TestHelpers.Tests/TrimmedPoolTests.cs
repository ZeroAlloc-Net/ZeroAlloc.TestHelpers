using System;
using System.Threading.Tasks;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// The gate does not count the refill of a pool that its own forced GC trimmed (#62).
/// <see cref="TrimmedPool"/> is emptied by a finalizer that runs after every GC, which is how
/// <c>ArrayPool.Shared</c> trims through <c>Gen2GcCallback</c>; under high memory load that trim
/// drops every pooled array. A call that rents from a warm pool allocates nothing, so each gate
/// must measure 0.
/// </summary>
/// <remarks>
/// Not parallel with the other tests: the trim runs on every GC in the process, and its static
/// pool must not be refilled or emptied by another test's collections while one measures.
/// </remarks>
[Collection(TrimmedPoolCollection.Name)]
public sealed class TrimmedPoolTests
{
    [Fact]
    public void AssertBudget_DoesNotCountTheRefillOfATrimmedPool()
    {
        using var pool = TrimmedPool.Start();

        AllocationGate.AssertBudget(0, 100, static () => TrimmedPool.Use(), "rent from a warm pool");
    }

    [Fact]
    public void AssertBudgetValueTask_DoesNotCountTheRefillOfATrimmedPool()
    {
        using var pool = TrimmedPool.Start();

        AllocationGate.AssertBudgetValueTask(
            0, 100, static () => new ValueTask<int>(TrimmedPool.Use()), "rent from a warm pool");
    }

    [Fact]
    public void MeasureBytesPerCall_DoesNotCountTheRefillOfATrimmedPool()
    {
        using var pool = TrimmedPool.Start();

        Assert.Equal(0, AllocationGate.MeasureBytesPerCall(100, static () => TrimmedPool.Use()));
    }

    [Fact]
    public void AssertNoMoreThan_DoesNotCountTheRefillOfATrimmedPool()
    {
        using var pool = TrimmedPool.Start();

        // The candidate allocates nothing once warm, so it allocates no more than an empty call.
        AllocationGate.AssertNoMoreThan(
            100, static () => { }, static () => TrimmedPool.Use(), "rent from a warm pool");
    }

    /// <summary>
    /// One pooled buffer, and a trim that empties it from a finalizer after every GC. The trim
    /// keeps itself alive by registering for finalization again, as <c>Gen2GcCallback</c> does.
    /// </summary>
    private sealed class TrimmedPool : IDisposable
    {
        private static byte[]? s_buffer;
        private static volatile bool s_trimming;

        public static TrimmedPool Start()
        {
            s_buffer = null;
            s_trimming = true;
            _ = new Trim();
            return new TrimmedPool();
        }

        // Rents the buffer, allocating it only when the pool is empty, and returns it.
        public static int Use()
        {
            var buffer = s_buffer ?? new byte[4096];
            s_buffer = buffer;
            return buffer.Length;
        }

        public void Dispose()
        {
            s_trimming = false;
            s_buffer = null;
        }

        private sealed class Trim
        {
            ~Trim()
            {
                if (!s_trimming)
                    return;

                s_buffer = null;
                GC.ReRegisterForFinalize(this);
            }
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TrimmedPoolCollection
{
    public const string Name = "trimmed-pool";
}
