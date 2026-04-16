using GamingCommander.Core.Models;

namespace GamingCommander.Core;

public interface ILibraryManager
{
    IReadOnlyList<LibraryRoot> LibraryRoots { get; }
    IReadOnlyList<IGame> Games { get; }
    IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath);
    void AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> games);
    void RemoveRoot(string rootPath);
    void Refresh();
    void RescanRoot(string rootPath, IReadOnlyList<GameEntry> games);
    void UpdateGameEntry(string rootPath, GameEntry updated);
    void DeleteGameEntry(string rootPath, string gameId);
    void RetagGame(string rootPath, string gameId, GameSourceKind newType);
}
