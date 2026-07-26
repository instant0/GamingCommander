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

    private void LoadRoots()
    {
        Entries.Clear();
        AppConfig config = _configService.Load();
        foreach (LibraryRoot root in config.LibraryRoots)
        {
            IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(root.RootPath);
            Entries.Add(new LibraryRootEntry(root.RootPath, root.DefaultType.ToString(), games.Count));
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
        Entries.Add(new LibraryRootEntry(path, defaultType.ToString(), 0));

        bool added = await ScanAndSaveAsync(path, defaultType);
        if (!added)
        {
            // Remove the entry if no games were found
            var entry = Entries.FirstOrDefault(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (entry != null) Entries.Remove(entry);
        }
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
            .Where(r => !r.RootPath.Equals(entry.Path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configService.Save(config with { LibraryRoots = newRoots });
    }

    /// <summary>Closes the setup dialog.</summary>
    public void Close()
    {
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
    /// </summary>
    private async Task<bool> ScanAndSaveAsync(string path, GameSourceKind defaultType)
    {
        // LibraryManager handles scanner routing (FolderScanner vs SteamLibraryScanner)
        bool added = await Task.Run(() => _libraryManager.AddRoot(path, defaultType, []));

        IReadOnlyList<GameEntry> games = _dbService.GetGamesForRoot(path);

        var entry = Entries.FirstOrDefault(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (entry != null) entry.GameCount = games.Count;

        return added;
    }
}
