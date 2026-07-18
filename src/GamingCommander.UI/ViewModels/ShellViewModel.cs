using System.Collections.ObjectModel;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Primary dual-pane shell ViewModel. Manages navigation between library roots
/// and game entries, item selection, details panel, status bar, and platform metadata display.
/// </summary>
public sealed class ShellViewModel : ReactiveObject
{
    private readonly ILibraryManager _libraryManager;
    private readonly IConfigService _configService;

    private string _currentRootPath = string.Empty;
    private int _selectedIndex;
    private int _previousRootIndex;
    private string _statusText = string.Empty;

    /// <summary>Raised after navigation completes. Subscribers should re-focus the left pane.</summary>
    public event Action? NavigationChanged;

    /// <summary>Raised when a game should be launched. Subscribers handle the actual process start.</summary>
    public event Action<ShellPaneItemViewModel>? RequestLaunch;

    public ShellViewModel(ILibraryManager libraryManager, IConfigService configService)
    {
        _libraryManager = libraryManager;
        _configService = configService;

        AppConfig config = _configService.Load();
        if (config.LibraryRoots.Count == 0)
        {
            StatusText = "No library roots configured. Press F2 to add folders in Settings.";
            Items = [];
            InteractionHint = "Press F2 to open Settings and add library folders.";
            return;
        }

            InteractionHint = "Arrows: navigate  |  Enter: launch/drill in  |  Esc/Backspace: go up  |  F4: edit tags  |  F5: launch  |  F9: roots";
        JumpToLibraryRoots();
    }

    /// <summary>Title displayed in the left pane header (root name or truncated path).</summary>
    public string LeftPaneTitle => IsAtRootLevel ? "Library Roots" : TruncatePath(_currentRootPath);

    /// <summary>Title displayed in the right pane header ('Details').</summary>
    public string RightPaneTitle => "Details";

    /// <summary>True when viewing the top-level library root list (not inside a root).</summary>
    public bool IsAtRootLevel
    {
        get => _isAtRootLevel;
        private set
        {
            if (SetProperty(ref _isAtRootLevel, value))
                OnPropertyChanged(nameof(LeftPaneTitle));
        }
    }
    private bool _isAtRootLevel = true;

    /// <summary>Observable collection of items displayed in the left pane.</summary>
    public ObservableCollection<ShellPaneItemViewModel> Items { get; } = [];

