using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// High-level library management over anchors. A Library (anchor) is a named
/// catalog owning one or more physical folders; game entries link to the anchor
/// by name. Scanning a physical folder stores games under the anchor.
/// </summary>
public interface ILibraryManager
{
    /// <summary>All configured libraries (anchors), read live from persistence.</summary>
    IReadOnlyList<Library> Libraries { get; }

    /// <summary>Returns game entries linked to the specified library (anchor) name.</summary>
    IReadOnlyList<GameEntry> GetGamesForLibrary(string libraryName);

    /// <summary>
    /// Adds (or updates) a library anchor owning the given folders.
    /// </summary>
    void UpsertLibrary(string name, GameSourceKind type, IReadOnlyList<string> folders);

    /// <summary>Removes a library anchor and its game entries. Returns true if removed.</summary>
    bool RemoveLibrary(string name);

    /// <summary>
    /// Scans a physical folder into a library anchor.
    /// Returns the discovered game entries.
    /// </summary>
    IReadOnlyList<GameEntry> ScanFolder(string folderPath, string libraryName, GameSourceKind type);

    /// <summary>
    /// Rescans ALL physical folders of a library anchor, updating the database.
    /// Called at startup and on explicit refresh.
    /// </summary>
    void RescanLibrary(string libraryName, CancellationToken ct = default);

    /// <summary>Rescans the folder for a single physical path, assigning games to a library.</summary>
    IReadOnlyList<GameEntry> SelectScannerAndScan(string folderPath, GameSourceKind type, CancellationToken ct = default);

    /// <summary>Updates a game entry in the database.</summary>
    void UpdateGameEntry(GameEntry updatedEntry);

    /// <summary>Deletes a game entry from the database.</summary>
    void DeleteGameEntry(string gameId);

    /// <summary>Retags a game entry with a new source type (re-anchoring support).</summary>
    void RetagGame(string gameId, GameSourceKind newType);
}
