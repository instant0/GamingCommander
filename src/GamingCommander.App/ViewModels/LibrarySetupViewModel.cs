using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.ViewModels;

/// <summary>
/// ViewModel for the unified Library Setup dialog (F2). Manages adding, removing,
/// and rescanning libraries (anchors). An anchor is a named catalog owning one or
/// more physical folders; for Standalone libraries the anchor name IS the folder
/// path, while platform anchors (Steam, Epic, ...) aggregate many physical folders
/// under one displayed anchor. Handles both first-run onboarding and ongoing management.
/// </summary>
public sealed class LibrarySetupViewModel : GamingCommander.UI.ViewModels.ReactiveObject
{
    private readonly IGamesDatabaseService _dbService;
    private readonly ILibraryManager _libraryManager;
    private readonly Window _window;
    private readonly SteamInstallPathLocator _steamLocator;

    public LibrarySetupViewModel(
        IConfigService configService,
        IGamesDatabaseService dbService,
        ILibraryManager libraryManager,
        Window window,
        SteamInstallPathLocator? steamLocator = null,
        bool isFirstRun = false)
    {
        _configService = configService;
        _dbService = dbService;
        _libraryManager = libraryManager;
        _window = window;
        _steamLocator = steamLocator ?? new SteamInstallPathLocator(new NullRegistryReader());

        // Load metadata toggle from config
        AppConfig config = _configService.Load();
        _enableOnlineMetadata = config.EnableOnlineMetadata;

        // Set title/subtitle based on context
        if (isFirstRun && _libraryManager.Libraries.Count == 0)
        {
            _titleText = "Welcome to GamingCommander";
            _subtitleText = "Add your game libraries below. For each library, GamingCommander will scan its folder(s) and find your games. Select the platform type — this becomes the anchor that the discovered games belong to.";
            _tipText = "Tip: Steam libraries should be added via the 'Add Steam' button; it reads every Steam library from your install.";
        }
        else
        {
            _titleText = "Library Setup";
            _subtitleText = "Add, remove, or rescan libraries (anchors). Changes apply immediately.";
            _tipText = string.Empty;
        }

        LoadLibraries();
    }

    private readonly IConfigService _configService;

    /// <summary>Library (anchor) entries displayed in the setup dialog.</summary>
    public ObservableCollection<LibraryEntry> Entries { get; } = [];

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

    private void LoadLibraries()
    {
        Entries.Clear();
        foreach (Library lib in _libraryManager.Libraries)
        {
            IReadOnlyList<GameEntry> games = _dbService.GetGamesForLibrary(lib.Name);
            Entries.Add(new LibraryEntry(lib.Name, GameSourceParser.ToDisplayName(lib.Type), games.Count)
            {
                IsScanned = true,
                FolderCount = lib.Folders.Count,
            });
        }
    }