    /// <summary>Index of the currently selected item in the left pane. -1 if nothing selected.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (SetProperty(ref _selectedIndex, value))
                UpdateDetailsForSelection();
        }
    }

    /// <summary>The currently selected ShellPaneItemViewModel, or null.</summary>
    public ShellPaneItemViewModel? SelectedItem =>
        SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    public string DetailsName => SelectedItem?.Title ?? string.Empty;
    public string DetailsPath => SelectedItem?.PathSummary ?? string.Empty;
    public string DetailsType => SelectedItem?.SourceLabel ?? string.Empty;
    public string DetailsExecutable => SelectedItem?.LaunchTarget ?? string.Empty;
    public string DetailsLastModified => FormatTimestamp(SelectedItem?.LastModified);
    public string DetailsResolvedType => SelectedItem?.ResolvedType ?? string.Empty;
    public string DetailsPlatformId => SelectedItem?.PlatformId ?? string.Empty;
    public bool HasPlatformId => !string.IsNullOrEmpty(SelectedItem?.PlatformId);
    public string DetailsPlatformStatus => SelectedItem?.PlatformStatus ?? string.Empty;
    public bool HasPlatformStatus => !string.IsNullOrEmpty(SelectedItem?.PlatformStatus);
    public string DetailsPlatformStatusColor => SelectedItem?.PlatformStatusColor ?? string.Empty;
    public string DetailsPlatformStatusDetail => SelectedItem?.PlatformStatusDetail ?? string.Empty;
    public bool HasPlatformStatusDetail => !string.IsNullOrEmpty(SelectedItem?.PlatformStatusDetail);
    public bool HasSelection => SelectedItem is not null;
    /// <summary>True when a game file (not a directory or parent) is selected.</summary>
    public bool HasGameSelected => SelectedItem is { Kind: FileSystemEntryKind.File };
    public bool HasOverride => SelectedItem?.HasOverride == true;

    /// <summary>Text shown in the bottom status bar.</summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>Context-sensitive hint text shown below the item list.</summary>
    public string InteractionHint { get; private set; } = string.Empty;

    public ObservableCollection<ShellCommandViewModel> Commands { get; } =
    [
        new ShellCommandViewModel { Hotkey = "F1", Label = "Help" },
        new ShellCommandViewModel { Hotkey = "F2", Label = "Setup" },
        new ShellCommandViewModel { Hotkey = "F3", Label = "Info" },
        new ShellCommandViewModel { Hotkey = "F4", Label = "Edit" },
        new ShellCommandViewModel { Hotkey = "F5", Label = "Launch" },
        new ShellCommandViewModel { Hotkey = "F6", Label = "Rescan" },
        new ShellCommandViewModel { Hotkey = "F7", Label = "Add" },
        new ShellCommandViewModel { Hotkey = "F8", Label = "Filter" },
        new ShellCommandViewModel { Hotkey = "F9", Label = "Drives" },
        new ShellCommandViewModel { Hotkey = "F10", Label = "Quit" },
    ];

    public string CurrentRootPath => _currentRootPath;
    public string ConfiguredRootsCount => $"{_libraryManager.LibraryRoots.Count} folder(s) configured";
    public int ItemCount => Items.Count;

    /// <summary>Populates the item list with configured library roots.</summary>
    public void JumpToLibraryRoots()
    {
        _currentRootPath = string.Empty;
        IsAtRootLevel = true;
        _selectedIndex = _previousRootIndex;
        OnPropertyChanged(nameof(CurrentRootPath));
        OnPropertyChanged(nameof(LeftPaneTitle));
        OnPropertyChanged(nameof(SelectedIndex));
        OnPropertyChanged(nameof(ItemCount));

        Items.Clear();
        foreach (LibraryRoot root in _libraryManager.LibraryRoots)
        {
            IReadOnlyList<GameEntry> games = _libraryManager.GetGamesForRoot(root.Path);
            Items.Add(new ShellPaneItemViewModel
            {
                Title = Path.GetFileName(root.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                SourceLabel = root.DefaultType.ToString(),
                PathSummary = root.Path,
                LaunchTarget = $"[Enter to browse — {games.Count} game(s)]",
                Kind = FileSystemEntryKind.Directory,
                LastModified = default,
                ResolvedType = string.Empty,
                GameCount = games.Count,
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

        _currentRootPath = item.PathSummary;
        IsAtRootLevel = false;
        OnPropertyChanged(nameof(CurrentRootPath));
        OnPropertyChanged(nameof(LeftPaneTitle));
        _selectedIndex = 0;
        OnPropertyChanged(nameof(SelectedIndex));

        LoadGamesForRoot(_currentRootPath);
        UpdateDetailsForSelection();
    }

    /// <summary>Goes up one level (games → roots, or no-op if already at roots).</summary>
    public void NavigateUp()
    {
        if (IsAtRootLevel) return;
        JumpToLibraryRoots();
    }

    public string? GetCurrentRootPath() => IsAtRootLevel ? null : _currentRootPath;

    public string? GetSelectedGameId() => SelectedItem?.GameId;

    /// <summary>Updates the source type of the selected game.</summary>
    public void RetagSelected(GameSourceKind newType)
    {
        if (SelectedItem?.GameId is null || IsAtRootLevel) return;
        _libraryManager.RetagGame(_currentRootPath, SelectedItem.GameId, newType);
        LoadGamesForRoot(_currentRootPath);
        StatusText = $"Retagged [{newType}]: {SelectedItem.Title}";
    }

    /// <summary>
    /// Replaces the current root's game entries with freshly scanned data.
    /// Called from MainWindow after F6 rescans the folder.
    /// </summary>
    public void ApplyRescannedGames(IReadOnlyList<GameEntry> games)
    {
        if (IsAtRootLevel) return;
        _libraryManager.RescanRoot(_currentRootPath, games);
        LoadGamesForRoot(_currentRootPath);
        StatusText = "Rescan complete";
    }

    /// <summary>Refreshes the current view by re-loading from database.</summary>
    public void Reload()
    {
        if (IsAtRootLevel)
            JumpToLibraryRoots();
        else
            LoadGamesForRoot(_currentRootPath);
    }

    private void LoadGamesForRoot(string rootPath)
    {
        Items.Clear();
        IReadOnlyList<GameEntry> games = _libraryManager.GetGamesForRoot(rootPath);

        // Add ".." parent-directory entry at the top
        Items.Add(new ShellPaneItemViewModel
        {
            Title = "..",
            SourceLabel = string.Empty,
            PathSummary = "Parent directory",
            LaunchTarget = string.Empty,
            Kind = FileSystemEntryKind.ParentDirectory,
            LastModified = default,
            ResolvedType = string.Empty,
            HasOverride = false,
            GameId = null,
            GameCount = 0,
        });

        foreach (GameEntry game in games)
        {
            // Extract platform-specific metadata from Extra dictionary
            string platformId = game.GameSource switch
            {
                GameSourceKind.Steam => game.Extra.TryGetValue("SteamAppId", out var sid) ? sid : string.Empty,
                GameSourceKind.Epic => game.Extra.TryGetValue("EpicCatalogItemId", out var eid) ? eid : string.Empty,
                _ => string.Empty,
            };

            string platformStatus = game.GameSource switch
            {
                GameSourceKind.Steam => game.Extra.TryGetValue("SteamStatus", out var status) ? status : string.Empty,
                _ => string.Empty,
            };

            string platformStatusColor = platformStatus switch
            {
                "Installed" => "#7FB7A5",
                "Moved" => "#E8C547",
                "Orphaned" => "#E87070",
                "Missing" => "#E87070",
                _ => string.Empty,
            };

            // Left-pane list color: empty for Installed/non-platform (converter returns default), colored for problems
            string itemStatusColor = platformStatus switch
            {
                "Moved" => "#E8C547",
                "Orphaned" => "#E87070",
                "Missing" => "#E87070",
                _ => string.Empty,
            };

            // Richer status detail for the details panel
            string platformStatusDetail = platformStatus switch
            {
                "Moved" => game.Extra.TryGetValue("AcfExpectedPath", out var expectedPath)
                    ? $"Moved — ACF expects: {expectedPath}"
                    : "Moved — ACF is in a different library",
                "Missing" => "Missing — ACF exists but game files not found",
                "Orphaned" => "Orphaned — game folder has no ACF registration",
                _ => string.Empty,
            };

            Items.Add(new ShellPaneItemViewModel
            {
                Title = game.DisplayName,
                SourceLabel = game.GameSource.ToString(),
                PathSummary = game.ExecutablePath,
                LaunchTarget = game.ExecutablePath,
                Kind = FileSystemEntryKind.File,
                LastModified = game.LastModified,
                ResolvedType = game.Override ? $"{game.GameSource} (override)" : game.GameSource.ToString(),
                HasOverride = game.Override,
                GameId = game.Id,
                PlatformId = platformId,
                PlatformStatus = platformStatus,
                PlatformStatusColor = platformStatusColor,
                PlatformStatusDetail = platformStatusDetail,
                ItemStatusColor = itemStatusColor,
                GameCount = 0,
            });
        }

        OnPropertyChanged(nameof(ItemCount));
        NavigationChanged?.Invoke();
    }

    private void UpdateDetailsForSelection()
    {
        OnPropertyChanged(nameof(SelectedItem));
        OnPropertyChanged(nameof(DetailsName));
        OnPropertyChanged(nameof(DetailsPath));
        OnPropertyChanged(nameof(DetailsType));
        OnPropertyChanged(nameof(DetailsExecutable));
        OnPropertyChanged(nameof(DetailsLastModified));
        OnPropertyChanged(nameof(DetailsPlatformId));
        OnPropertyChanged(nameof(HasPlatformId));
        OnPropertyChanged(nameof(DetailsPlatformStatus));
        OnPropertyChanged(nameof(HasPlatformStatus));
        OnPropertyChanged(nameof(DetailsPlatformStatusColor));
        OnPropertyChanged(nameof(DetailsPlatformStatusDetail));
        OnPropertyChanged(nameof(HasPlatformStatusDetail));
        OnPropertyChanged(nameof(DetailsResolvedType));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasGameSelected));
        OnPropertyChanged(nameof(HasOverride));
    }

    private static string TruncatePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Library";
        if (path.Length <= 50) return path;
        return "..." + path[(path.Length - 47)..];
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue || timestamp.Value == default) return "—";
        return timestamp.Value.ToString("yyyy-MM-dd HH:mm");
    }
}
