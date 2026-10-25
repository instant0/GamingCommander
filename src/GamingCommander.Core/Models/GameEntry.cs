namespace GamingCommander.Core.Models;

/// <summary>
/// A discovered game entry stored in the games database (Plan 127 A1). Composed of typed
/// sub-records: <see cref="Identity"/>, <see cref="Location"/>, <see cref="Launch"/>,
/// <see cref="Detection"/>, <see cref="Store"/>, <see cref="Acf"/>, <see cref="Epic"/>.
/// The flat read-only accessors below preserve the historical call surface.
/// </summary>
public sealed record GameEntry(
    /// <summary>What the game is: id, anchor, title, source.</summary>
    IdentityInfo Identity,
    /// <summary>Where the game physically lives.</summary>
    LocationInfo Location,
    /// <summary>How the game is launched.</summary>
    LaunchInfo Launch,
    /// <summary>Typed tags (user + auto-derived engine/genre) — Plan 127 B13.</summary>
    List<Tag> Tags,
    /// <summary>Fields manually set by the user via F4; values are ISO timestamps.</summary>
    Dictionary<string, string> UserOverrides,
    /// <summary>Detected game engine (Plan 102 Phase 2). Unknown when no signal.</summary>
    GameEngineKind GameEngine = GameEngineKind.Unknown,
    /// <summary>Timestamp of the most recent scan that produced this entry.</summary>
    DateTimeOffset LastScanned = default,
    /// <summary>Timestamp of the game directory's most recent modification.</summary>
    DateTimeOffset LastModified = default)
{
    /// <summary>Detection provenance — why this entry is a game and how confident (Plan 127 A3).</summary>
    public DetectionInfo Detection { get; init; } = new();

    /// <summary>Typed store identifiers (Plan 127 A2).</summary>
    public StoreInfo Store { get; init; } = new();

    /// <summary>Steam ACF metadata (Plan 127 A2).</summary>
    public SteamAcfInfo Acf { get; init; } = new();

    /// <summary>Epic Games Store metadata (Plan 127 A2).</summary>
    public EpicInfo Epic { get; init; } = new();

    /// <summary>Unified install state + version (Plan 127 B8).</summary>
    public InstallInfo Install { get; init; } = new();

    /// <summary>Usage tracking — last played, launch count, play time (Plan 127 B7).</summary>
    public TrackingInfo Tracking { get; init; } = new();

    /// <summary>User-owned state — favourite/hidden/rating/notes/completion (Plan 127 B9).</summary>
    public UserStateInfo UserState { get; init; } = new();

    /// <summary>Kind + parent/add-on links (Plan 127 B11).</summary>
    public RelationsInfo Relations { get; init; } = new();

    // ── Read-only convenience accessors (historical flat surface) ──────────────

    /// <summary>Deterministic unique identifier (MD5-based).</summary>
    public string Id => Identity.Id;

    /// <summary>Library (anchor) name this game belongs to.</summary>
    public string Library => Identity.Library;

    /// <summary>Human-readable game name shown in the UI.</summary>
    public string DisplayName => Identity.DisplayName;

    /// <summary>Detected or overridden store/platform type.</summary>
    public GameSourceKind GameSource => Identity.GameSource;

    /// <summary>True if the user manually changed the source type.</summary>
    public bool IsSourceOverridden => Identity.IsSourceOverridden;

    /// <summary>Absolute physical path of the game's installation folder.</summary>
    public string FolderPath => Location.FolderPath;

    /// <summary>Name of the game's installation folder.</summary>
    public string FolderName => Location.FolderName;

    /// <summary>Path to the primary game executable.</summary>
    public string ExecutablePath => Launch.ExecutablePath;

    /// <summary>Path to the game's launcher executable (if any).</summary>
    public string LauncherPath => Launch.LauncherPath;

    /// <summary>Command-line arguments passed to the executable on launch.</summary>
    public string CommandLineArguments => Launch.CommandLineArguments;

    /// <summary>User-selected extras from the PCGW catalog / free text.</summary>
    public string ExtraLaunchArguments => Launch.ExtraLaunchArguments;

    /// <summary>Path to the launcher manifest file (e.g., Steam ACF).</summary>
    public string ManifestPath => Launch.ManifestPath;
}
