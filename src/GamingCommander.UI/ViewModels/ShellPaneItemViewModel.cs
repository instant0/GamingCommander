using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// View model for items displayed in the left-pane list (games, directories, parent entries).
/// </summary>
public sealed class ShellPaneItemViewModel
{
    /// <summary>Display name of the item (game name or directory name).</summary>
    public required string Title { get; init; }

    /// <summary>Short label for the game's source type (e.g., 'Steam', 'Standalone').</summary>
    public required string SourceLabel { get; init; }

    /// <summary>Truncated path summary for display (~50 chars max).</summary>
    public required string PathSummary { get; init; }

    /// <summary>Path used to launch the game when Enter is pressed.</summary>
    public required string LaunchTarget { get; init; }

    /// <summary>Command-line arguments to pass when launching the game. Empty for URI-only launches.</summary>
    public string CommandLineArguments { get; init; } = string.Empty;

    /// <summary>Whether this is a directory, file, or parent directory entry.</summary>
    public FileSystemEntryKind Kind { get; init; }

    /// <summary>True if this item can be drilled into (directory or parent).</summary>
    public bool IsBrowsable => Kind is FileSystemEntryKind.Directory or FileSystemEntryKind.ParentDirectory;

    /// <summary>Timestamp of the last modification to this item.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>The effective source type after applying overrides.</summary>
    public string ResolvedType { get; init; } = string.Empty;

    /// <summary>True if the user manually changed this item's source type.</summary>
    public bool HasOverride { get; init; }

    /// <summary>Database ID of the game, or null for non-game items.</summary>
    public string? GameId { get; init; }

    /// <summary>
    /// Platform-specific identifier (e.g. Steam App ID, Epic Catalog Item ID).
    /// Populated from GameEntry.PlatformMetadata during LoadGamesForRoot.
    /// </summary>
    public string PlatformId { get; init; } = string.Empty;

    /// <summary>
    /// Platform-specific status string (e.g. Steam: Installed, Moved, Orphaned).
    /// Empty for non-platform games.
    /// </summary>
    public string PlatformStatus { get; init; } = string.Empty;

    /// <summary>
    /// Color hex string for PlatformStatus (e.g. "#7FB7A5" for Installed).
    /// Empty when no status is present.
    /// </summary>
    public string PlatformStatusColor { get; init; } = string.Empty;

    /// <summary>
    /// Descriptive status detail (e.g. "Moved (ACF in D:\SteamLibrary)", "Missing — ACF exists but game files not found").
    /// Shown in the right-pane details panel for richer context.
    /// </summary>
    public string PlatformStatusDetail { get; init; } = string.Empty;

    /// <summary>
    /// Foreground color hex for the game title in the left-pane list.
    /// Set when game has a non-normal status (Moved, Orphaned, Missing).
    /// Empty for normal (Installed) or non-platform games — converter returns default text color.
    /// </summary>
    public string ItemStatusColor { get; init; } = string.Empty;

    /// <summary>Number of games in this root (only set for root-level entries).</summary>
    public int GameCount { get; init; }

    /// <summary>
    /// Suffix appended to the item's display for scanning state.
    /// Set to "⏳ Scanning..." when this root is currently being scanned.
    /// Empty when idle.
    /// </summary>
    public string ScanningBadge { get; init; } = string.Empty;
}
