using System.Collections.ObjectModel;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Primary dual-pane shell ViewModel. Manages navigation between library roots
/// and game entries, item selection, details panel, status bar, and platform metadata display.
/// </summary>
public sealed class ShellViewModel : ReactiveObject
{
    private readonly ILibraryManager _libraryManager;
    private readonly IConfigService _configService;
    private readonly ITagColorProvider? _tagColorProvider;
    private readonly IMetadataStore? _metadataStore;
    private GameMetadataRecord? _selectedMetadata;

    private string _currentRootPath = string.Empty;
    private int _selectedIndex;
    private int _previousRootIndex;
    private string _statusText = string.Empty;
    private bool _isScanning;
    private string? _scanningRootPath;

    /// <summary>Raised after navigation completes. Subscribers should re-focus the left pane.</summary>
    public event Action? NavigationChanged;

    /// <summary>Raised when a game should be launched. Subscribers handle the actual process start.</summary>
    public event Action<ShellPaneItemViewModel>? RequestLaunch;

    /// <summary>Sidecar extras for a game, or null.</summary>
    public GameMetadataRecord? GetSidecar(string gameEntryId) =>
        string.IsNullOrWhiteSpace(gameEntryId) ? null : _metadataStore?.Get(gameEntryId);

    /// <summary>Creates the shell ViewModel with navigation, selection, and details panel state.</summary>
    public ShellViewModel(
        ILibraryManager libraryManager,
        IConfigService configService,
        ITagColorProvider? tagColorProvider = null,
        IMetadataStore? metadataStore = null)
    {
        _libraryManager = libraryManager;
        _configService = configService;
        _tagColorProvider = tagColorProvider;
        _metadataStore = metadataStore;

        AppConfig config = _configService.Load();
        if (config.LibraryRoots.Count == 0)
        {
            StatusText = "No library roots configured. Press F2 to add folders in Settings.";
            Items = [];
            InteractionHint = "Press F2 to open Settings and add library folders.";
            return;
        }

            InteractionHint = "Arrows: navigate  |  Enter: launch/drill in  |  Esc/Backspace: go up  |  F4: configure";
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

    /// <summary>Display name of the currently selected game, shown in the details panel.</summary>
    public string DetailsName => SelectedItem?.Title ?? string.Empty;
    /// <summary>Install path of the currently selected game.</summary>
    public string DetailsPath => SelectedItem?.PathSummary ?? string.Empty;
    /// <summary>Source type of the currently selected game (e.g., Steam, GOG).</summary>
    public string DetailsType => SelectedItem?.SourceLabel ?? string.Empty;
    /// <summary>Primary executable path of the currently selected game.</summary>
    public string DetailsExecutable => SelectedItem?.LaunchTarget ?? string.Empty;
    public bool HasMultipleExes => SelectedItem?.HasMultipleExes == true;
    public string MultipleExeWarning =>
        HasMultipleExes
            ? "Multiple EXE files detected — press F4 to choose the main one."
            : string.Empty;
    /// <summary>Last modification timestamp of the game's installation directory.</summary>
    public string DetailsLastModified => FormatTimestamp(SelectedItem?.LastModified);
    /// <summary>The resolved source type as a human-readable string.</summary>
    public string DetailsResolvedType => SelectedItem?.ResolvedType ?? string.Empty;
    /// <summary>Platform-specific identifier (Steam App ID, Epic Catalog ID, etc.).</summary>
    public string DetailsPlatformId => SelectedItem?.PlatformId ?? string.Empty;
    /// <summary>True when a platform-specific identifier is available for the selected game.</summary>
    public bool HasPlatformId => !string.IsNullOrEmpty(SelectedItem?.PlatformId);
    /// <summary>Platform status text (Installed, Moved, Orphaned, Missing).</summary>
    public string DetailsPlatformStatus => SelectedItem?.PlatformStatus ?? string.Empty;
    /// <summary>True when platform status information is available.</summary>
    public bool HasPlatformStatus => !string.IsNullOrEmpty(SelectedItem?.PlatformStatus);
    /// <summary>Hex color code for the platform status display.</summary>
    public string DetailsPlatformStatusColor => SelectedItem?.PlatformStatusColor ?? string.Empty;
    /// <summary>Detailed status text (e.g., 'Moved — ACF expects: D:\...').</summary>
    public string DetailsPlatformStatusDetail => SelectedItem?.PlatformStatusDetail ?? string.Empty;
    /// <summary>True when detailed platform status information is available.</summary>
    public bool HasPlatformStatusDetail => !string.IsNullOrEmpty(SelectedItem?.PlatformStatusDetail);
    /// <summary>True when any item is selected in the left pane.</summary>
    public bool HasSelection => SelectedItem is not null;
    /// <summary>True when a game file (not a directory or parent) is selected.</summary>
    public bool HasGameSelected => SelectedItem is { Kind: FileSystemEntryKind.File };
    /// <summary>True when the selected game has a user-defined folder override.</summary>
    public bool HasOverride => SelectedItem?.HasOverride == true;
    /// <summary>Comma-separated tags for the selected game (e.g., "RPG, Open World").</summary>
    public string DetailsTags => SelectedItem?.Tags ?? string.Empty;
    /// <summary>True when the selected game has tags assigned.</summary>
    public bool HasTags => !string.IsNullOrEmpty(SelectedItem?.Tags);
    /// <summary>Tag badges with colors for the selected game.</summary>
    public List<TagBadgeViewModel> DetailsTagBadges => SelectedItem?.TagBadges ?? [];

    /// <summary>Sidecar extras for the selected game (Plan 119). Empty when no sidecar row.</summary>
    public string DetailsDeveloper => _selectedMetadata?.Developer ?? string.Empty;
    public string DetailsPublisher => _selectedMetadata?.Publisher ?? string.Empty;
    public string DetailsGenre => _selectedMetadata?.Genre ?? string.Empty;
    public string DetailsReleaseDate => _selectedMetadata?.ReleaseDate ?? string.Empty;
    public string DetailsMetacritic => _selectedMetadata?.MetacriticScore?.ToString() ?? string.Empty;
    public string DetailsPcgwUrl => _selectedMetadata?.PcGamingWikiUrl ?? string.Empty;
    /// <summary>True when the sidecar has at least one extra field for the selection.</summary>
    public bool HasMetadataExtras => _selectedMetadata?.HasDisplayableExtras == true;

    /// <summary>Windows config path from PCGW, tokens expanded. Empty when unknown.</summary>
    public string DetailsConfigPath =>
        MetadataDetailsFormatter.WindowsConfig(_selectedMetadata?.Details, SelectedInstallDirectory);
    /// <summary>Windows save path from PCGW, tokens expanded. Empty when unknown.</summary>
    public string DetailsSavePath =>
        MetadataDetailsFormatter.WindowsSaves(_selectedMetadata?.Details, SelectedInstallDirectory);
    public bool DetailsConfigPathClickable =>
        WindowsExplorer.IsClickableFolder(DetailsConfigPath, SelectedInstallDirectory);
    public bool DetailsSavePathClickable =>
        WindowsExplorer.IsClickableFolder(DetailsSavePath, SelectedInstallDirectory);
    public bool DetailsConfigPathDisplayOnly =>
        !string.IsNullOrEmpty(DetailsConfigPath) && !DetailsConfigPathClickable;
    public bool DetailsSavePathDisplayOnly =>
        !string.IsNullOrEmpty(DetailsSavePath) && !DetailsSavePathClickable;

    private string? SelectedInstallDirectory =>
        string.IsNullOrWhiteSpace(SelectedItem?.InstallDirectory) ? null : SelectedItem.InstallDirectory;
    /// <summary>Short PCGW argument catalog for the right pane.</summary>
    public string DetailsCommandLine => MetadataDetailsFormatter.CommandLineSummary(_selectedMetadata?.Details);
    /// <summary>Short video caps (fov, ultrawide, …).</summary>
    public string DetailsVideo => MetadataDetailsFormatter.VideoSummary(_selectedMetadata?.Details);
    /// <summary>True when sidecar operator details exist (paths / args / video).</summary>
    public bool HasMetadataDetails => _selectedMetadata?.Details?.HasAny == true;

    /// <summary>Online / Offline / Lookup Disabled chip (bottom right).</summary>
    public string LookupStatusText
    {
        get => _lookupStatusText;
        set => SetProperty(ref _lookupStatusText, value);
    }
    private string _lookupStatusText = "Lookup Disabled";

    /// <summary>Background lookup queue, next to the Online chip. Empty when idle.</summary>
    public string LookupQueueText
    {
        get => _lookupQueueText;
        set => SetProperty(ref _lookupQueueText, value);
    }
    private string _lookupQueueText = string.Empty;

    /// <summary>Text shown in the bottom status bar.</summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>Context-sensitive hint text shown below the item list.</summary>
    public string InteractionHint { get; private set; } = string.Empty;

    /// <summary>True when a scan is in progress on any root.</summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    /// <summary>Root path currently being scanned, or null if idle. Used for badge display.</summary>
    public string? ScanningRootPath
    {
        get => _scanningRootPath;
        private set => SetProperty(ref _scanningRootPath, value);
    }

    /// <summary>Hotkey-to-action mappings displayed in the bottom command bar.</summary>
    public ObservableCollection<ShellCommandViewModel> Commands { get; } =
    [
        new ShellCommandViewModel { Hotkey = "F1", Label = "Help" },
        new ShellCommandViewModel { Hotkey = "F2", Label = "Setup" },
        new ShellCommandViewModel { Hotkey = "F3", Label = "Lookup" },
        new ShellCommandViewModel { Hotkey = "F4", Label = "Edit" },
        new ShellCommandViewModel { Hotkey = "F5", Label = "Rescan" },
        new ShellCommandViewModel { Hotkey = "F8", Label = "Filter" },
        new ShellCommandViewModel { Hotkey = "F10", Label = "Quit" },
    ];

    /// <summary>The full path of the currently browsed library root.</summary>
    public string CurrentRootPath => _currentRootPath;
    /// <summary>Number of configured library roots.</summary>
    public string ConfiguredRootsCount => $"{_libraryManager.LibraryRoots.Count} folder(s) configured";
    /// <summary>Number of items currently displayed in the left pane.</summary>
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
            IReadOnlyList<GameEntry> games = _libraryManager.GetGamesForRoot(root.RootPath);
            string gameCountText = $"({games.Count} game{(games.Count != 1 ? "s" : "")})";
            string trimmedRoot = root.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folderName = Path.GetFileName(trimmedRoot);
            if (string.IsNullOrEmpty(folderName))
                folderName = trimmedRoot;
            Items.Add(new ShellPaneItemViewModel
            {
                // Plan 117: folder name in title; full path in LeftPath (disambiguates D:\Games vs E:\Games).
                Title = folderName,
                Subtitle = gameCountText,
                LeftPath = root.RootPath,
                SourceLabel = root.DefaultType.ToString(),
                PathSummary = root.RootPath,
                LaunchTarget = $"[Enter to browse — {games.Count} game(s)]",
                Kind = FileSystemEntryKind.Directory,
                LastModified = default,
                ResolvedType = string.Empty,
                GameCount = games.Count,
                StoreBadge = BuildStoreBadge(root.DefaultType),
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

    /// <summary>Returns the full path of the currently browsed library root, or null if at root level.</summary>
    public string? GetCurrentRootPath() => IsAtRootLevel ? null : _currentRootPath;

    /// <summary>Returns the ID of the currently selected game, or null if no game is selected.</summary>
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
    /// Called from MainWindow after F5 rescans the folder.
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

    /// <summary>
    /// Marks a root as currently being scanned. Updates the scanning badge on root entries.
    /// Must be called from the UI thread.
    /// </summary>
    public void SetScanning(string rootPath)
    {
        ScanningRootPath = rootPath;
        IsScanning = true;
        UpdateScanningBadges();
    }

    /// <summary>
    /// Clears scanning state and removes all scanning badges.
    /// Must be called from the UI thread.
    /// </summary>
    public void ClearScanning()
    {
        ScanningRootPath = null;
        IsScanning = false;
        UpdateScanningBadges();
    }

    /// <summary>
    /// Updates scanning badges on root-level items based on current ScanningRootPath.
    /// </summary>
    private void UpdateScanningBadges()
    {
        if (!IsAtRootLevel) return;

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            string expectedBadge = string.Equals(item.PathSummary, ScanningRootPath, StringComparison.OrdinalIgnoreCase)
                ? "⏳ Scanning..."
                : string.Empty;

            if (item.ScanningBadge != expectedBadge)
            {
                Items[i] = new ShellPaneItemViewModel
                {
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    LeftPath = item.LeftPath,
                    SourceLabel = item.SourceLabel,
                    PathSummary = item.PathSummary,
                    LaunchTarget = item.LaunchTarget,
                    CommandLineArguments = item.CommandLineArguments,
                    Kind = item.Kind,
                    LastModified = item.LastModified,
                    ResolvedType = item.ResolvedType,
                    HasOverride = item.HasOverride,
                    GameId = item.GameId,
                    PlatformId = item.PlatformId,
                    PlatformStatus = item.PlatformStatus,
                    PlatformStatusColor = item.PlatformStatusColor,
                    PlatformStatusDetail = item.PlatformStatusDetail,
                    ItemStatusColor = item.ItemStatusColor,
                    GameCount = item.GameCount,
                    ScanningBadge = expectedBadge,
                    Tags = item.Tags,
                    TagBadges = item.TagBadges,
                    StoreBadge = item.StoreBadge,
                };
            }
        }
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
                GameSourceKind.Steam => game.PlatformMetadata.TryGetValue("SteamAppId", out var steamAppId) ? steamAppId : string.Empty,
                GameSourceKind.Epic => game.PlatformMetadata.TryGetValue("EpicCatalogItemId", out var epicCatalogItemId) ? epicCatalogItemId : string.Empty,
                _ => string.Empty,
            };

            string platformStatus = game.GameSource switch
            {
                GameSourceKind.Steam => game.PlatformMetadata.TryGetValue("SteamStatus", out var status) ? status : string.Empty,
                _ => string.Empty,
            };

            string platformStatusColor = platformStatus switch
            {
                // Design: Installed = white (default), only show colors for problems
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
                "Moved" => FormatMovedDetail(game),
                "Missing" => FormatMissingDetail(game),
                "Orphaned" => FormatOrphanedDetail(game),
                _ => string.Empty,
            };

            // Resolve launch target: prefer steam:// URI over raw exe path
            string launchTarget = game.CommandLineArguments.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
                ? game.CommandLineArguments
                : game.ExecutablePath;

            // Left pane: show just the exe filename
            string leftPath = Path.GetFileName(game.ExecutablePath);

            // Parenthetical subtitle: tags if present, empty otherwise
            string subtitle = game.Tags.Count > 0 ? $"({string.Join(", ", game.Tags)})" : string.Empty;

            Items.Add(new ShellPaneItemViewModel
            {
                Title = game.DisplayName,
                Subtitle = subtitle,
                LeftPath = leftPath,
                SourceLabel = game.GameSource.ToString(),
                PathSummary = game.ExecutablePath,
                InstallDirectory = WindowsExplorer.ParentDirectory(game.ExecutablePath) ?? string.Empty,
                HasMultipleExes = game.PlatformMetadata.TryGetValue("ExeCandidateCount", out string? exeCount)
                    && int.TryParse(exeCount, out int n) && n > 1,
                AlternateExes = game.PlatformMetadata.GetValueOrDefault("ExeCandidates", string.Empty),
                LaunchTarget = launchTarget,
                CommandLineArguments = game.CommandLineArguments,
                Kind = FileSystemEntryKind.File,
                LastModified = game.LastModified,
                ResolvedType = game.IsSourceOverridden ? $"{game.GameSource} (override)" : game.GameSource.ToString(),
                HasOverride = game.IsSourceOverridden,
                GameId = game.Id,
                PlatformId = platformId,
                PlatformStatus = platformStatus,
                PlatformStatusColor = platformStatusColor,
                PlatformStatusDetail = platformStatusDetail,
                ItemStatusColor = itemStatusColor,
                GameCount = 0,
                Tags = game.Tags.Count > 0 ? string.Join(", ", game.Tags) : string.Empty,
                TagBadges = BuildTagBadges(game.Tags),
                StoreBadge = BuildStoreBadge(game.GameSource),
            });
        }

        OnPropertyChanged(nameof(ItemCount));
        NavigationChanged?.Invoke();
    }

