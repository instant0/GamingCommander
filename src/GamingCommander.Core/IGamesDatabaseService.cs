using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// CRUD operations for the game database (data/games.json). Provides in-memory caching.
/// Games are a flat list; each entry links to a Library (anchor) by <see cref="GameEntry.Library"/>.
/// </summary>
public interface IGamesDatabaseService
{
    /// <summary>Loads the games database from disk. Returns cached version if already loaded.</summary>
    GamesDatabase Load();

    /// <summary>Persists the games database to disk and updates the in-memory cache.</summary>
    void Save(GamesDatabase db);

    /// <summary>Returns all game entries linked to the specified library (anchor) name.</summary>
    IReadOnlyList<GameEntry> GetGamesForLibrary(string libraryName);

    /// <summary>Adds or replaces the game entries for a library (anchor).</summary>
    void SetGamesForLibrary(string libraryName, IEnumerable<GameEntry> games);

    /// <summary>Removes all game entries linked to the specified library name.</summary>
    void RemoveGamesForLibrary(string libraryName);

    /// <summary>Updates a single game entry by ID.</summary>
    void UpdateGameEntry(GameEntry updatedEntry);

    /// <summary>Removes a game entry by ID.</summary>
    void DeleteGameEntry(string gameId);

    /// <summary>Changes the source type of a game entry without modifying other fields.</summary>
    void RetagGame(string gameId, GameSourceKind newType);
}
