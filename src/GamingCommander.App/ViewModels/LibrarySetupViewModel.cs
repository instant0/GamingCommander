using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.ViewModels;

/// <summary>
/// ViewModel for the unified Library Setup dialog (F2). Manages adding, removing,
/// and rescanning library roots. Handles both first-run onboarding and ongoing management.
/// </summary>
public sealed class LibrarySetupViewModel : GamingCommander.UI.ViewModels.ReactiveObject
{
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _dbService;
    private readonly ILibraryManager _libraryManager;
    private readonly Window _window;

    public LibrarySetupViewModel(
        IConfigService configService,
        IGamesDatabaseService dbService,
        ILibraryManager libraryManager,
        Window window,
        bool isFirstRun = false)
    {
        _configService = configService;
        _dbService = dbService;
        _libraryManager = libraryManager;
        _window = window;

        // Load metadata toggle from config
        AppConfig config = _configService.Load();
        _enableOnlineMetadata = config.EnableOnlineMetadata;

        // Set title/subtitle based on context
        if (isFirstRun && config.LibraryRoots.Count == 0)
        {
            _titleText = "Welcome to GamingCommander";
            _subtitleText = "Add your game library folders below. For each folder, GamingCommander will scan it and find your games. Select the platform type for each folder — this sets the default for all games inside it.";
            _tipText = "Tip: Steam library roots should point to the folder containing steamapps/, not the steamapps/ itself.";
        }
        else
        {
            _titleText = "Library Root Setup";
            _subtitleText = "Add, remove, or rescan library roots. Changes apply immediately.";
            _tipText = string.Empty;
        }

        LoadRoots();
    }

    /// <summary>Library root entries displayed in the setup dialog.</summary>
    public ObservableCollection<LibraryRootEntry> Entries { get; } = [];

    /// <summary>Title text shown in the dialog header.</summary>
    public string TitleText
    {
        get => _titleText;
        private set => SetProperty(ref _titleText, value);
    }
    private string _titleText = string.Empty;

    /// <summary>Subtitle text shown below the title.</summary>
    public string SubtitleText
    {
        get => _subtitleText;
        private set => SetProperty(ref _subtitleText, value);
    }
    private string _subtitleText = string.Empty;

    /// <summary>Tip text shown below the subtitle (empty when not applicable).</summary>
    public string TipText
    {
        get => _tipText;
        private set => SetProperty(ref _tipText, value);
    }
    private string _tipText = string.Empty;

    /// <summary>Whether to enable online metadata lookups (PCGW, Steam).</summary>
    public bool EnableOnlineMetadata
    {
        get => _enableOnlineMetadata;
        set => SetProperty(ref _enableOnlineMetadata, value);
    }
    private bool _enableOnlineMetadata;

    private void LoadRoots()
    {
        Entries.Clear();
        AppConfig config = _configService.Load();
        foreach (LibraryRoot root in config.LibraryRoots)
        {
            IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(root.RootPath);
            Entries.Add(new LibraryRootEntry(root.RootPath, root.DefaultType.ToString(), games.Count)
            {
                IsScanned = true
            });
        }
    }

    /// <summary>Opens a folder picker, scans the folder, and adds it as a library root.</summary>
    public async Task AddRootAsync()
    {
        ScanStatus = string.Empty;

        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Root", AllowMultiple = false });

        if (folders.Count == 0) return;
        string rawPath = folders[0].Path.LocalPath;
        string path = LibraryManager.NormalizeLibraryRoot(rawPath);

        if (Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        // Nesting check: reject if this path is inside an existing root or contains one
        foreach (var existing in Entries)
        {
            if (LibraryManager.IsChildOf(path, existing.Path))
            {
                string existingName = Path.GetFileName(existing.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                ScanStatus = $"This folder is inside an existing library root ({existingName}). Pick one or the other.";
                OnPropertyChanged(nameof(ScanStatus));
                return;
            }
            if (LibraryManager.IsChildOf(existing.Path, path))
            {
                string existingName = Path.GetFileName(existing.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                ScanStatus = $"An existing library root ({existingName}) is inside this folder. Remove it first if you want to add the parent.";
                OnPropertyChanged(nameof(ScanStatus));
                return;
            }
        }

        GameSourceKind defaultType = GameSourceParser.InferFromPath(path);
        var entry = new LibraryRootEntry(path, defaultType.ToString(), 0);
        Entries.Add(entry);

        bool added = await ScanAndSaveAsync(path, defaultType, entry);
        if (!added)
        {
            // Remove the entry if no games were found
            Entries.Remove(entry);
        }
    }

    /// <summary>Rescans a library root for games and updates the entry's game count.</summary>
    public async Task RescanAsync(LibraryRootEntry entry)
    {
        GameSourceKind type = GameSourceParser.ParseFromString(entry.DefaultType);
        await ScanAndSaveAsync(entry.Path, type, entry);
    }

    /// <summary>Removes a library root from the database, config, and UI.</summary>
    public void RemoveEntry(LibraryRootEntry entry)
    {
        Entries.Remove(entry);
        _dbService.RemoveRoot(entry.Path);

        AppConfig config = _configService.Load();
        var newRoots = config.LibraryRoots
            .Where(r => !r.RootPath.Equals(entry.Path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configService.Save(config with { LibraryRoots = newRoots });
    }

    /// <summary>Closes the setup dialog and persists the metadata toggle.</summary>
    public void Close()
    {
        // Persist the online metadata toggle
        AppConfig config = _configService.Load();
        if (config.EnableOnlineMetadata != _enableOnlineMetadata)
        {
            _configService.Save(config with { EnableOnlineMetadata = _enableOnlineMetadata });
        }

        _window.Close();
    }

    /// <summary>Status message shown in the dialog (e.g., rejection reason).</summary>
    public string ScanStatus
    {
        get => _scanStatus;
        private set => SetProperty(ref _scanStatus, value);
    }
    private string _scanStatus = string.Empty;

    /// <summary>
    /// Scans a folder and saves it as a library root.
    /// Returns true if the root was added, false if the folder was empty.
    /// Updates entry scan progress badges.
    /// </summary>
    private async Task<bool> ScanAndSaveAsync(string path, GameSourceKind defaultType, LibraryRootEntry entry)
    {
        entry.IsScanning = true;

        // LibraryManager handles scanner routing (FolderScanner vs SteamLibraryScanner)
        bool added = await Task.Run(() => _libraryManager.AddRoot(path, defaultType, []));

        IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(path);
        entry.GameCount = games.Count;
        entry.IsScanning = false;
        entry.IsScanned = true;

        return added;
    }
}
