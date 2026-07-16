using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.ViewModels;

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

    public ObservableCollection<WizardLibraryEntry> Entries { get; } = [];

    public string[] AvailableTypes { get; } =
    [
        "Standalone",
        "Steam",
        "GOG",
        "Epic",
        "EA App",
        "Ubisoft Connect",
        "Battle.net",
        "Xbox",
        "Rockstar",
        "Steam Emulator",
    ];

    public string ScanStatus
    {
        get => _scanStatus;
        private set => SetProperty(ref _scanStatus, value);
    }
    private string _scanStatus = string.Empty;

    public bool EnableOnlineMetadata
    {
        get => _enableOnlineMetadata;
        set => SetProperty(ref _enableOnlineMetadata, value);
    }
    private bool _enableOnlineMetadata;

    public async Task AddEntryAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Folder" });
        if (folders.Count == 0) return;

        string rawPath = folders[0].Path.LocalPath;
        string path = LibraryManager.NormalizeLibraryRoot(rawPath);
        if (Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        GameSourceKind defaultType = GameSourceParser.InferFromPath(path);
        var entry = new WizardLibraryEntry(path, defaultType.ToString());
        Entries.Add(entry);
        await ScanEntryAsync(entry);
    }

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

    public void RemoveEntry(WizardLibraryEntry entry)
    {
        Entries.Remove(entry);
    }

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

    private void AddRecommendedPaths()
    {
        string[] recommended =
        [
            @"D:\Games",
            @"E:\Games",
            @"C:\Games",
            @"D:\SteamLibrary",
        ];

        foreach (string path in recommended)
        {
            if (Directory.Exists(path) &&
                !Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                var entry = new WizardLibraryEntry(path, GameSourceParser.InferFromPath(path).ToString());
                Entries.Add(entry);
                _ = ScanEntryAsync(entry);
            }
        }
    }
}

    public sealed class WizardLibraryEntry : GamingCommander.UI.ViewModels.ReactiveObject
{
    public WizardLibraryEntry(string path, string selectedType)
    {
        Path = path;
        _selectedType = selectedType;
        SelectedType = selectedType;
    }

    public string Path { get; }

    public string SelectedType
    {
        get => _selectedType;
        set => SetProperty(ref _selectedType, value);
    }
    private string _selectedType = string.Empty;

    public int GameCount
    {
        get => _gameCount;
        set => SetProperty(ref _gameCount, value);
    }
    private int _gameCount;

    public bool IsScanned
    {
        get => _isScanned;
        set => SetProperty(ref _isScanned, value);
    }
    private bool _isScanned;

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }
    private bool _isScanning;
}
