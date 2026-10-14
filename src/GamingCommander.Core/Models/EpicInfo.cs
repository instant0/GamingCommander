namespace GamingCommander.Core.Models;

/// <summary>
/// Epic Games Store metadata for a game entry (Plan 127 A2). Migrated out of the legacy
/// platform-metadata string bag.
/// </summary>
public sealed record EpicInfo
{
    /// <summary>Epic scan status: <c>Installed</c>, <c>Missing</c>, or <c>Orphaned</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Absolute path to the ProgramData <c>.item</c> file, when matched.</summary>
    public string ItemPath { get; init; } = string.Empty;

    /// <summary>True when no Epic data is present.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Status) && string.IsNullOrEmpty(ItemPath);
}
