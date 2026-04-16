using GamingCommander.Core.Models;

namespace GamingCommander.Detection;

public sealed class DesignTimeGameDiscoveryService : IGameDiscoveryService
{
    public IReadOnlyList<GameRecord> DiscoverInstalledGames()
    {
        return
        [
            new GameRecord(
                Id: "steam-570",
                Title: "Dota 2",
                Source: GameSourceKind.Steam,
                InstallPath: @"D:\SteamLibrary\steamapps\common\dota 2 beta",
                LaunchTarget: "steam://run/570",
                ExecutablePath: @"D:\SteamLibrary\steamapps\common\dota 2 beta\game\bin\win64\dota2.exe",
                LastModified: DateTimeOffset.FromUnixTimeSeconds(1700000000),
                SupportsPointerInteraction: true,
                SupportsKeyboardOnlyFlow: true),
            new GameRecord(
                Id: "standalone-openmw",
                Title: "OpenMW",
                Source: GameSourceKind.Standalone,
                InstallPath: @"D:\Games\OpenMW",
                LaunchTarget: @"D:\Games\OpenMW\openmw-launcher.exe",
                ExecutablePath: @"D:\Games\OpenMW\openmw-launcher.exe",
                LastModified: DateTimeOffset.FromUnixTimeSeconds(1650000000),
                SupportsPointerInteraction: true,
                SupportsKeyboardOnlyFlow: true),
            new GameRecord(
                Id: "steam-292030",
                Title: "The Witcher 3",
                Source: GameSourceKind.Steam,
                InstallPath: @"E:\SteamLibrary\steamapps\common\The Witcher 3",
                LaunchTarget: "steam://run/292030",
                ExecutablePath: @"E:\SteamLibrary\steamapps\common\The Witcher 3\witcher3.exe",
                LastModified: DateTimeOffset.FromUnixTimeSeconds(1680000000),
                SupportsPointerInteraction: true,
                SupportsKeyboardOnlyFlow: true),
        ];
    }
}
