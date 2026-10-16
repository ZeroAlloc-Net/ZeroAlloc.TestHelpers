# Installation and packaging

## Package reference

`dotnet add package ZeroAlloc.TestHelpers` writes the right reference, because the package is a development dependency. Written out by hand, it is:

```xml
<PackageReference Include="ZeroAlloc.TestHelpers" Version="1.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>contentfiles;build</IncludeAssets>
</PackageReference>
```

The package has no runtime DLL. It ships three source files that compile directly into your assembly as `internal static` classes in the `ZeroAlloc.TestHelpers` namespace:

| File | Needs | Compiled into your project |
| --- | --- | --- |
| `AllocationGate.cs` | the BCL only | always |
| `TextSnapshot.cs` | the BCL only | always |
| `GeneratorSnapshot.cs` | `Microsoft.CodeAnalysis` | only when your project references Roslyn |

Keep `build` in `IncludeAssets`. The package's `build/ZeroAlloc.TestHelpers.targets` decides whether `GeneratorSnapshot.cs` is compiled. Without it, `GeneratorSnapshot.cs` is always compiled, and a project with no Roslyn reference fails with `CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft'`.

## Choosing whether GeneratorSnapshot is compiled

By default the targets file checks your resolved references. `GeneratorSnapshot.cs` is compiled only when `Microsoft.CodeAnalysis.dll` is among them, whether it comes from a direct `Microsoft.CodeAnalysis.CSharp` reference or arrives transitively. Source-generator test projects already reference Roslyn, so they need no configuration. A Native AOT smoke app or a plain unit-test project gets `AllocationGate` and `TextSnapshot` without pulling in the compiler.

To override the check, set `ZeroAllocTestHelpersIncludeGeneratorSnapshot`:

```xml
<PropertyGroup>
  <!-- true: always compile GeneratorSnapshot.cs. false: never compile it. Unset: detect Roslyn. -->
  <ZeroAllocTestHelpersIncludeGeneratorSnapshot>false</ZeroAllocTestHelpersIncludeGeneratorSnapshot>
</PropertyGroup>
```

## Why source-only

Test infrastructure should never appear on a consumer package's public API surface. By compiling into the consumer's assembly as `internal`, the helpers are scoped to each consumer and cost nothing at the consumer's runtime. `GeneratorSnapshot` also depends on this: `[CallerFilePath]` has to resolve to the consumer's test file.

## Package probes

`tests/PackageProbes/run.sh` packs the package and builds three consumer projects against the nupkg:

- an app with no Roslyn reference that uses only `AllocationGate`;
- a test project with no Roslyn reference that runs `TextSnapshot` tests;
- a test project that references Roslyn and runs `GeneratorSnapshot` tests.

CI runs it with `--aot`, which also publishes the first project with Native AOT.
