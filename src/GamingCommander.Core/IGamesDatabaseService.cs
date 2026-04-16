using GamingCommander.Core.Models;

namespace GamingCommander.Core;

public interface IGamesDatabaseService
{
    GamesDatabase Load();
    void Save(GamesDatabase db);
    IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath);
    void AddRoot(string rootPath, GameSourceKind defaultType, IEnumerable<GameEntry> games);
    void RemoveRoot(string rootPath);
    void RescanRoot(string rootPath, IEnumerable<GameEntry> games);
    void UpdateGameEntry(string rootPath, GameEntry updated);
    void DeleteGameEntry(string rootPath, string gameId);
    void RetagGame(string rootPath, string gameId, GameSourceKind newType);
}
