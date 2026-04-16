using System.Text.Json;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

public sealed class GamesDatabaseService : IGamesDatabaseService
{
    private readonly string _dbPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public GamesDatabaseService(string dbPath)
    {
        _dbPath = dbPath;
    }

    public GamesDatabase Load()
    {
        if (!File.Exists(_dbPath))
            return new GamesDatabase(Roots: []);

        try
        {
            string json = File.ReadAllText(_dbPath);
            var dto = JsonSerializer.Deserialize<GamesDatabaseDto>(json, JsonOptions);
            if (dto is null) return new GamesDatabase(Roots: []);

            return new GamesDatabase(
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
        }
        catch
        {
            return new GamesDatabase(Roots: []);
        }
    }

    public void Save(GamesDatabase db)
    {
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
                    Override = g.Override,
                    ExecutablePath = g.ExecutablePath,
                    LauncherPath = g.LauncherPath,
                    CmdlineArgs = g.CmdlineArgs,
                    ManifestPath = g.ManifestPath,
                    LastScanned = g.LastScanned,
                    LastModified = g.LastModified,
                    Extra = g.Extra,
                }).ToList(),
            }).ToList(),
        };

        string json = JsonSerializer.Serialize(dto, JsonOptions);
        string? dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_dbPath, json);
    }

    public IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath)
    {
        GamesDatabase db = Load();
        return db.Roots
            .FirstOrDefault(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))?
            .Games ?? [];
    }

    public void AddRoot(string rootPath, GameSourceKind defaultType, IEnumerable<GameEntry> games)
    {
        GamesDatabase db = Load();
        if (db.Roots.Any(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)))
            return;

        var roots = db.Roots.ToList();
        roots.Add(new GameRoot(rootPath, defaultType, games.ToList()));
        Save(new GamesDatabase(roots));
    }

    public void RemoveRoot(string rootPath)
    {
        GamesDatabase db = Load();
        var roots = db.Roots
            .Where(r => !r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save(new GamesDatabase(roots));
    }

    public void RescanRoot(string rootPath, IEnumerable<GameEntry> games)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int idx = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;

        var existing = roots[idx];
        roots[idx] = existing with { Games = games.ToList() };
        Save(new GamesDatabase(roots));
    }

    public void UpdateGameEntry(string rootPath, GameEntry updated)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int idx = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;

        var games = roots[idx].Games.ToList();
        int gIdx = games.FindIndex(g => g.Id == updated.Id);
        if (gIdx < 0) return;

        games[gIdx] = updated;
        roots[idx] = roots[idx] with { Games = games };
        Save(new GamesDatabase(roots));
    }

    public void DeleteGameEntry(string rootPath, string gameId)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int idx = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;

        var games = roots[idx].Games.Where(g => g.Id != gameId).ToList();
        roots[idx] = roots[idx] with { Games = games };
        Save(new GamesDatabase(roots));
    }

    public void RetagGame(string rootPath, string gameId, GameSourceKind newType)
    {
        GamesDatabase db = Load();
        var roots = db.Roots.ToList();
        int idx = roots.FindIndex(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;

        var games = roots[idx].Games.ToList();
        int gIdx = games.FindIndex(g => g.Id == gameId);
        if (gIdx < 0) return;

        bool isOverride = newType != roots[idx].DefaultType;
        games[gIdx] = games[gIdx] with { GameSource = newType, Override = isOverride };
        roots[idx] = roots[idx] with { Games = games };
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
