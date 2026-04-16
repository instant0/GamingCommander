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

    public string? GameId { get; init; }

    public int GameCount { get; init; }
}
