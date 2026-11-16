# AGENTS.md: ZeroAlloc.TestHelpers

A source-only NuGet package of test helpers: `AllocationGate` (exact per-call allocation budgets),
`TextSnapshot` and `GeneratorSnapshot` (snapshot tests, the latter for source-generator output). It is
what the other ZeroAlloc repos use to prove their "0 B" claims, so a change here changes how every
consumer's gate measures.

## Layout

- `src/ZeroAlloc.TestHelpers` builds no shipped assembly: `IncludeBuildOutput` is false. The three `.cs`
  files are packed as `contentFiles` and compile into the consumer's project as `internal` classes in
  namespace `ZeroAlloc.TestHelpers`. The project compiles them only to validate them. It is an
  exception to the org rule on `IsAotCompatible`: the project sets it to `false` because nothing
  runs from it. The shipped source itself must stay NativeAOT-safe.
- `[CallerFilePath]` in the snapshot helpers must resolve to the consumer's test file, which is why the
  helpers ship as source and never as a DLL. Do not move them into a compiled assembly.
- `build/` and `buildTransitive/*.targets` ship in the package. They drop `GeneratorSnapshot.cs` from
  consumers with no `Microsoft.CodeAnalysis.dll` reference, and drop `Snapshots/**/*.verified.cs` and
  `*.received.cs` from the compile. Overrides: `ZeroAllocTestHelpersIncludeGeneratorSnapshot` and
  `ZeroAllocTestHelpersCompileSnapshotFiles`.
- `tests/ZeroAlloc.TestHelpers.Tests` is the unit suite. `tests/PackageProbes` is not in the solution:
  it builds three consumer projects against the packed nupkg, which is the only test of the
  contentFiles and the `.targets`.
- `tests/**/Snapshots/*.verified.*` are data, not code. The helpers write into them during tests.

## What consumers rely on (AllocationGate contract)

Every ZeroAlloc repo gates its hot path with `AllocationGate`, so treat its surface as public API even
though the class is `internal` and `PublicAPI.Shipped.txt` stays empty:

- Namespace `ZeroAlloc.TestHelpers`, class `AllocationGate`, and the existing method names and
  signatures (`AssertBudget`, `MeasureBytesPerCall`, `AssertNoMoreThan`, their `ValueTask` and `Async`
  variants, `AssertBudgetAsync`, `MeasureBytesPerCallInterleavedAsync`). Add overloads; never add an
  optional parameter to an existing one.
- Budgets are bytes per call, rounded up, so a recurring allocation never reads as 0 B. A gate measures
  after a forced GC and a warmup, until two windows agree and tiered compilation has settled, and
  keeps the least window. A measurement that cannot settle within the timeout throws rather than
  return a figure. Do not weaken any of this: it is what makes a passing 0 B gate trustworthy.
- The failure message names label, bytes per call, windows and the budget. Consumers read it in CI logs.
- `AllocationGate.cs` and `TextSnapshot.cs` need only the BCL and must compile in a project with no
  Roslyn reference and publish under NativeAOT. Only `GeneratorSnapshot.cs` may use Roslyn.

## Build and test

Prerequisites: SDK 11.0.100 (`global.json`, `rollForward: latestMinor`) plus the 10 SDK for CI parity;
runtimes 10 and 11, because the test project targets `net10.0;net11.0` and the `AllocationGateOnly`
probe targets `net11.0`.

CI job `build` in `.github/workflows/ci.yml`:

```bash
dotnet restore
dotnet build --configuration Release --no-restore -p:Version=0.0.0-ci.1
dotnet test --configuration Release --no-build --verbosity normal --collect "XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
dotnet pack src/ZeroAlloc.TestHelpers/ZeroAlloc.TestHelpers.csproj --configuration Release --no-build --output ./artifacts -p:Version=0.0.0-ci.1
bash tests/PackageProbes/run.sh --aot
```

- The version goes to the build, not only to pack: pack runs `--no-build` and the version guard in
  `Directory.Build.props` fails with `ZAVERSION001` or `ZAVERSION002` otherwise. Build and test alone
  need no `-p:Version`.
- The `Coverage gate` step runs the shared `MarcelRoozekrans/ci-baseline/actions/coverage-gate` action on
  `TestResults/**/coverage.cobertura.xml`. The commands above produce the reports but do not reproduce
  the floor check. `codecov.yml` fails a project coverage drop beyond 1%.
- `--aot` needs a native AOT toolchain; without it `bash tests/PackageProbes/run.sh` runs every other
  probe check. Probes pack into `tests/PackageProbes/obj`.
- Other CI jobs: `lint-commits` (commitlint, pull requests only), `api-compat` (shared workflow, runs on
  SDKs 10 and 11), `baseline-checks.yml` (gitleaks, CodeQL, NuGetAudit).

Fast inner loop, no CI job runs these:

```bash
dotnet test tests/ZeroAlloc.TestHelpers.Tests -f net10.0 --configuration Release --filter "FullyQualifiedName~TextSnapshotTests"
```

The full suite takes about a minute on `net11.0` because the gates wait for tiering to settle.

## Conventions and gotchas

- Warnings are errors, and `RS0016` and `RS0017` too. `LangVersion` is `latest`, `Nullable` is enabled.
- Package versions live in `Directory.Packages.props` (central management). The `TestHelpers` package is a
  `DevelopmentDependency` with `SuppressDependenciesWhenPacking`: consumers get no dependency edge.
- The snapshot tests write and delete files in the shared `Snapshots` folder. `TestTfmsInParallel` is
  false and `SnapshotCollection` serializes tests in one process; keep new snapshot tests in that
  collection and do not run both target frameworks in parallel.
