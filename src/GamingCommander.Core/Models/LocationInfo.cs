namespace GamingCommander.Core.Models;

/// <summary>
/// Physical-location metadata for a game entry (Plan 127 A2). Migrated out of the legacy
/// platform-metadata string bag. Distinct from
/// <see cref="GameEntry.FolderPath"/> (the absolute game folder): these are scanner-derived
/// roots and catalog folder names used for status/detail display and repair.
/// </summary>
public sealed record LocationInfo
{
    /// <summary>Absolute physical path of the game's installation folder.</summary>
    public string FolderPath { get; init; } = string.Empty;

    /// <summary>Name of the game's installation folder.</summary>
    public string FolderName { get; init; } = string.Empty;

    /// <summary>Physical library root the folder lives under (or the catalog manifests dir for Epic).</summary>
    public string LibraryRoot { get; init; } = string.Empty;

    /// <summary>Actual physical library root where the game was found (moved context).</summary>
    public string ActualLibraryRoot { get; init; } = string.Empty;

    /// <summary>Catalog game folder path (Epic install location / repair context).</summary>
    public string GameFolder { get; init; } = string.Empty;

    /// <summary>Prior physical folder paths this game lived at (external-move history, Plan 127 A5).</summary>
    public List<string> PreviousLocations { get; init; } = [];

    /// <summary>True when no location data is present.</summary>
    public bool IsEmpty =>
        string.IsNullOrEmpty(FolderPath)
        && string.IsNullOrEmpty(FolderName)
        && string.IsNullOrEmpty(LibraryRoot)
        && string.IsNullOrEmpty(ActualLibraryRoot)
        && string.IsNullOrEmpty(GameFolder);
}
