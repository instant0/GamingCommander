namespace GamingCommander.Core.Models;

/// <summary>
/// Application configuration persisted to settings.json.
/// Library (anchor) definitions live in their own file (libraries.json) via
/// ILibrariesService — not here.
/// </summary>
public sealed record AppConfig(
    /// <summary>Folder names to exclude from game scanning.</summary>
    IReadOnlyList<string> HiddenFolders,
    /// <summary>True if the first-run wizard has not yet completed.</summary>
    bool IsFirstRun,
    /// <summary>Last application version that was launched (for upgrade detection).</summary>
    string? LastSeenVersion = null,
    /// <summary>Whether to query online metadata sources (PCGamingWiki, etc.).</summary>
    bool EnableOnlineMetadata = false);
