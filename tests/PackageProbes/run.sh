#!/usr/bin/env bash
# Packs ZeroAlloc.TestHelpers and builds the probe projects against the nupkg, the way an external
# consumer sees it: contentFiles plus build/*.targets, not the source files linked in directly.
#
#   AllocationGateOnly         no Roslyn reference. Must compile, run, and with --aot publish
#                              natively; GeneratorSnapshot.cs must stay out of its compile. #50
#   GeneratorSnapshotConsumer  references Roslyn. Must keep GeneratorSnapshot with no settings,
#                              and its snapshot tests must pass.
#
# The ZeroAllocTestHelpersIncludeGeneratorSnapshot override is checked in both directions.
#
# Usage: tests/PackageProbes/run.sh [--aot]
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
feed="$here/obj/feed"

# A version no restore has seen before, so the global packages folder cannot serve a stale copy.
version="0.0.0-probe.$(date +%s)"
cache="$(dotnet nuget locals global-packages --list | sed 's/^global-packages: //')"
trap 'rm -rf "$cache/zeroalloc.testhelpers/$version"' EXIT

rm -rf "$feed"
dotnet pack "$repo/src/ZeroAlloc.TestHelpers/ZeroAlloc.TestHelpers.csproj" \
  --configuration Release --output "$feed" -p:Version="$version"

props=(-p:ZeroAllocTestHelpersVersion="$version")

echo "::group::AllocationGateOnly builds and runs without Roslyn"
dotnet run --project "$here/AllocationGateOnly" --configuration Release "${props[@]}"
echo "::endgroup::"

echo "::group::GeneratorSnapshotConsumer keeps GeneratorSnapshot"
dotnet test "$here/GeneratorSnapshotConsumer" --configuration Release "${props[@]}"
echo "::endgroup::"

# Expected to fail. Each check greps for the specific error, so an unrelated failure is not
# mistaken for the override working. A separate configuration keeps these builds apart from
# the ones above.
expect_failure() {
  local name="$1" pattern="$2"; shift 2
  local log
  if log="$(dotnet build "$here/$name" --configuration Probe "${props[@]}" "$@" 2>&1)"; then
    echo "$log"; echo "FAIL: $name built with $*, expected $pattern"; exit 1
  fi
  if ! grep -q "$pattern" <<<"$log"; then
    echo "$log"; echo "FAIL: $name failed with $*, but not with $pattern"; exit 1
  fi
  echo "OK: $name with $* fails with $pattern"
}

echo "::group::Override: true forces GeneratorSnapshot in, false forces it out"
expect_failure AllocationGateOnly "error CS0234" -p:ZeroAllocTestHelpersIncludeGeneratorSnapshot=true
expect_failure GeneratorSnapshotConsumer "error CS0103" -p:ZeroAllocTestHelpersIncludeGeneratorSnapshot=false
echo "::endgroup::"

if [[ "${1:-}" == "--aot" ]]; then
  echo "::group::AllocationGateOnly publishes with Native AOT"
  dotnet publish "$here/AllocationGateOnly" --configuration Release "${props[@]}" --output "$here/obj/aot"
  "$here/obj/aot/AllocationGateOnly"
  echo "::endgroup::"
fi

echo "All package probes passed."
