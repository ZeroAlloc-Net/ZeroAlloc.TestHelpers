using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// The asynchronous gates (#65) count with the process-wide allocation counter, so they see work
/// that resumes on another thread. Every call here completes asynchronously on a dedicated
/// worker thread, which is exactly what the thread-local synchronous gates cannot measure.
/// </summary>
/// <remarks>
/// Not parallel with the other tests: the process-wide counter also counts what concurrently
/// running tests allocate.
/// </remarks>
[Collection(ProcessWideCollection.Name)]
public sealed class AsyncAllocationGateTests
{
    private const int Iterations = 1_000;

    [Fact]
    public async Task MeasureBytesPerCallAsync_AllocationFreeAsyncCall_ReturnsZero()
    {
        using var worker = new WorkerCompletion(allocateBytes: 0);

        var perCall = await AllocationGate.MeasureBytesPerCallAsync(Iterations, worker.Next);

        Assert.Equal(0, perCall);
        Assert.True(worker.CompletedOnWorker > 0, "no call completed asynchronously on the worker");
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_AllocationOnAnotherThread_IsCounted()
    {
        // The worker allocates the array, not the caller's thread, so a thread-local counter
        // would read 0 here.
        using var worker = new WorkerCompletion(allocateBytes: 1024);

        var perCall = await AllocationGate.MeasureBytesPerCallAsync(Iterations, worker.Next);

        // The array's payload plus its object header.
        Assert.InRange(perCall, 1024, 1024 + 64);
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_Generic_AllocationOnAnotherThread_IsCounted()
    {
        using var worker = new WorkerCompletion(allocateBytes: 1024);

        var perCall = await AllocationGate.MeasureBytesPerCallAsync(Iterations, worker.NextValue);

        Assert.InRange(perCall, 1024, 1024 + 64);
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_Generic_AllocationFreeAsyncCall_ReturnsZero()
    {
        using var worker = new WorkerCompletion(allocateBytes: 0);

        var perCall = await AllocationGate.MeasureBytesPerCallAsync(Iterations, worker.NextValue);

        Assert.Equal(0, perCall);
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_AsyncStateMachine_CountsItsBox()
    {
        // An async method that really yields boxes its state machine on every call. That is the
        // cost the synchronous gates refuse to measure, and it must be visible here.
        var perCall = await AllocationGate.MeasureBytesPerCallAsync(100, static async () =>
            await Task.Yield());

        Assert.True(perCall > 0, $"expected the state machine box to be counted, got {perCall} B/call");
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_RunsWarmupThenThreeWindows()
    {
        long calls = 0;
        _ = await AllocationGate.MeasureBytesPerCallAsync(10, 7, () =>
        {
            calls++;
            return default;
        });

        Assert.Equal(7 + (3 * 10), calls);
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_DefaultWarmup_IsDefaultAsyncWarmup()
    {
        long calls = 0;
        _ = await AllocationGate.MeasureBytesPerCallAsync(10, () =>
        {
            calls++;
            return default;
        });

        Assert.Equal(AllocationGate.DefaultAsyncWarmup + (3 * 10), calls);
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_ValidatesArgumentsBeforeRunning()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AllocationGate.MeasureBytesPerCallAsync(10, (Func<ValueTask>)null!));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            AllocationGate.MeasureBytesPerCallAsync(0, static () => default));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            AllocationGate.MeasureBytesPerCallAsync(10, -1, static () => default));
    }

    [Fact]
    public async Task MeasureBytesPerCallAsync_PropagatesTheActionsException()
    {
        _ = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
            AllocationGate.MeasureBytesPerCallAsync(10, static async () =>
            {
                await Task.Yield();
                throw new InvalidTimeZoneException();
            }));
    }

    [Fact]
    public async Task AssertBudgetAsync_AllocationFreeAsyncCall_DoesNotThrow()
    {
        using var worker = new WorkerCompletion(allocateBytes: 0);

        await AllocationGate.AssertBudgetAsync(0, Iterations, worker.Next, "zero-alloc async call");
        await AllocationGate.AssertBudgetAsync(0, Iterations, worker.NextValue, "zero-alloc async call");
    }

    [Fact]
    public async Task AssertBudgetAsync_OverBudget_ThrowsWithDiagnosticMessage()
    {
        using var worker = new WorkerCompletion(allocateBytes: 1024);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertBudgetAsync(0, Iterations, worker.Next, "worker allocator"));

        Assert.Contains("worker allocator", ex.Message, StringComparison.Ordinal);
        Assert.Contains("budget is 0 B/call", ex.Message, StringComparison.Ordinal);
        Assert.Contains($"tolerance of {AllocationGate.DefaultAsyncToleranceBytesPerCall} B/call", ex.Message, StringComparison.Ordinal);
        Assert.Contains("MemoryDiagnoser", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssertBudgetAsync_Generic_OverBudget_Throws()
    {
        using var worker = new WorkerCompletion(allocateBytes: 1024);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertBudgetAsync(0, Iterations, worker.NextValue, "worker allocator"));
    }

    [Fact]
    public async Task AssertBudgetAsync_WithinTolerance_DoesNotThrow()
    {
        // 1 KiB once every hundred calls is about 11 B/call: over a 0 B budget, within 16 B/call.
        using var worker = new WorkerCompletion(allocateBytes: 1024, everyNthCall: 100);

        await AllocationGate.AssertBudgetAsync(0, 16, Iterations, 100, worker.Next, "occasional allocator");
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertBudgetAsync(0, 0, Iterations, 100, worker.Next, "occasional allocator"));
    }

    [Fact]
    public async Task AssertNoMoreThanAsync_CandidateAllocatesTheSame_DoesNotThrow()
    {
        using var baseline = new WorkerCompletion(allocateBytes: 256);
        using var candidate = new WorkerCompletion(allocateBytes: 256);

        await AllocationGate.AssertNoMoreThanAsync(Iterations, baseline.Next, candidate.Next, "same allocation");
        await AllocationGate.AssertNoMoreThanAsync(Iterations, baseline.NextValue, candidate.NextValue, "same allocation");
    }

    [Fact]
    public async Task AssertNoMoreThanAsync_CandidateAllocatesMore_ThrowsWithBothFigures()
    {
        using var baseline = new WorkerCompletion(allocateBytes: 0);
        using var candidate = new WorkerCompletion(allocateBytes: 256);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThanAsync(Iterations, baseline.Next, candidate.Next, "DI-resolved vs hand-built"));

        Assert.Contains("DI-resolved vs hand-built", ex.Message, StringComparison.Ordinal);
        Assert.Contains("baseline allocated 0 B total", ex.Message, StringComparison.Ordinal);
        Assert.Contains("MemoryDiagnoser", ex.Message, StringComparison.Ordinal);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AllocationGate.AssertNoMoreThanAsync(Iterations, 0, 100, baseline.NextValue, candidate.NextValue, "generic"));
    }

    /// <summary>
    /// A call that always completes asynchronously, on a dedicated worker thread, and allocates
    /// nothing itself. <c>allocateBytes</c> makes the worker allocate an array before it completes
    /// the call, so the allocation happens on a thread the caller never runs on.
    /// </summary>
    private sealed class WorkerCompletion : IValueTaskSource, IValueTaskSource<int>, IDisposable
    {
        private readonly int _allocateBytes;
        private readonly int _everyNthCall;
        private readonly AutoResetEvent _requested = new(initialState: false);
        private readonly Thread _thread;
        private ManualResetValueTaskSourceCore<int> _core;
        private volatile bool _stopping;
        private long _calls;

        public WorkerCompletion(int allocateBytes, int everyNthCall = 1)
        {
            _allocateBytes = allocateBytes;
            _everyNthCall = everyNthCall;

            // Continuations run on the thread pool, as after real I/O, never inline on the worker:
            // a test whose code ran on the worker would deadlock joining it in Dispose.
            _core.RunContinuationsAsynchronously = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "async gate worker" };
            _thread.Start();
        }

        public long CompletedOnWorker { get; private set; }

        public static byte[]? Sink { get; private set; }

        public ValueTask Next()
        {
            _core.Reset();
            _requested.Set();
            return new ValueTask(this, _core.Version);
        }

        public ValueTask<int> NextValue()
        {
            _core.Reset();
            _requested.Set();
            return new ValueTask<int>(this, _core.Version);
        }

        public void Dispose()
        {
            _stopping = true;
            _requested.Set();
            _thread.Join();
            _requested.Dispose();
        }

        private void Run()
        {
            while (true)
            {
                _requested.WaitOne();
                if (_stopping)
                    return;

                if (_allocateBytes > 0 && ++_calls % _everyNthCall == 0)
                    Sink = new byte[_allocateBytes];

                CompletedOnWorker++;
                _core.SetResult(42);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(
            Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _core.OnCompleted(continuation, state, token, flags);

        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);

        int IValueTaskSource<int>.GetResult(short token) => _core.GetResult(token);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessWideCollection
{
    public const string Name = "process-wide-allocation";
}
