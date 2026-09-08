using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of IGamesDatabaseService with in-memory caching.
/// Reads/writes data/games.json (flat games[] list); each game links to a
/// Library (anchor) by <see cref="GameEntry.Library"/>.
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
            () => new GamesDatabaseDto { Games = [] });
        if (dto is null)
        {
            _cachedDb = new GamesDatabase(Games: []);
            return _cachedDb;
        }

        _cachedDb = new GamesDatabase(
            (dto.Games ?? []).Select(ToEntry).ToList());
        return _cachedDb;
    }

    /// <summary>Serializes and persists the games database to disk. Updates the in-memory cache.</summary>
    public void Save(GamesDatabase db)
    {
        _cachedDb = db;
        var dto = new GamesDatabaseDto
        {
            Games = db.Games.Select(ToDto).ToList(),
        };
        JsonFileHelper.WriteToFile(_dbPath, dto);
    }

    /// <summary>Returns all game entries linked to the specified library (anchor) name.</summary>
    public IReadOnlyList<GameEntry> GetGamesForLibrary(string libraryName)
    {
        GamesDatabase db = Load();
        return db.Games
            .Where(g => g.Library.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Replaces all game entries for a library anchor while preserving user overrides.
    /// Every entry is stored under <paramref name="libraryName"/>, so callers never have
    /// to pre-stamp <see cref="GameEntry.Library"/> themselves.
    /// </summary>
    public void SetGamesForLibrary(string libraryName, IEnumerable<GameEntry> games)
    {
        GamesDatabase db = Load();
        var scanned = games.ToList();

        // Preserve overrides for games whose ID matches an existing entry.
        var existingByLibrary = db.Games
            .Where(g => g.Library.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(g => g.Id, StringComparer.OrdinalIgnoreCase);

        var merged = new List<GameEntry>();
        foreach (GameEntry g in scanned)
        {
            // Every stored entry belongs to this library anchor, regardless of what
            // the scan-context stamped. Anchor name is authoritative here.
            GameEntry anchored = g with { Library = libraryName };
            if (existingByLibrary.TryGetValue(g.Id, out GameEntry? existing))
                merged.Add(MergeGameEntries(existing, anchored) with { Library = libraryName });
            else
                merged.Add(anchored);
        }

        var keep = db.Games
            .Where(g => !g.Library.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        keep.AddRange(merged);

        Save(new GamesDatabase(keep));
    }

    /// <summary>Removes all game entries linked to the specified library name.</summary>
    public void RemoveGamesForLibrary(string libraryName)
    {
        GamesDatabase db = Load();
        var keep = db.Games
            .Where(g => !g.Library.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save(new GamesDatabase(keep));
    }

    /// <summary>Updates a single game entry by ID (case-insensitive).</summary>
    public void UpdateGameEntry(GameEntry updatedEntry)
    {
        GamesDatabase db = Load();
        var games = db.Games.ToList();
        int idx = games.FindIndex(g =>
            g.Id.Equals(updatedEntry.Id, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        games[idx] = updatedEntry;
        Save(new GamesDatabase(games));
    }

    /// <summary>Removes a game entry by ID (case-insensitive).</summary>
    public void DeleteGameEntry(string gameId)
    {
        GamesDatabase db = Load();
        var games = db.Games
            .Where(g => !g.Id.Equals(gameId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save(new GamesDatabase(games));
    }

    /// <summary>Changes the source type of a game entry without modifying other fields.</summary>
    public void RetagGame(string gameId, GameSourceKind newType)
    {
        GamesDatabase db = Load();
        var games = db.Games.ToList();
        int idx = games.FindIndex(g => g.Id.Equals(gameId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        games[idx] = games[idx] with { GameSource = newType, IsSourceOverridden = true };
        Save(new GamesDatabase(games));
    }

    // ════════════════════════════════════════════════════════════════
    //  Merge / DTO mapping
    // ════════════════════════════════════════════════════════════════

    private static GameEntry MergeGameEntries(GameEntry existing, GameEntry scanned)
    {
        // Preserve display name if user changed it (differs from auto-normalized folder name)
        string autoDetectedName = FileSystemHelper.NormalizeDisplayName(existing.FolderName);
        string displayName = existing.DisplayName != autoDetectedName
            ? existing.DisplayName
            : scanned.DisplayName;

        GameSourceKind gameSource = existing.IsSourceOverridden ? existing.GameSource : scanned.GameSource;

        string commandLineArgs = !string.IsNullOrEmpty(existing.CommandLineArguments)
            && existing.CommandLineArguments != scanned.CommandLineArguments
            ? existing.CommandLineArguments
            : scanned.CommandLineArguments;

        string launcherPath = !string.IsNullOrEmpty(existing.LauncherPath)
            ? existing.LauncherPath
            : scanned.LauncherPath;

        string manifestPath = !string.IsNullOrEmpty(existing.ManifestPath)
            ? existing.ManifestPath
            : scanned.ManifestPath;

        string executablePath = existing.UserOverrides.ContainsKey(GameEntryFields.ExecutablePath)
            ? existing.ExecutablePath
            : scanned.ExecutablePath;

        Dictionary<string, string> platformMetadata = new(scanned.PlatformMetadata);
        if (existing.UserOverrides.ContainsKey(GameEntryFields.ExecutablePath))
        {
            platformMetadata.Remove(PlatformMetadataKeys.ExeCandidates);
            platformMetadata.Remove(PlatformMetadataKeys.ExeCandidateCount);
        }

        // E8: preserve a title-pin marker through a rescan merge.
        if (existing.PlatformMetadata.TryGetValue(PlatformMetadataKeys.TitleSource, out string? titleSource)
            && titleSource is TitleSourceValues.PcgwPick or TitleSourceValues.UserOverride)
        {
            platformMetadata[PlatformMetadataKeys.TitleSource] = titleSource;
        }

        return scanned with
        {
            DisplayName = displayName,
            GameSource = gameSource,
            ExecutablePath = executablePath,
            PlatformMetadata = platformMetadata,
            CommandLineArguments = commandLineArgs,
            LauncherPath = launcherPath,
            ManifestPath = manifestPath,
            Tags = existing.Tags.Count > 0 ? existing.Tags : scanned.Tags,
            UserOverrides = existing.UserOverrides.Count > 0 ? existing.UserOverrides : scanned.UserOverrides,
            LastScanned = DateTimeOffset.UtcNow,
        };
    }

    private static GameEntry ToEntry(GameEntryDto dto)
    {
        return new GameEntry(
            dto.Id,
            dto.Library ?? string.Empty,
            dto.FolderPath ?? string.Empty,
            dto.FolderName ?? string.Empty,
            dto.DisplayName ?? string.Empty,
            dto.GameSource,
            dto.IsSourceOverridden,
            dto.ExecutablePath ?? string.Empty,
            dto.LauncherPath ?? string.Empty,
            dto.CommandLineArguments ?? string.Empty,
            dto.ManifestPath ?? string.Empty,
            dto.LastScanned,
            dto.LastModified,
            dto.PlatformMetadata ?? [],
            dto.Tags ?? [],
            dto.UserOverrides ?? [],
            dto.GameEngine,
            dto.ExtraLaunchArguments ?? "");
    }

    private static GameEntryDto ToDto(GameEntry g)
    {
        return new GameEntryDto
        {
            Id = g.Id,
            Library = g.Library,
            FolderPath = g.FolderPath,
            FolderName = g.FolderName,
            DisplayName = g.DisplayName,
            GameSource = g.GameSource,
            IsSourceOverridden = g.IsSourceOverridden,
            ExecutablePath = g.ExecutablePath,
            LauncherPath = g.LauncherPath,
            CommandLineArguments = g.CommandLineArguments,
            ManifestPath = g.ManifestPath,
            LastScanned = g.LastScanned,
            LastModified = g.LastModified,
            PlatformMetadata = g.PlatformMetadata,
            Tags = g.Tags,
            UserOverrides = g.UserOverrides,
            GameEngine = g.GameEngine,
            ExtraLaunchArguments = g.ExtraLaunchArguments,
        };
    }

    private sealed class GamesDatabaseDto
    {
        public int Version { get; set; } = 1;
        public List<GameEntryDto>? Games { get; set; }
    }

    private sealed class GameEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Library { get; set; }
        public string? FolderPath { get; set; }
        public string? FolderName { get; set; }
        public string? DisplayName { get; set; }
        public GameSourceKind GameSource { get; set; }
        public bool IsSourceOverridden { get; set; }
        public string? ExecutablePath { get; set; }
        public string? LauncherPath { get; set; }
        public string? CommandLineArguments { get; set; }
        public string? ManifestPath { get; set; }
        public DateTimeOffset LastScanned { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public Dictionary<string, string>? PlatformMetadata { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, string>? UserOverrides { get; set; }
        public GameEngineKind GameEngine { get; set; }
        public string? ExtraLaunchArguments { get; set; }
    }
}
