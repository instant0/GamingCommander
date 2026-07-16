using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

public sealed class ShellPaneItemViewModel
{
    public required string Title { get; init; }

    public required string SourceLabel { get; init; }

    public required string PathSummary { get; init; }

    public required string LaunchTarget { get; init; }

    public FileSystemEntryKind Kind { get; init; }

    public bool IsBrowsable => Kind is FileSystemEntryKind.Directory or FileSystemEntryKind.ParentDirectory;

    public DateTimeOffset LastModified { get; init; }

    public string ResolvedType { get; init; } = string.Empty;

    public bool HasOverride { get; init; }

    public string? GameId { get; init; }

    /// <summary>
    /// Platform-specific identifier (e.g. Steam App ID, Epic Catalog Item ID).
    /// Populated from GameEntry.Extra during LoadGamesForRoot.
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

    public int GameCount { get; init; }
}