- A change to `build/*.targets` or the content-file wiring needs a probe in `tests/PackageProbes`
  (with an `expect_failure` for the opposite setting), not only a unit test.
- Gate measurements depend on tiered JIT. `TieringNoise.cs` and `StableMeasurementTests.cs` simulate the noise
  of a large test run; extend them when you change the stability or settling logic.

<!-- org-core:start sha256=8faf417fa86f11bd8ad9bc98dfa4683fba44e440291e84c6cb9b0ec3ca0c0e7e (generated from docker-nuc scripts/agents-md/org-core-zeroalloc.md; do not edit here) -->
## Org rules

These rules hold in every ZeroAlloc-Net repo. Each library makes two promises: it works under
NativeAOT and trimming, and its hot path allocates nothing. The 2026-10-11 sweep found 16 of 28 repos
allocating on the hot path, most with docs claiming 0 B.

### Hot path

The operation a consumer runs repeatedly: `Send`, a cache hit, `Validate`, `Map`, `Serialize`, a
resilience call, a state transition, an append, a worker poll, including the idle poll. Startup and
registration are not hot path. For a source generator, the hot path is the code it emits: judge the
snapshots and the generated output.

### Allocation traps the sweep found

| Pattern | Instead |
|---|---|
| `async ValueTask` that only awaits and returns | `return new ValueTask<T>(inner.XAsync(ct));` when the method only forwards a `Task<T>` (return a forwarded `ValueTask<T>` directly). The wrapper stores a synchronous throw in the returned task; the direct return lets it, and the cancellation status of the callee, reach the caller. Keep the `async` wrapper when callers depend on either |
| `Task.WhenAll` over tasks that already completed | skip the await only when every task has `IsCompletedSuccessfully` (`Status == TaskStatus.RanToCompletion` on `netstandard2.0`); otherwise `await Task.WhenAll`, which also reports faults of completed tasks |
| `TagList` or log `params object[]` built before `Enabled`/`IsEnabled` | check first, or `LoggerMessage.Define` |
| event args allocated before checking for subscribers | copy the handler to a local, check, then allocate |
| `new Serializer()` / `new Utf8JsonWriter()` per call | cache stateless instances, reuse writers with `Reset` |
| `GetString()` / `ToString()` round-trips in JSON converters | parse and format through spans |
| `ToArray()` / `ToList()` copies between layers | pass `ReadOnlyMemory<T>` or spans, copy once at most |
| LINQ in emitted code | emit `for` loops |
| `yield` iterator behind a public `GetEnumerator` | public struct enumerator, from the first release |
| `Enum.IsDefined(typeof(E), v)` with a boxed value, `decimal.GetBits(v)` to read the scale | `Enum.IsDefined<E>(v)` when `v` has type `E` (.NET 5 or later, so not on .NET Framework, `netstandard2.0`, `netstandard2.1` or .NET Core 3.1); `v.Scale` for a scale read only (.NET 7 or later, never for the coefficient or sign). Keep the old call on older targets |
| configuration parsed or read per call | parse once and cache |
| an `AsyncLocal` write per call | pass state explicitly |
| a capturing lambda or method-group delegate per call | cache the delegate in a field, or a static lambda |
| a per-key object that is never removed | remove on release; otherwise it is a leak |

Inherent allocations are fine when documented: the consumer's own result, an exception on a failure
path, a one-time cache fill.

### Tests and benchmarks

- Every hot path has an allocation gate test (`AllocationGate` from ZeroAlloc.TestHelpers) that measures
  the whole public call, including any state setup.
- Budgets are exact. If a path allocates, assert that number and comment what it is.
- Test the generated code, not a hand-written stand-in, and cover every shape: each attribute, nested
  collections, and the asynchronous case with a fake that returns pending tasks.
- Benchmarks run in CI through the shared `benchmark-smoke.yml` and target the library's LTS
  frameworks. The benchmark body allocates nothing besides the library itself. Run locally with
  `--inProcess`.
- Publish exact allocations and same-run ratios against a `Baseline`, never absolute timings. Run with
  `--launchCount 3`, pin CPUs with `taskset`, give the spread, and label every table.

### NativeAOT and trimming

- Packable runtime projects, and projects consumers publish, set `IsAotCompatible=true` on modern
  target frameworks. Analyzers and source generators target `netstandard2.0`.
- No reflection or runtime code generation on a supported path. An opt-in reflection fallback carries
  honest `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`. Every suppression must be true;
  remove dead ones.
- The AOT smoke sample publishes with `TreatWarningsAsErrors=true`. If a third-party warning really
  blocks it, use a narrow, justified suppression for that warning. `WarningsNotAsErrors` selects codes,
  not assemblies, so it would also downgrade the same warning from generated code.
- The smoke runs the hot path of every package in the repo. A package without a smoke has unknown AOT
  status. Adapters run against real services in CI service containers; publish-only is a last resort.
  App and template smokes make authenticated requests.

### Docs and shipping

- Docs match measurements. After a hot-path change, re-measure and update `docs/performance.md` and the
  README. Never write "0 B" without a test that asserts it.
- New public API is `feat:`, even for a performance change. Public API changes are additive: never add
  an optional parameter to a shipped signature. A breaking end state goes in a `next-major` issue.
- A behaviour change gets a `## Behaviour change` section in the PR body.
- Conventional Commits with release-please. Subjects start lowercase, body lines stay within 100
  characters, and a body never contains nested parentheses: release-please drops the whole commit.
- Local packs use `9.9.9-local.<sha>`. After a release, verify the package on NuGet.
- Fix review findings in the PR; file an issue only for what cannot go in it.

Full rules, template wiring lessons and the reasons behind each rule: https://github.com/ZeroAlloc-Net/.github/blob/main/AGENTS.md
<!-- org-core:end -->
