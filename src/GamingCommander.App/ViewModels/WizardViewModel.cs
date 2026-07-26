using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.ViewModels;

/// <summary>
/// ViewModel for the first-run wizard. Scans configured library folders,
/// presents results, and saves initial configuration.
/// </summary>
public sealed class WizardViewModel : GamingCommander.UI.ViewModels.ReactiveObject
{
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _dbService;
    private readonly FolderScanner _scanner;
    private readonly Window _window;

    public WizardViewModel(
        IConfigService configService,
        IGamesDatabaseService dbService,
        Window window,
        BlacklistData? blacklist = null)
    {
        _configService = configService;
        _dbService = dbService;
        blacklist ??= BlacklistData.Empty;
        _scanner = new FolderScanner(configService.Load().HiddenFolders, blacklist);
        _window = window;
    }

    /// <summary>Library entries added by the user during the wizard.</summary>
    public ObservableCollection<WizardLibraryEntry> Entries { get; } = [];

    /// <summary>Status text displayed during scanning (e.g., "Scanning D:\Games...").</summary>
    public string ScanStatus
    {
        get => _scanStatus;
        private set => SetProperty(ref _scanStatus, value);
    }
    private string _scanStatus = string.Empty;

    /// <summary>Whether to enable online metadata lookup (PCGamingWiki, etc.).</summary>
    public bool EnableOnlineMetadata
    {
        get => _enableOnlineMetadata;
        set => SetProperty(ref _enableOnlineMetadata, value);
    }
    private bool _enableOnlineMetadata;

    /// <summary>Opens a folder picker and adds the selected folder as a library entry.</summary>
    public async Task AddEntryAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Folder" });
        if (folders.Count == 0) return;

        string rawPath = folders[0].Path.LocalPath;
        string path = LibraryManager.NormalizeLibraryRoot(rawPath);
        if (Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        // Nesting check: reject if this path is inside an existing entry or contains one
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
        var entry = new WizardLibraryEntry(path, defaultType.ToString());
        Entries.Add(entry);
        await ScanEntryAsync(entry);
    }

    /// <summary>Scans a library entry for games using the appropriate scanner.</summary>
    public async Task ScanEntryAsync(WizardLibraryEntry entry)
    {
        if (entry.IsScanning) return;
        entry.IsScanning = true;
        ScanStatus = $"Scanning {entry.Path}...";
        OnPropertyChanged(nameof(ScanStatus));

        await Task.Run(() =>
        {
            GameSourceKind selectedType = GameSourceParser.ParseFromString(entry.SelectedType);

            // Structural auto-detect: if it looks like a Steam library, use SteamScanner
            // regardless of selected type. This handles the common case where the user
            // adds "D:\Games" which happens to be a Steam library.
            bool isSteamLibrary = LibraryManager.LooksLikeSteamLibrary(entry.Path);
            GameSourceKind effectiveType = isSteamLibrary ? GameSourceKind.Steam : selectedType;

            IReadOnlyList<GameEntry> games;
            if (effectiveType == GameSourceKind.Steam)
            {
                var steamScanner = new SteamLibraryScanner([entry.Path]);
                games = steamScanner.Scan(entry.Path);
            }
            else
            {
                games = _scanner.Scan(entry.Path, effectiveType);
            }
            _dbService.AddRoot(entry.Path, effectiveType, games);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                entry.GameCount = games.Count;
                entry.IsScanned = true;
                entry.IsScanning = false;
                entry.SelectedType = effectiveType.ToString();
                ScanStatus = $"Scanned {games.Count} games in {entry.Path}";
                OnPropertyChanged(nameof(ScanStatus));
            });
        });
    }

    /// <summary>Removes a library entry from the wizard.</summary>
    public void RemoveEntry(WizardLibraryEntry entry)
    {
        Entries.Remove(entry);
    }

    /// <summary>Saves all entries as library roots and closes the wizard.</summary>
    public void Finish()
    {
        var roots = Entries
            .Select(e => new LibraryRoot(e.Path, GameSourceParser.ParseFromString(e.SelectedType)))
            .ToList();

        AppConfig current = _configService.Load();
        AppConfig config = new AppConfig(roots, [], current.HiddenFolders,
            IsFirstRun: false, current.LastSeenVersion, EnableOnlineMetadata);
        _configService.Save(config);

        _window.Close(true);
    }

    /// <summary>Saves only scanned entries as library roots and closes the wizard.</summary>
    public void Cancel()
    {
        var roots = Entries
            .Where(e => e.IsScanned)
            .Select(e => new LibraryRoot(e.Path, GameSourceParser.ParseFromString(e.SelectedType)))
            .ToList();

        AppConfig current = _configService.Load();
        AppConfig config = new AppConfig(roots, [], current.HiddenFolders,
            IsFirstRun: false, current.LastSeenVersion, EnableOnlineMetadata);
        _configService.Save(config);

        _window.Close(false);
    }
}