    /// <summary>True when ProgramData Manifests exist and are not already part of an Epic anchor.</summary>
    public bool CanAddEpicCatalog
    {
        get
        {
            string dir = EpicManifestPaths.DefaultManifestsDir;
            if (!EpicItemCatalog.LooksLikeManifestsDir(dir))
                return false;
            return !Entries.Any(e => e.DefaultType.Equals(
                GameSourceParser.ToDisplayName(GameSourceKind.Epic), StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Add the default Epic Manifests folder as a single "Epic" library (anchor).</summary>
    public async Task AddEpicCatalogAsync()
    {
        string dir = EpicManifestPaths.DefaultManifestsDir;
        if (!CanAddEpicCatalog)
        {
            ScanStatus = "Epic Manifests folder not found, or already added.";
            return;
        }

        var entry = await AddLibraryAsync(
            "Epic Games Store", GameSourceKind.Epic, [dir]);
        if (entry != null)
        {
            Entries.Add(entry);
            OnPropertyChanged(nameof(CanAddEpicCatalog));
        }
        else
        {
            ScanStatus = "No Epic games were found in the Manifests folder.";
        }
    }

    /// <summary>
    /// True when Steam is installed (registry InstallPath resolves) AND libraryfolders.vdf
    /// yields ≥1 library, AND a Steam anchor is not already configured.
    /// </summary>
    public bool CanAddSteamLibraries
    {
        get
        {
            if (!_steamLocator.IsSteamAvailable)
                return false;
            return !Entries.Any(e =>
                e.DefaultType.Equals(GameSourceParser.ToDisplayName(GameSourceKind.Steam), StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Adds ONE "Steam" library anchor owning every vdf-discovered physical library,
    /// then scans all of them into that single anchor. Each game keeps its real
    /// FolderPath; the anchor "Steam" is the top-level displayed catalog.
    /// </summary>
    public async Task AddSteamLibrariesAsync()
    {
        if (!CanAddSteamLibraries)
        {
            ScanStatus = "Steam not found (no registry key), or already added.";
            return;
        }

        string? installPath = _steamLocator.FindInstallPath();
        if (installPath is null)
        {
            ScanStatus = "Steam install path not found in registry.";
            return;
        }

        var libraries = _steamLocator.FindAllLibraries();
        if (libraries.Count == 0)
        {
            ScanStatus = "No Steam libraries found in libraryfolders.vdf.";
            return;
        }

        var entry = await AddLibraryAsync("Steam", GameSourceKind.Steam, libraries);
        if (entry != null)
        {
            Entries.Add(entry);
            OnPropertyChanged(nameof(CanAddSteamLibraries));
        }
        else
        {
            ScanStatus = "No Steam games were found across the discovered libraries.";
        }
    }

    /// <summary>Opens a folder picker and adds it as a new Standalone library anchor.</summary>
    public async Task AddLibraryFolderAsync()
    {
        ScanStatus = string.Empty;

        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Folder", AllowMultiple = false });

        if (folders.Count == 0) return;
        string path = LibraryManager.NormalizeLibraryRoot(folders[0].Path.LocalPath);

        // Standalone: the anchor name IS the folder path.
        if (Entries.Any(e => e.Name.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        // Nesting check: reject if this path is inside an existing anchor folder or contains one
        foreach (var existing in Entries)
        {
            if (LibraryManager.IsChildOf(path, existing.Name))
            {
                string existingName = Path.GetFileName(existing.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                ScanStatus = $"This folder is inside an existing library ({existingName}). Pick one or the other.";
                OnPropertyChanged(nameof(ScanStatus));
                return;
            }
            if (LibraryManager.IsChildOf(existing.Name, path))
            {
                string existingName = Path.GetFileName(existing.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                ScanStatus = $"An existing library ({existingName}) is inside this folder. Remove it first if you want to add the parent.";
                OnPropertyChanged(nameof(ScanStatus));
                return;
            }
        }

        GameSourceKind type = GameSourceParser.InferFromPath(path);
        var entry = await AddLibraryAsync(path, type, [path]);
        if (entry != null)
            Entries.Add(entry);
        else
            ScanStatus = "No games were found in that folder.";
    }

    /// <summary>Rescans all folders of a library anchor and updates the entry's game count.</summary>
    public async Task RescanAsync(LibraryEntry entry)
    {
        entry.IsScanning = true;
        await Task.Run(() => _libraryManager.RescanLibrary(entry.Name));
        IReadOnlyList<GameEntry> games = _dbService.GetGamesForLibrary(entry.Name);
        entry.GameCount = games.Count;
        entry.IsScanning = false;
        entry.IsScanned = true;
    }

    /// <summary>Removes a library anchor and its game entries from the database and UI.</summary>
    public void RemoveEntry(LibraryEntry entry)
    {
        Entries.Remove(entry);
        _libraryManager.RemoveLibrary(entry.Name);
        OnPropertyChanged(nameof(CanAddSteamLibraries));
        OnPropertyChanged(nameof(CanAddEpicCatalog));
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
    /// Creates (or updates) a library anchor with the given folders, scans every folder,
    /// and persists the games under that anchor. Returns the new entry when at least one
    /// game was found, otherwise removes the anchor and returns null.
    /// </summary>
    private async Task<LibraryEntry?> AddLibraryAsync(
        string name, GameSourceKind type, IReadOnlyList<string> folders)
    {
        var entry = new LibraryEntry(name, GameSourceParser.ToDisplayName(type), 0) { IsScanning = true };
        Entries.Add(entry);

        // Register the anchor, then scan all its folders into it.
        _libraryManager.UpsertLibrary(name, type, folders);
        try
        {
            await Task.Run(() => _libraryManager.RescanLibrary(name));
        }
        finally
        {
            entry.IsScanning = false;
        }

        IReadOnlyList<GameEntry> games = _dbService.GetGamesForLibrary(name);
        if (games.Count == 0)
        {
            Entries.Remove(entry);
            _libraryManager.RemoveLibrary(name);
            return null;
        }

        entry.GameCount = games.Count;
        entry.IsScanned = true;
        entry.FolderCount = folders.Count;
        return entry;
    }
}
