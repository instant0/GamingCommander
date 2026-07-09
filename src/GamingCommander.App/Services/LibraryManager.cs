using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Real implementation of ILibraryManager that reads library roots from
/// IConfigService directly (no stale in-memory copy), and uses FolderScanner
/// for game discovery.
/// </summary>
public sealed class LibraryManager : ILibraryManager
{
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _db;
    private readonly FolderScanner _scanner;

    public LibraryManager(
        IConfigService configService,
        IGamesDatabaseService db,
        FolderScanner scanner)
    {
        _configService = configService;
        _db = db;
        _scanner = scanner;
    }

    /// <summary>
    /// Roots are read live from persisted config — never stale.
    /// </summary>
    public IReadOnlyList<LibraryRoot> LibraryRoots =>
        _configService.Load().LibraryRoots;

    public IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath)
    {
        return _db.GetGamesForRoot(rootPath);
    }

    public void AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> games)
    {
        AppConfig config = _configService.Load();

        // If no games were provided, scan the folder to discover them
        IReadOnlyList<GameEntry> resolved = games;
        if (resolved.Count == 0 && Directory.Exists(rootPath))
            resolved = _scanner.Scan(rootPath, defaultType);

        // Persist to games database
        _db.AddRoot(rootPath, defaultType, resolved);

        // Persist root to config (append if not already present)
        var roots = config.LibraryRoots.ToList();
        if (!roots.Any(r => r.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase)))
        {
            roots.Add(new LibraryRoot(rootPath, defaultType));
            _configService.Save(config with { LibraryRoots = roots });
        }
    }

    public void RemoveRoot(string rootPath)
    {
        // Remove from games database
        _db.RemoveRoot(rootPath);

        // Remove from config
        AppConfig config = _configService.Load();
        var roots = config.LibraryRoots
            .Where(r => !r.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configService.Save(config with { LibraryRoots = roots });
    }

    /// <summary>
    /// Re-scans all configured library roots and updates the games database.
    /// Called at startup (if roots exist) or on explicit refresh.
    /// </summary>
    public void Refresh()
    {
        AppConfig config = _configService.Load();
        foreach (LibraryRoot root in config.LibraryRoots)
        {
            if (!Directory.Exists(root.Path))
                continue;

            IReadOnlyList<GameEntry> games = _scanner.Scan(root.Path, root.DefaultType);
            _db.RescanRoot(root.Path, games);
        }
    }

    public void RescanRoot(string rootPath, IReadOnlyList<GameEntry> games)
    {
        _db.RescanRoot(rootPath, games);
    }

    public void UpdateGameEntry(string rootPath, GameEntry updated)
    {
        _db.UpdateGameEntry(rootPath, updated);
    }

    public void DeleteGameEntry(string rootPath, string gameId)
    {
        _db.DeleteGameEntry(rootPath, gameId);
    }

    public void RetagGame(string rootPath, string gameId, GameSourceKind newType)
    {
        _db.RetagGame(rootPath, gameId, newType);
    }
}
