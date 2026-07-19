using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// CRUD operations for the games database (data/games.json). Provides in-memory caching.
/// </summary>
public interface IGamesDatabaseService
{
    /// <summary>Loads the games database from disk. Returns cached version if already loaded.</summary>
    GamesDatabase Load();

    /// <summary>Persists the games database to disk and updates the in-memory cache.</summary>
    void Save(GamesDatabase db);

    /// <summary>Returns all game entries associated with the specified library root path.</summary>
    IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath);

    /// <summary>Adds a new library root with its game entries.</summary>
    void AddRoot(string rootPath, GameSourceKind defaultType, IEnumerable<GameEntry> initialGames);

    /// <summary>Removes a library root and all its associated game entries.</summary>
    void RemoveRoot(string rootPath);

    /// <summary>Replaces all game entries for a root with freshly scanned results.</summary>
    void RescanRoot(string rootPath, IEnumerable<GameEntry> games);

    /// <summary>Updates a single game entry within the specified root.</summary>
    void UpdateGameEntry(string rootPath, GameEntry updatedEntry);

    /// <summary>Removes a game entry by ID from the specified root.</summary>
    void DeleteGameEntry(string rootPath, string gameId);

    /// <summary>Changes the source type of a game entry without modifying other fields.</summary>
    void RetagGame(string rootPath, string gameId, GameSourceKind newType);
}
