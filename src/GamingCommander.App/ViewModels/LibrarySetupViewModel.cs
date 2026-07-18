using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.ViewModels;

/// <summary>
/// ViewModel for the F2 Library Setup dialog. Manages adding, removing, and rescanning library roots.
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
        Window window)
    {
        _configService = configService;
        _dbService = dbService;
        _libraryManager = libraryManager;
        _window = window;
        LoadRoots();
    }

    /// <summary>Library root entries displayed in the setup dialog.</summary>
    public ObservableCollection<LibraryRootEntry> Entries { get; } = [];

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

    /// <summary>Opens a folder picker, scans the folder, and adds it as a library root.</summary>
    public async Task AddRootAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Library Root", AllowMultiple = false });

        if (folders.Count == 0) return;
        string rawPath = folders[0].Path.LocalPath;
        string path = LibraryManager.NormalizeLibraryRoot(rawPath);

        if (Entries.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        GameSourceKind defaultType = GameSourceParser.InferFromPath(path);
        Entries.Add(new LibraryRootEntry(path, defaultType.ToString(), 0));

        await ScanAndSaveAsync(path, defaultType);
    }

    /// <summary>Rescans a library root for games and updates the entry's game count.</summary>
    public async Task RescanAsync(LibraryRootEntry entry)
    {
        GameSourceKind type = GameSourceParser.ParseFromString(entry.DefaultType);
        await ScanAndSaveAsync(entry.Path, type);
        IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(entry.Path);
        entry.GameCount = games.Count;
    }

    /// <summary>Removes a library root from the database, config, and UI.</summary>
    public void RemoveEntry(LibraryRootEntry entry)
    {
        Entries.Remove(entry);
        _dbService.RemoveRoot(entry.Path);

        AppConfig config = _configService.Load();
        var newRoots = config.LibraryRoots
            .Where(r => !r.Path.Equals(entry.Path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configService.Save(config with { LibraryRoots = newRoots });
    }

    /// <summary>Closes the setup dialog.</summary>
    public void Close()
    {
        _window.Close();
    }

    private async Task ScanAndSaveAsync(string path, GameSourceKind defaultType)
    {
        // LibraryManager handles scanner routing (FolderScanner vs SteamLibraryScanner)
        await Task.Run(() => _libraryManager.AddRoot(path, defaultType, []));

        IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(path);

        var entry = Entries.FirstOrDefault(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (entry != null) entry.GameCount = games.Count;
    }
}