    private void UpdateDetailsForSelection()
    {
        string? gameId = SelectedItem?.GameId;
        _selectedMetadata = gameId is null ? null : _metadataStore?.Get(gameId);

        OnPropertyChanged(nameof(SelectedItem));
        OnPropertyChanged(nameof(DetailsName));
        OnPropertyChanged(nameof(DetailsPath));
        OnPropertyChanged(nameof(DetailsType));
        OnPropertyChanged(nameof(DetailsExecutable));
        OnPropertyChanged(nameof(HasMultipleExes));
        OnPropertyChanged(nameof(MultipleExeWarning));
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
        OnPropertyChanged(nameof(DetailsTags));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(DetailsTagBadges));
        OnPropertyChanged(nameof(DetailsDeveloper));
        OnPropertyChanged(nameof(DetailsPublisher));
        OnPropertyChanged(nameof(DetailsGenre));
        OnPropertyChanged(nameof(DetailsReleaseDate));
        OnPropertyChanged(nameof(DetailsMetacritic));
        OnPropertyChanged(nameof(DetailsPcgwUrl));
        OnPropertyChanged(nameof(HasMetadataExtras));
        OnPropertyChanged(nameof(DetailsConfigPath));
        OnPropertyChanged(nameof(DetailsSavePath));
        OnPropertyChanged(nameof(DetailsConfigPathClickable));
        OnPropertyChanged(nameof(DetailsSavePathClickable));
        OnPropertyChanged(nameof(DetailsConfigPathDisplayOnly));
        OnPropertyChanged(nameof(DetailsSavePathDisplayOnly));
        OnPropertyChanged(nameof(DetailsCommandLine));
        OnPropertyChanged(nameof(DetailsVideo));
        OnPropertyChanged(nameof(HasMetadataDetails));
    }

    /// <summary>Applies a sidecar row after a background refresh (Plan 119 step 5). Call on the UI thread.</summary>
    public void ApplySidecarMetadata(string gameEntryId, GameMetadataRecord? record)
    {
        if (SelectedItem?.GameId != gameEntryId)
            return;

        _selectedMetadata = record;
        OnPropertyChanged(nameof(DetailsDeveloper));
        OnPropertyChanged(nameof(DetailsPublisher));
        OnPropertyChanged(nameof(DetailsGenre));
        OnPropertyChanged(nameof(DetailsReleaseDate));
        OnPropertyChanged(nameof(DetailsMetacritic));
        OnPropertyChanged(nameof(DetailsPcgwUrl));
        OnPropertyChanged(nameof(HasMetadataExtras));
        OnPropertyChanged(nameof(DetailsConfigPath));
        OnPropertyChanged(nameof(DetailsSavePath));
        OnPropertyChanged(nameof(DetailsConfigPathClickable));
        OnPropertyChanged(nameof(DetailsSavePathClickable));
        OnPropertyChanged(nameof(DetailsConfigPathDisplayOnly));
        OnPropertyChanged(nameof(DetailsSavePathDisplayOnly));
        OnPropertyChanged(nameof(DetailsCommandLine));
        OnPropertyChanged(nameof(DetailsVideo));
        OnPropertyChanged(nameof(HasMetadataDetails));
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

    /// <summary>Formats actionable detail text for Orphaned Steam games (folder exists, no ACF).</summary>
    private static string FormatOrphanedDetail(GameEntry game)
    {
        string folder = game.PlatformMetadata.GetValueOrDefault("FolderName", game.FolderName);
        string libRoot = game.PlatformMetadata.GetValueOrDefault("LibraryRoot", "unknown library");
        return $"Orphaned — no Steam manifest for '{folder}'. " +
               $"This folder exists in {libRoot} but is not registered with Steam. " +
               "To fix: Use ACF Generate to create a manifest, or re-install via Steam.";
    }

    /// <summary>Formats actionable detail text for Missing Steam games (ACF exists, no game folder).</summary>
    private static string FormatMissingDetail(GameEntry game)
    {
        string acfPath = game.PlatformMetadata.GetValueOrDefault("AcfFilePath", "");
        string expected = game.PlatformMetadata.GetValueOrDefault("AcfExpectedPath", "");
        return $"Missing — Steam manifest at {acfPath} expects files at {expected}. " +
               "Game folder not found in any configured library. " +
               "Possible: game is in an unconfigured library, or was uninstalled. " +
               "To fix: Add the correct Steam library via F2, or delete the orphaned ACF.";
    }

    /// <summary>Formats actionable detail text for Moved Steam games (game in different library than ACF).</summary>
    private static string FormatMovedDetail(GameEntry game)
    {
        string acfPath = game.PlatformMetadata.GetValueOrDefault("AcfFilePath", "unknown");
        string acfLib = game.PlatformMetadata.GetValueOrDefault("AcfLibraryPath", "unknown");
        string actualLib = game.PlatformMetadata.GetValueOrDefault("ActualLibraryRoot", "unknown");
        string folder = game.PlatformMetadata.GetValueOrDefault("FolderName", game.FolderName);
        string acfFileName = Path.GetFileName(acfPath);
        string targetPath = Path.Combine(actualLib, "steamapps", acfFileName);
        return $"Moved — game '{folder}' found in {actualLib} but ACF is in {acfLib}. " +
               $"To fix: Move ACF to {targetPath} and restart Steam.";
    }

    /// <summary>
    /// Builds tag badge view models with configurable colors for each tag.
    /// </summary>
    private List<TagBadgeViewModel> BuildTagBadges(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0) return [];

        var badges = new List<TagBadgeViewModel>(tags.Count);
        foreach (string tag in tags)
        {
            TagType tagType = _tagColorProvider?.GetTagType(tag) ?? Core.Models.TagType.User;
            var (bg, fg) = _tagColorProvider?.GetColor(tag, tagType) ?? ("#2A3A4A", "#B8C8D8");
            badges.Add(new TagBadgeViewModel { Name = tag, Background = bg, Foreground = fg });
        }
        return badges;
    }

    /// <summary>
    /// Mapping from GameSourceKind to (display label, color config key).
    /// Display label is what appears on the badge; color key is looked up in tag_colors.json.
    /// </summary>
    private static readonly Dictionary<GameSourceKind, (string Label, string ColorKey)> s_storeBadgeMap = new()
    {
        [GameSourceKind.Steam] = ("Steam", "Steam"),
        [GameSourceKind.Gog] = ("GOG", "GOG"),
        [GameSourceKind.Epic] = ("Epic", "Epic"),
        [GameSourceKind.EaApp] = ("EA", "EA"),
        [GameSourceKind.UbisoftConnect] = ("Ubisoft", "Ubisoft"),
        [GameSourceKind.BattleNet] = ("Battle.net", "BattleNet"),
        [GameSourceKind.Xbox] = ("Xbox", "Xbox"),
        [GameSourceKind.Rockstar] = ("Rockstar", "Rockstar"),
        [GameSourceKind.SteamEmu] = ("Steam Emu", "Steam Emu"),
    };

    /// <summary>
    /// Builds a store badge for the left pane. Returns null for Standalone/Unknown (no badge shown).
    /// </summary>
    private TagBadgeViewModel? BuildStoreBadge(GameSourceKind source)
    {
        if (!s_storeBadgeMap.TryGetValue(source, out var entry))
            return null;

        var (bg, fg) = _tagColorProvider?.GetColor(entry.ColorKey, TagType.Store) ?? ("#2A3A4A", "#B8C8D8");
        return new TagBadgeViewModel { Name = entry.Label, Background = bg, Foreground = fg };
    }
}
