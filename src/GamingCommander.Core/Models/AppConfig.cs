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
    bool EnableOnlineMetadata = false,
    /// <summary>
    /// Maximum relative directory depth below a game folder searched for executables by the
    /// recursive fallback. User-configurable (disk-read cost only). Corpus data
    /// (`docs/detection/05-executable-discovery.md`) shows real game exes at rel 1–4, ~85% at
    /// rel ≤3, so the default is 3. Clamped to 1–5 at use.
    /// </summary>
    int ScanDepth = 3,
    /// <summary>Per-store indexing policy. Null means every store is disabled.</summary>
    StorePolicySettings? StorePolicies = null);
