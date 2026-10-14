namespace GamingCommander.Core.Models;

/// <summary>
/// Steam ACF (appmanifest) metadata for a game entry (Plan 127 A2). Migrated out of the legacy
/// <c>GameEntry.PlatformMetadata</c> string bag. Named <c>SteamAcfInfo</c> to avoid colliding with
/// the scanner's parsed <c>AcfInfo</c> record.
/// </summary>
public sealed record SteamAcfInfo
{
    /// <summary>Steam scan status: <c>Installed</c>, <c>Moved</c>, <c>Missing</c>, or <c>Orphaned</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Absolute path to the <c>appmanifest_&lt;appid&gt;.acf</c> file.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Physical library root that owns the ACF.</summary>
    public string LibraryPath { get; init; } = string.Empty;

    /// <summary>Where the game folder is expected (moved/missing context).</summary>
    public string ExpectedPath { get; init; } = string.Empty;

    /// <summary>ACF build id.</summary>
    public string BuildId { get; init; } = string.Empty;

    /// <summary>ACF SizeOnDisk value (bytes, as string).</summary>
    public string SizeOnDisk { get; init; } = string.Empty;

    /// <summary>ACF StateFlags bitmask (as string).</summary>
    public string StateFlags { get; init; } = string.Empty;

    /// <summary>True when no ACF data is present.</summary>
    public bool IsEmpty =>
        string.IsNullOrEmpty(Status)
        && string.IsNullOrEmpty(FilePath)
        && string.IsNullOrEmpty(LibraryPath);
}
