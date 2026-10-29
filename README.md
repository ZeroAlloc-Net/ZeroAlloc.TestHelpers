# ZeroAlloc.TestHelpers

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.TestHelpers.svg)](https://www.nuget.org/packages/ZeroAlloc.TestHelpers)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/ZeroAlloc-Net/ZeroAlloc.TestHelpers/graph/badge.svg)](https://codecov.io/gh/ZeroAlloc-Net/ZeroAlloc.TestHelpers)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/blob/main/LICENSE)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/MarcelRoozekrans?style=flat&logo=githubsponsors&color=ea4aaa&label=Sponsor)](https://github.com/sponsors/MarcelRoozekrans)

Prove "zero allocation" in a unit test: exact per-call allocation budgets that fail CI when a hot path regresses, plus snapshot testing for source generators, shipped as source with no runtime dependency.

## Why ZeroAlloc.TestHelpers

- **A claim becomes a test.** `AllocationGate` runs a call as many times as you ask and fails if it allocates more bytes per call than you allow. A BenchmarkDotNet `[MemoryDiagnoser]` run shows a number; this gate fails CI when the number changes.
- **Exact counts.** Synchronous gates count with `GC.GetAllocatedBytesForCurrentThread` after a forced GC and a warmup, and round the average up, so a recurring allocation never reads as 0 B.
- **Steady state, not first runs.** Each gate measures windows of calls until two in a row agree and keeps the least, so a one-off allocation is not counted. A window that is not yet within budget does not end the gate until tiered compilation has settled, so unoptimised tier-0 code, which can box where the optimised code does not, never fails a budget. A passing gate stops after two windows. A measurement that cannot settle within 20 s throws instead of returning a figure that may be tier-0.
- **Async calls too.** The `Async` gates await every call and count allocations on every thread, for code that really yields on I/O.
- **Source-only, nothing to ship.** The helpers compile into your test project as `internal` classes: no runtime DLL, nothing on your public API, and `AllocationGate` compiles into a NativeAOT app; CI publishes one with it.

## Install

```bash
dotnet add package ZeroAlloc.TestHelpers
```

The package is a development dependency, so it stays private to the project that references it.

## Quick start

```csharp
using Xunit;
using ZeroAlloc.TestHelpers;

public class FormattingTests
{
    private readonly char[] _buffer = new char[16];

    [Fact]
    public void TryFormat_into_a_buffer_allocates_nothing() =>
        AllocationGate.AssertBudget(0, 1_000, () => 12_345.TryFormat(_buffer, out _), "TryFormat");

    [Fact]
    public void ToString_allocates_exactly_one_string() =>
        Assert.Equal(32, AllocationGate.MeasureBytesPerCall(1_000, () => 12_345.ToString()));
}
```

Put `12_345.ToString()` under the zero budget and the test fails with the exact cost:

```text
AllocationGate: ToString allocated 32000 B total over 1000 iterations (32 B/call avg, the least of 59 windows), budget is 0 B/call (0 B total).
```

## What you get

- **Budgets and measurements** for `Action` and `ValueTask<T>` calls: `AssertBudget`, `MeasureBytesPerCall` and their `ValueTask` variants. [AllocationGate](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/blob/main/docs/allocation-gate.md)
- **Relative gates**: `AssertNoMoreThan` fails when a candidate path allocates more than a baseline measured in the same run.
- **Async gates** with a documented method and tolerance, for calls that complete on another thread. [Calls that complete asynchronously](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/blob/main/docs/allocation-gate.md#calls-that-complete-asynchronously)
- **`TextSnapshot`** for string snapshots, with no Roslyn reference. [Snapshot testing](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/blob/main/docs/snapshot-testing.md)
- **`GeneratorSnapshot`** for source-generator output, reading and writing the Verify.SourceGenerators file format, so existing `*.verified.cs` files keep working. It is compiled only into projects that reference Roslyn. [Installation and packaging](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/blob/main/docs/installation.md)

## Learn more

- Documentation: [docs/](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/tree/main/docs)
- Related packages: [ZeroAlloc.Analyzers](https://github.com/ZeroAlloc-Net/ZeroAlloc.Analyzers) finds allocations at compile time; this package proves they are gone. The [ZeroAlloc libraries](https://github.com/ZeroAlloc-Net) gate their hot paths with it
- Contributing: issues and pull requests are welcome on [GitHub](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/issues)
- License: [MIT](https://github.com/ZeroAlloc-Net/ZeroAlloc.TestHelpers/blob/main/LICENSE)
