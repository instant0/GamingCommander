using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

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

        var existingForLib = db.Games
            .Where(g => g.Library.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var existingById = existingForLib.ToDictionary(g => g.Id, StringComparer.OrdinalIgnoreCase);
        var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var merged = new List<GameEntry>();
        foreach (GameEntry g in scanned)
        {
            // Every stored entry belongs to this library anchor, regardless of what
            // the scan-context stamped. Anchor name is authoritative here.
            GameEntry anchored = GameIdentity.WithRebuiltKeys(
                g with { Identity = g.Identity with { Library = libraryName } });

            if (existingById.TryGetValue(anchored.Id, out GameEntry? existing))
            {
                consumed.Add(existing.Id);
                merged.Add(MergeGameEntries(existing, anchored));
                continue;
            }

            // External move/rename: adopt the existing identity so the sidecar + user
            // state survive (Plan 127 A5).
            GameEntry? relocated = FindRelocated(existingForLib, consumed, anchored);
            if (relocated is not null)
            {
                consumed.Add(relocated.Id);
                GameEntry adopted = anchored with
                {
                    Identity = anchored.Identity with { Id = relocated.Id },
                    Location = anchored.Location with
                    {
                        PreviousLocations = AppendPreviousLocation(relocated),
                    },
                };
                merged.Add(MergeGameEntries(relocated, adopted));
                continue;
            }

            merged.Add(anchored);
        }

        var keep = db.Games
            .Where(g => !g.Library.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        keep.AddRange(merged);

        Save(new GamesDatabase(keep));
    }

    /// <summary>
    /// Finds an existing (not-yet-consumed) entry that is the same game at a different path —
    /// i.e. it was moved/renamed externally. Strong keys (store ids) match regardless of disk
    /// state; the weak <c>FolderName</c> key matches only when the old folder is gone (Plan 127 A5).
    /// </summary>
    private static GameEntry? FindRelocated(
        IReadOnlyList<GameEntry> existing, HashSet<string> consumed, GameEntry scanned)
    {
        var scannedKeys = new HashSet<string>(
            scanned.Identity.IdentityKeys.Select(k => k.Kind + "\u0000" + k.Value),
            StringComparer.OrdinalIgnoreCase);

        foreach (GameEntry e in existing)
        {
            if (consumed.Contains(e.Id))
                continue;
            if (PathsEqual(e.FolderPath, scanned.FolderPath))
                continue;

            foreach (IdentityKey key in e.Identity.IdentityKeys)
            {
                if (!scannedKeys.Contains(key.Kind + "\u0000" + key.Value))
                    continue;

                if (!string.Equals(key.Kind, IdentityKeyKinds.FolderName, StringComparison.OrdinalIgnoreCase))
                    return e; // strong key
                if (!Directory.Exists(e.FolderPath))
                    return e; // weak key: only when the old folder is gone
            }
        }

        return null;
    }

    private static List<string> AppendPreviousLocation(GameEntry relocated)
    {
        var previous = new List<string>(relocated.Location.PreviousLocations);
        if (!string.IsNullOrEmpty(relocated.FolderPath)
            && !previous.Contains(relocated.FolderPath, StringComparer.OrdinalIgnoreCase))
        {
            previous.Add(relocated.FolderPath);
        }

        return previous;
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

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
        games[idx] = games[idx] with
        {
            Identity = games[idx].Identity with { GameSource = newType, IsSourceOverridden = true },
        };
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

        // When the exe is user-pinned, drop scanner-derived candidates.
        DetectionInfo detection = scanned.Detection;
        if (existing.UserOverrides.ContainsKey(GameEntryFields.ExecutablePath))
        {
            detection = detection with { CandidateExes = [] };
        }

        // E8: preserve a title-pin marker through a rescan merge.
        if (existing.Detection.TitleSource is TitleSourceValues.PcgwPick or TitleSourceValues.UserOverride)
        {
            detection = detection with { TitleSource = existing.Detection.TitleSource };
        }

        return scanned with
        {
            Identity = scanned.Identity with { DisplayName = displayName, GameSource = gameSource },
            Launch = scanned.Launch with
            {
                ExecutablePath = executablePath,
                CommandLineArguments = commandLineArgs,
                LauncherPath = launcherPath,
                ManifestPath = manifestPath,
            },
            Detection = detection,
            Tags = existing.Tags.Count > 0 ? existing.Tags : scanned.Tags,
            UserOverrides = existing.UserOverrides.Count > 0 ? existing.UserOverrides : scanned.UserOverrides,
            LastScanned = DateTimeOffset.UtcNow,
        };
    }

    private static GameEntry ToEntry(GameEntryDto dto)
    {
        return new GameEntry(
            dto.Identity,
            dto.Location,
            dto.Launch,
            dto.Tags ?? [],
            dto.UserOverrides ?? [],
            dto.GameEngine,
            dto.LastScanned,
            dto.LastModified)
        {
            Detection = dto.Detection ?? new(),
            Store = dto.Store ?? new(),
            Acf = dto.Acf ?? new(),
            Epic = dto.Epic ?? new(),
        };
    }

    private static GameEntryDto ToDto(GameEntry g)
    {
        return new GameEntryDto
        {
            Identity = g.Identity,
            Location = g.Location,
            Launch = g.Launch,
            Tags = g.Tags,
            UserOverrides = g.UserOverrides,
            GameEngine = g.GameEngine,
            LastScanned = g.LastScanned,
            LastModified = g.LastModified,
            Detection = g.Detection,
            Store = g.Store,
            Acf = g.Acf,
            Epic = g.Epic,
        };
    }

    private sealed class GamesDatabaseDto
    {
        public int Version { get; set; } = 1;
        public List<GameEntryDto>? Games { get; set; }
    }

    private sealed class GameEntryDto
    {
        public IdentityInfo Identity { get; set; } = new();
        public LocationInfo Location { get; set; } = new();
        public LaunchInfo Launch { get; set; } = new();
        public List<string>? Tags { get; set; }
        public Dictionary<string, string>? UserOverrides { get; set; }
        public GameEngineKind GameEngine { get; set; }
        public DateTimeOffset LastScanned { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public DetectionInfo? Detection { get; set; }
        public StoreInfo? Store { get; set; }
        public SteamAcfInfo? Acf { get; set; }
        public EpicInfo? Epic { get; set; }
    }
}
