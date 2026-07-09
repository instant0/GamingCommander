using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

public sealed class DesignTimeLibraryManager : ILibraryManager
{
    private readonly IGamesDatabaseService _db;
    private readonly List<LibraryRoot> _roots = [];

    public DesignTimeLibraryManager(
        IGamesDatabaseService db)
    {
        _db = db;
    }

    public IReadOnlyList<LibraryRoot> LibraryRoots => _roots;

    public IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath)
    {
        return _db.GetGamesForRoot(rootPath);
    }

    public void AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> games)
    {
        if (!_roots.Any(r => r.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase)))
        {
            _roots.Add(new LibraryRoot(rootPath, defaultType));
        }
    }

    public void RemoveRoot(string rootPath)
    {
        _roots.RemoveAll(r => r.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
    }

    public void Refresh()
    {
        // No-op in design-time stub; LibraryManager handles real refresh.
    }

    public void RescanRoot(string rootPath, IReadOnlyList<GameEntry> games)
    {
        _db.RescanRoot(rootPath, games);
    }

    public void UpdateGameEntry(string rootPath, GameEntry updated)
    {
        _db.UpdateGameEntry(rootPath, updated);
    }

    public void DeleteGameEntry(string rootPath, string gameId)
    {
        _db.DeleteGameEntry(rootPath, gameId);
    }

    public void RetagGame(string rootPath, string gameId, GameSourceKind newType)
    {
        _db.RetagGame(rootPath, gameId, newType);
    }
}
