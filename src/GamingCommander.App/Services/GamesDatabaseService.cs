using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of IGamesDatabaseService with in-memory caching.
/// Reads/writes data/games.json using DTO mapping.
/// </summary>
public sealed class GamesDatabaseService : IGamesDatabaseService
{
    private readonly string _dbPath;
    private GamesDatabase? _cachedDb;

    /// <summary>Creates a new database service targeting the specified JSON file path.</summary>
    public GamesDatabaseService(string dbPath)
    {
        _dbPath = dbPath;
    }

    /// <summary>Loads the games database from disk. Returns cached version if already loaded.</summary>
    public GamesDatabase Load()
    {
        if (_cachedDb is not null)
            return _cachedDb;

        GamesDatabaseDto? dto = JsonFileHelper.ReadFromFile<GamesDatabaseDto>(
            _dbPath,
            () => new GamesDatabaseDto { Roots = [] });
        if (dto is null)
        {
            _cachedDb = new GamesDatabase(Roots: []);
            return _cachedDb;
        }

        _cachedDb = new GamesDatabase(
            dto.Roots?
                .Select(r => new GameRoot(
                    r.RootPath,
                    r.DefaultType,
                    r.Games?
                        .Select(g => new GameEntry(
                            g.Id,
                            g.FolderName,
                            g.DisplayName,
                            g.GameSource,
                            g.Override,
                            g.ExecutablePath,
                            g.LauncherPath,
                            g.CmdlineArgs,
                            g.ManifestPath,
                            g.LastScanned,
                            g.LastModified,
                            g.Extra ?? []))
                        .ToList() ?? []))
                .ToList() ?? []);
        return _cachedDb;
    }

    /// <summary>Serializes and persists the games database to disk. Updates the in-memory cache.</summary>
    public void Save(GamesDatabase db)
    {
        // Update cache first, then persist to disk
        _cachedDb = db;

        var dto = new GamesDatabaseDto
        {
            Roots = db.Roots.Select(r => new GameRootDto
            {
                RootPath = r.RootPath,
                DefaultType = r.DefaultType,
                Games = r.Games.Select(g => new GameEntryDto
                {
                    Id = g.Id,
                    FolderName = g.FolderName,
                    DisplayName = g.DisplayName,
                    GameSource = g.GameSource,
                    Override = g.IsSourceOverridden,
                    ExecutablePath = g.ExecutablePath,
                    LauncherPath = g.LauncherPath,
                    CmdlineArgs = g.CommandLineArguments,
                    ManifestPath = g.ManifestPath,
                    LastScanned = g.LastScanned,
                    LastModified = g.LastModified,
                    Extra = g.PlatformMetadata,
                }).ToList(),
            }).ToList(),
        };

        JsonFileHelper.WriteToFile(_dbPath, dto);
    }

    /// <summary>Returns all game entries for the specified library root path.</summary>
    public IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath)
    {
        GamesDatabase db = Load();
        return db.Roots
            .FirstOrDefault(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))?
            .Games ?? [];
    }

    /// <summary>Adds a new library root with its game entries to the database.</summary>
    public void AddRoot(string rootPath, GameSourceKind defaultType, IEnumerable<GameEntry> initialGames)
    {
        GamesDatabase db = Load();
        if (db.Roots.Any(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)))
            return;

        var roots = db.Roots.ToList();
        roots.Add(new GameRoot(rootPath, defaultType, initialGames.ToList()));
        Save(new GamesDatabase(roots));
    }

    /// <summary>Removes a library root and all associated game entries.</summary>
    public void RemoveRoot(string rootPath)
    {
        GamesDatabase db = Load();
        var roots = db.Roots
            .Where(r => !r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save(new GamesDatabase(roots));
    }

    /// <summary>Replaces all entries for a root with freshly scanned results.</summary>
    public void RescanRoot(string rootPath, IEnumerable<GameEntry> games)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int rootIndex = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (rootIndex < 0) return;

        var existing = roots[rootIndex];
        roots[rootIndex] = existing with { Games = games.ToList() };
        Save(new GamesDatabase(roots));
    }

    /// <summary>Updates a single game entry within the specified root.</summary>
    public void UpdateGameEntry(string rootPath, GameEntry updatedEntry)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int rootIndex = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (rootIndex < 0) return;

        var games = roots[rootIndex].Games.ToList();
        int gameIndex = games.FindIndex(g => g.Id == updatedEntry.Id);
        if (gameIndex < 0) return;

        games[gameIndex] = updatedEntry;
        roots[rootIndex] = roots[rootIndex] with { Games = games };
        Save(new GamesDatabase(roots));
    }

    /// <summary>Removes a game entry by ID from the specified root.</summary>
    public void DeleteGameEntry(string rootPath, string gameId)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int rootIndex = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (rootIndex < 0) return;

        var games = roots[rootIndex].Games.Where(g => g.Id != gameId).ToList();
        roots[rootIndex] = roots[rootIndex] with { Games = games };
        Save(new GamesDatabase(roots));
    }

    /// <summary>Changes the source type of a game entry.</summary>
    public void RetagGame(string rootPath, string gameId, GameSourceKind newType)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int rootIndex = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (rootIndex < 0) return;

        var games = roots[rootIndex].Games.ToList();
        int gameIndex = games.FindIndex(g => g.Id == gameId);
        if (gameIndex < 0) return;

        bool isOverride = newType != roots[rootIndex].DefaultType;
        games[gameIndex] = games[gameIndex] with { GameSource = newType, IsSourceOverridden = isOverride };
        roots[rootIndex] = roots[rootIndex] with { Games = games };
        Save(new GamesDatabase(roots));
    }

    private sealed class GamesDatabaseDto
    {
        public List<GameRootDto>? Roots { get; set; }
    }

    private sealed class GameRootDto
    {
        public string RootPath { get; set; } = string.Empty;
        public GameSourceKind DefaultType { get; set; }
        public List<GameEntryDto>? Games { get; set; }
    }

    private sealed class GameEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public GameSourceKind GameSource { get; set; }
        public bool Override { get; set; }
        public string ExecutablePath { get; set; } = string.Empty;
        public string LauncherPath { get; set; } = string.Empty;
        public string CmdlineArgs { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;
        public DateTimeOffset LastScanned { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public Dictionary<string, string> Extra { get; set; } = [];
    }
}
