using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// High-level library management: reads roots from config, delegates scanning and CRUD to services.
/// </summary>
public interface ILibraryManager
{
    /// <summary>Currently configured library roots, read live from persisted config.</summary>
    IReadOnlyList<LibraryRoot> LibraryRoots { get; }

    /// <summary>Returns game entries for the specified root, reading from the database.</summary>
    IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath);

    /// <summary>Adds a new library root to both config and database.</summary>
    void AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> initialGames);

    /// <summary>Removes a root from both config and database.</summary>
    void RemoveRoot(string rootPath);

    /// <summary>Reloads all library roots from config and refreshes the database cache.</summary>
    void Refresh();

    /// <summary>Rescans a root using the provided scanner results, updating the database.</summary>
    void RescanRoot(string rootPath, IReadOnlyList<GameEntry> games);

    /// <summary>Updates a game entry in the database.</summary>
    void UpdateGameEntry(string rootPath, GameEntry updatedEntry);

    /// <summary>Deletes a game entry from the database.</summary>
    void DeleteGameEntry(string rootPath, string gameId);

    /// <summary>Retags a game entry with a new source type.</summary>
    void RetagGame(string rootPath, string gameId, GameSourceKind newType);
}
