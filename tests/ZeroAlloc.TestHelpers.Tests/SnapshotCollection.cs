using Xunit;

namespace ZeroAlloc.TestHelpers.Tests;

/// <summary>
/// The snapshot tests run one at a time: some set <c>ZA_SNAPSHOT_UPDATE</c>, which is
/// process-wide, and some add files to the shared Snapshots folder.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SnapshotCollection
{
    public const string Name = "snapshots";
}
