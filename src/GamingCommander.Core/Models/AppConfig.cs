namespace GamingCommander.Core.Models;

/// <summary>
/// Application configuration persisted to settings.json.
/// </summary>
public sealed record AppConfig(
    /// <summary>Configured library root paths with default game source types.</summary>
    IReadOnlyList<LibraryRoot> LibraryRoots,
    /// <summary>Per-folder source type overrides that take precedence over root defaults.</summary>
    IReadOnlyList<FolderOverride> FolderOverrides,
    /// <summary>Folder names to exclude from game scanning.</summary>
    IReadOnlyList<string> HiddenFolders,
    /// <summary>True if the first-run wizard has not yet completed.</summary>
    bool IsFirstRun,
    /// <summary>Last application version that was launched (for upgrade detection).</summary>
    string? LastSeenVersion = null,
    /// <summary>Whether to query online metadata sources (PCGamingWiki, etc.).</summary>
    bool EnableOnlineMetadata = false);
