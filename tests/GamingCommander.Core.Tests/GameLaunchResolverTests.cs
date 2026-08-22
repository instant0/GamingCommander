using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class GameLaunchResolverTests
{
    [Fact]
    public void SteamUri_AlwaysSteam_IgnoresExtras()
    {
        var game = Make(
            exe: @"C:\Games\Cyberpunk 2077\bin\x64\Cyberpunk2077.exe",
            cmd: "steam://rungameid/1091500",
            extras: "--launcher-skip");

        (string target, string args) = GameLaunchResolver.Resolve(game);
        Assert.Equal("steam://rungameid/1091500", target);
        Assert.Equal("", args);
    }

    [Fact]
    public void Standalone_CombinesLegacyAndExtras()
    {
        var game = Make(
            exe: @"D:\Games\game.exe",
            cmd: "-windowed",
            extras: "--launcher-skip");

        (string target, string args) = GameLaunchResolver.Resolve(game);
        Assert.Equal(@"D:\Games\game.exe", target);
        Assert.Equal("-windowed --launcher-skip", args);
    }

    private static GameEntry Make(string exe, string cmd, string extras) =>
        new(
            Id: "g1",
            FolderName: "Game",
            DisplayName: "Game",
            GameSource: GameSourceKind.Steam,
            IsSourceOverridden: false,
            ExecutablePath: exe,
            LauncherPath: "",
            CommandLineArguments: cmd,
            ManifestPath: "",
            LastScanned: DateTimeOffset.UnixEpoch,
            LastModified: DateTimeOffset.UnixEpoch,
            PlatformMetadata: [],
            Tags: [],
            UserOverrides: [],
            ExtraLaunchArguments: extras);
}
