using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Navigation half of <see cref="ShellViewModel"/> (Plan 125 final stage):
/// library-root listing, drill-in/up, current-view reload, and re-anchor actions.
/// </summary>
public sealed partial class ShellViewModel
{
    private int _previousRootIndex;

    /// <summary>Populates the item list with configured library roots.</summary>
    public void JumpToLibraryRoots()
    {
        ActiveFilter = null;
        _currentLibraryName = string.Empty;
        IsAtRootLevel = true;
        _selectedIndex = _previousRootIndex;
        OnPropertyChanged(nameof(CurrentLibraryName));
        OnPropertyChanged(nameof(LeftPaneTitle));
        OnPropertyChanged(nameof(IsFilterActive));
        OnPropertyChanged(nameof(SelectedIndex));
        OnPropertyChanged(nameof(ItemCount));

        Items.Clear();
        foreach (Library lib in _libraryManager.Libraries)
        {
            IReadOnlyList<GameEntry> games = _libraryManager.GetGamesForLibrary(lib.Name);
            string gameCountText = $"({games.Count} game{(games.Count != 1 ? "s" : "")})";
            string folderList = lib.Folders.Count > 0
                ? string.Join("  •  ", lib.Folders)
                : lib.Name;
            Items.Add(new ShellPaneItemViewModel
            {
                // Anchor name is the displayed top-level library. For Standalone anchors
                // this equals the physical folder path; for platform anchors (Steam, Epic)
                // it is a display catalog aggregating many physical folders.
                Title = lib.Name,
                Subtitle = gameCountText,
                LeftPath = lib.Name,
                SourceLabel = lib.Type.ToString(),
                PathSummary = lib.Name,
                LaunchTarget = $"[Enter to browse — {games.Count} game(s)]",
                FolderSummary = folderList,
                Kind = FileSystemEntryKind.Directory,
                LastModified = default,
                ResolvedType = string.Empty,
                GameCount = games.Count,
                StoreBadge = BuildStoreBadge(lib.Type),
            });
        }

        // Clamp in case a root was removed while we were drilled in
        if (_selectedIndex >= Items.Count)
            _selectedIndex = Math.Max(0, Items.Count - 1);
        OnPropertyChanged(nameof(SelectedIndex));

        UpdateDetailsForSelection();
        NavigationChanged?.Invoke();
    }

    /// <summary>Drills into the selected item (root → games, directory → sub-entries) or launches a game.</summary>
    public void NavigateInto()
    {
        ShellPaneItemViewModel? item = SelectedItem;
        if (item is null) return;

        // Handle ".." parent-directory entry — go up one level
        if (item.Kind == FileSystemEntryKind.ParentDirectory)
        {
            NavigateUp();
            return;
        }

        // Handle game file — launch it
        if (item.Kind == FileSystemEntryKind.File)
        {
            RequestLaunch?.Invoke(item);
            return;
        }

        // Handle directory — drill in (only remaining kind: Directory)
        // Save the root list index so we can restore it when navigating back up
        _previousRootIndex = SelectedIndex;

        _currentLibraryName = item.PathSummary;
        IsAtRootLevel = false;
        OnPropertyChanged(nameof(CurrentLibraryName));
        OnPropertyChanged(nameof(LeftPaneTitle));
        _selectedIndex = 0;
        OnPropertyChanged(nameof(SelectedIndex));

        LoadGamesForLibrary(_currentLibraryName);
        UpdateDetailsForSelection();
    }

    /// <summary>Goes up one level (games/filter → roots, or no-op if already at roots).</summary>
    public void NavigateUp()
    {
        if (IsAtRootLevel && ActiveFilter is null)
            return;
        JumpToLibraryRoots();
    }

    /// <summary>Library root for the selected game, or the folder being browsed.</summary>
    public string? GetCurrentLibraryName()
    {
        if (!string.IsNullOrWhiteSpace(SelectedItem?.LibraryName))
            return SelectedItem.LibraryName;
        return IsAtRootLevel ? null : _currentLibraryName;
    }

    /// <summary>Updates the source type of the selected game (re-anchor support).</summary>
    public void RetagSelected(GameSourceKind newType)
    {
        if (SelectedItem?.GameId is null) return;
        _libraryManager.RetagGame(SelectedItem.GameId, newType);
        Reload();
        StatusText = $"Retagged [{newType}]: {SelectedItem.Title}";
    }

    /// <summary>
    /// Reloads the current library after a rescan. Called from MainWindow after F5 rescan.
    /// </summary>
    public void ApplyRescannedGames(IReadOnlyList<GameEntry> games)
    {
        if (IsAtRootLevel) return;
        LoadGamesForLibrary(_currentLibraryName);
        StatusText = "Rescan complete";
    }

    /// <summary>Refreshes the current view by re-loading from database.</summary>
    public void Reload()
    {
        if (ActiveFilter is not null)
            LoadFilteredGames();
        else if (IsAtRootLevel)
            JumpToLibraryRoots();
        else
            LoadGamesForLibrary(_currentLibraryName);
    }
}