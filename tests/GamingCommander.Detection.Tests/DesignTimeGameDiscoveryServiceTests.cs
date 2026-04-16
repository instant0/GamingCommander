using GamingCommander.Core.Models;
using GamingCommander.Detection;

namespace GamingCommander.Detection.Tests;

public sealed class DesignTimeGameDiscoveryServiceTests
{
    [Fact]
    public void DiscoverInstalledGamesReturnsSteamAndStandaloneSamples()
    {
        DesignTimeGameDiscoveryService service = new();

        var games = service.DiscoverInstalledGames();

        Assert.NotEmpty(games);
        Assert.Contains(games, game => game.Source == GameSourceKind.Steam);
        Assert.Contains(games, game => game.Source == GameSourceKind.Standalone);
    }
}
