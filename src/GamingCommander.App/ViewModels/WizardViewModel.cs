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
    ];

    public string ScanStatus
    {
        get => _scanStatus;
        private set => SetProperty(ref _scanStatus, value);
    }
    private string _scanStatus = string.Empty;

    public async Task AddEntryAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Folder" });
        if (folders.Count == 0) return;

        string path = folders[0].Path.LocalPath;
        if (Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        GameSourceKind defaultType = InferType(path);
        var entry = new WizardLibraryEntry(path, defaultType.ToString());
        Entries.Add(entry);
    }

    public async Task ScanEntryAsync(WizardLibraryEntry entry)
    {
        if (entry.IsScanning) return;
        entry.IsScanning = true;
        ScanStatus = $"Scanning {entry.Path}...";
        OnPropertyChanged(nameof(ScanStatus));

        await Task.Run(() =>
        {
            GameSourceKind type = ParseType(entry.SelectedType);
            IReadOnlyList<GameEntry> games = _scanner.Scan(entry.Path, type);
            _dbService.AddRoot(entry.Path, type, games);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                entry.GameCount = games.Count;
                entry.IsScanned = true;
                entry.IsScanning = false;
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
            .Select(e => new LibraryRoot(e.Path, ParseType(e.SelectedType)))
            .ToList();

        AppConfig current = _configService.Load();
        AppConfig config = new AppConfig(roots, [], current.HiddenFolders, IsFirstRun: false);
        _configService.Save(config);

        _window.Close(true);
    }

    public void Cancel()
    {
        var roots = Entries
            .Where(e => e.IsScanned)
            .Select(e => new LibraryRoot(e.Path, ParseType(e.SelectedType)))
            .ToList();

        AppConfig current = _configService.Load();
        AppConfig config = new AppConfig(roots, [], current.HiddenFolders, IsFirstRun: false);
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
                var entry = new WizardLibraryEntry(path, InferType(path).ToString());
                Entries.Add(entry);
                _ = ScanEntryAsync(entry);
            }
        }
    }

    private static GameSourceKind InferType(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("steam")) return GameSourceKind.Steam;
        if (lower.Contains("epic")) return GameSourceKind.Epic;
        if (lower.Contains("gog")) return GameSourceKind.Gog;
        if (lower.Contains("ea ") || lower.Contains("electronic arts")) return GameSourceKind.EaApp;
        if (lower.Contains("ubisoft")) return GameSourceKind.UbisoftConnect;
        return GameSourceKind.Standalone;
    }

    private static GameSourceKind ParseType(string type) => type switch
    {
        "Steam" => GameSourceKind.Steam,
        "GOG" => GameSourceKind.Gog,
        "Epic" => GameSourceKind.Epic,
        "EA App" => GameSourceKind.EaApp,
        "Ubisoft Connect" => GameSourceKind.UbisoftConnect,
        _ => GameSourceKind.Standalone,
    };
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
