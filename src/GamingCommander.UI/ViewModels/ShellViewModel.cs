using System.Collections.ObjectModel;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Primary dual-pane shell ViewModel. Manages navigation between library roots
/// and game entries, item selection, details panel, status bar, and platform metadata display.
/// Split into domain partials (Plan 125 final stage): Navigation, Filtering, Search,
/// Loading, Scanning, Details, Details.Status, Details.Metadata, Tags.
/// </summary>
public sealed partial class ShellViewModel : ReactiveObject
{
    private readonly ILibraryManager _libraryManager;
    private readonly IConfigService _configService;
    private readonly ITagColorProvider? _tagColorProvider;
    private readonly IMetadataStore? _metadataStore;

    private string _currentLibraryName = string.Empty;
    private int _selectedIndex;
    private string _statusText = string.Empty;
    private bool _isScanning;
    private string? _scanningRootPath;

    /// <summary>Typed characters needed before live filtering starts (Plan 122).</summary>
    public const int SearchThreshold = 3;

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
        if (_libraryManager.Libraries.Count == 0)
        {
            StatusText = "No libraries configured. Press F2 to add library folders.";
            Items = [];
            InteractionHint = "Press F2 to open Settings and add library folders.";
            return;
        }

            InteractionHint = "Arrows: navigate  |  Type: search  |  Enter: launch/drill in  |  Esc/Backspace: go up  |  F4: configure";
        JumpToLibraryRoots();
    }

    /// <summary>Title displayed in the left pane header (live search, root name, path, or active filter).</summary>
    public string LeftPaneTitle =>
        _searchBuffer.Length > 0
            ? $"Search: '{_searchBuffer}'"
            : ActiveFilter is not null
                ? $"Filter: {ActiveFilter.Caption}"
                : IsAtRootLevel ? "Library Roots" : TruncatePath(_currentLibraryName);

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

    /// <summary>Cross-library filter, or null when browsing roots / one folder.</summary>
    public GameFilter? ActiveFilter { get; private set; }

    public bool IsFilterActive => ActiveFilter is not null;

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

    /// <summary>The name of the currently browsed library anchor.</summary>
    public string CurrentLibraryName => _currentLibraryName;
    /// <summary>Number of configured libraries (anchors).</summary>
    public string ConfiguredRootsCount => $"{_libraryManager.Libraries.Count} librar{(_libraryManager.Libraries.Count != 1 ? "ies" : "y")}";
    /// <summary>Number of items currently displayed in the left pane.</summary>
    public int ItemCount => Items.Count;

    /// <summary>Returns the ID of the currently selected game, or null if no game is selected.</summary>
    public string? GetSelectedGameId() => SelectedItem?.GameId;

    private static string TruncatePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Library";
        if (path.Length <= 50) return path;
        return "..." + path[(path.Length - 47)..];
    }
}