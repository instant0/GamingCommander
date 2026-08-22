namespace GamingCommander.Core.Models;

/// <summary>
/// A discovered game entry stored in the games database.
/// </summary>
public sealed record GameEntry(
    /// <summary>Deterministic unique identifier (MD5-based).</summary>
    string Id,
    /// <summary>Name of the game's installation folder.</summary>
    string FolderName,
    /// <summary>Human-readable game name shown in the UI.</summary>
    string DisplayName,
    /// <summary>Detected or overridden store/platform type.</summary>
    GameSourceKind GameSource,
    /// <summary>True if the user manually changed the source type.</summary>
    bool IsSourceOverridden,
    /// <summary>Path to the primary game executable.</summary>
    string ExecutablePath,
    /// <summary>Path to the game's launcher executable (if any).</summary>
    string LauncherPath,
    /// <summary>Command-line arguments passed to the executable on launch.</summary>
    string CommandLineArguments,
    /// <summary>Path to the launcher manifest file (e.g., Steam ACF).</summary>
    string ManifestPath,
    /// <summary>Timestamp of the most recent scan that produced this entry.</summary>
    DateTimeOffset LastScanned,
    /// <summary>Timestamp of the game directory's most recent modification.</summary>
    DateTimeOffset LastModified,
    /// <summary>
    /// Platform-specific metadata. Common keys: SteamStatus, SteamAppId,
    /// AcfExpectedPath, AcfLibraryPath.
    /// </summary>
    Dictionary<string, string> PlatformMetadata,
    /// <summary>
    /// User-defined tags (e.g., "RPG", "Co-op", "Story Rich").
    /// Managed via F4 dialog. Additive merge with metadata tags.
    /// </summary>
    List<string> Tags,
    /// <summary>
    /// Fields manually set by the user via F4. Keys are field names
    /// (e.g., "DisplayName", "ExecutablePath", "Tags"). Automated
    /// enrichment skips fields present in this dictionary.
    /// Values are ISO timestamps of when the override was set.
    /// </summary>
    Dictionary<string, string> UserOverrides,
    /// <summary>Detected game engine (Plan 102 Phase 2). Unknown when no signal.</summary>
    GameEngineKind GameEngine = GameEngineKind.Unknown,
    /// <summary>User-selected extras from the PCGW catalog / free text. Used on exe launch only.</summary>
    string ExtraLaunchArguments = "");
