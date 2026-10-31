using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Identifies the source file a reader consumed, so callers can skip unchanged inventories.
/// </summary>
public sealed record StoreManifestFingerprint(
    string Path,
    long Length,
    DateTimeOffset LastWriteUtc);

/// <summary>
/// Read-only view of one store's own installed-game inventory. A reader never writes and
/// never starts a recursive scan.
/// </summary>
public interface IStoreInventoryReader
{
    /// <summary>The store this reader covers.</summary>
    GameSourceKind Kind { get; }

    /// <summary>Fingerprint of the last read source, when the store has a file source.</summary>
    StoreManifestFingerprint? Fingerprint { get; }

    /// <summary>Reads the inventory once. Missing folders are reported, not skipped.</summary>
    IReadOnlyList<StoreInventoryEntry> Read();
}
