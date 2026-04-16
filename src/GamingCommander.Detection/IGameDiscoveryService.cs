using GamingCommander.Core.Models;

namespace GamingCommander.Detection;

public interface IGameDiscoveryService
{
    IReadOnlyList<GameRecord> DiscoverInstalledGames();
}
