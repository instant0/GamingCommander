using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.ViewModels;

public sealed class LibrarySetupViewModel : GamingCommander.UI.ViewModels.ReactiveObject
{
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _dbService;
    private readonly ILibraryManager _libraryManager;
    private readonly FolderScanner _scanner;
    private readonly Window _window;

    public LibrarySetupViewModel(
        IConfigService configService,
        IGamesDatabaseService dbService,
        ILibraryManager libraryManager,
        FolderScanner scanner,
        Window window)
    {
        _configService = configService;
        _dbService = dbService;
        _libraryManager = libraryManager;
        _scanner = scanner;
        _window = window;
        LoadRoots();
    }

    public ObservableCollection<LibraryRootEntry> Entries { get; } = [];

    public string[] AvailableTypes { get; } =
    [
        "Standalone",
        "Steam",
        "GOG",
        "Epic",
        "EA App",
        "Ubisoft Connect",
    ];

    private void LoadRoots()
    {
        Entries.Clear();
        AppConfig config = _configService.Load();
        foreach (LibraryRoot root in config.LibraryRoots)
        {
            IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(root.Path);
            Entries.Add(new LibraryRootEntry(root.Path, root.DefaultType.ToString(), games.Count));
        }
    }

    public async Task AddRootAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Root", AllowMultiple = false });

        if (folders.Count == 0) return;
        string path = folders[0].Path.LocalPath;

        if (Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        GameSourceKind defaultType = InferType(path);
        Entries.Add(new LibraryRootEntry(path, defaultType.ToString(), 0));

        await ScanAndSaveAsync(path, defaultType);
    }

    public async Task RescanAsync(LibraryRootEntry entry)
    {
        GameSourceKind type = ParseType(entry.DefaultType);
        await ScanAndSaveAsync(entry.Path, type);
        IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(entry.Path);
        entry.GameCount = games.Count;
    }

    public void RemoveEntry(LibraryRootEntry entry)
    {
        Entries.Remove(entry);
        _dbService.RemoveRoot(entry.Path);

        AppConfig config = _configService.Load();
        var newRoots = config.LibraryRoots
            .Where(r => !r.Path.Equals(entry.Path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configService.Save(new AppConfig(newRoots, config.FolderOverrides, config.IsFirstRun));
    }

    public void Close()
    {
        _window.Close();
    }

    private async Task ScanAndSaveAsync(string path, GameSourceKind defaultType)
    {
        IReadOnlyList<GameEntry> games = await Task.Run(() => _scanner.Scan(path, defaultType));

        _dbService.AddRoot(path, defaultType, games);

        AppConfig config = _configService.Load();
        var roots = config.LibraryRoots.ToList();
        int idx = roots.FindIndex(r => r.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            roots.Add(new LibraryRoot(path, defaultType));
        else
            roots[idx] = new LibraryRoot(path, defaultType);

        _configService.Save(config with { LibraryRoots = roots });

        var entry = Entries.FirstOrDefault(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (entry != null) entry.GameCount = games.Count;
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

    public sealed class LibraryRootEntry : GamingCommander.UI.ViewModels.ReactiveObject
{
    public LibraryRootEntry(string path, string defaultType, int gameCount)
    {
        Path = path;
        _defaultType = defaultType;
        DefaultType = defaultType;
        GameCount = gameCount;
    }

    public string Path { get; }
    public int GameCount { get; set; }

    public string DefaultType
    {
        get => _defaultType;
        set => SetProperty(ref _defaultType, value);
    }
    private string _defaultType = string.Empty;
}
