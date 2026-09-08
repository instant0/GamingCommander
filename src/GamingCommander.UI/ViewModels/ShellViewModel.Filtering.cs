using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Filtering half of <see cref="ShellViewModel"/> (Plan 125 final stage):
/// the F8 cross-library filter, its option list, and tag-enriched game enumeration.
/// </summary>
public sealed partial class ShellViewModel
{
    /// <summary>Show every matching game from every library root.</summary>
    public void ApplyFilter(GameFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Value))
        {
            JumpToLibraryRoots();
            return;
        }

        ActiveFilter = filter;
        _currentLibraryName = string.Empty;
        IsAtRootLevel = false;
        OnPropertyChanged(nameof(CurrentLibraryName));
        OnPropertyChanged(nameof(LeftPaneTitle));
        OnPropertyChanged(nameof(IsFilterActive));
        LoadFilteredGames();
        StatusText = $"Filter: {filter.Caption} ({Items.Count(i => i.Kind == FileSystemEntryKind.File)} games)";
    }

    /// <summary>Drop the filter and return to library roots.</summary>
    public void ClearFilter() => JumpToLibraryRoots();

    /// <summary>All games plus sidecar genre/engine tags, for the F8 list.</summary>
    public IReadOnlyList<GameFilterOption> CollectFilterOptions() =>
        GameFilterMatcher.CollectOptions(
            EnumerateGamesWithExtraTags().Select(x => (x.Game, (IEnumerable<string>)x.Extra)));

    private IEnumerable<(string LibraryName, GameEntry Game, IReadOnlyList<string> Extra)> EnumerateGamesWithExtraTags()
    {
        foreach (Library lib in _libraryManager.Libraries)
        {
            foreach (GameEntry game in _libraryManager.GetGamesForLibrary(lib.Name))
            {
                GameMetadataRecord? sidecar = _metadataStore?.Get(game.Id);
                yield return (lib.Name, game, TagNormalizer.FromMetadata(sidecar?.Genre, sidecar?.Engine));
            }
        }
    }
}