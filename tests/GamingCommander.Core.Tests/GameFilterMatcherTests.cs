using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class GameFilterMatcherTests
{
    private static readonly DateTimeOffset T = new(2026, 8, 22, 0, 0, 0, TimeSpan.Zero);

    private static GameEntry Game(string name, GameSourceKind source, params string[] tags) =>
        new(
            Id: name,
            FolderName: name,
            DisplayName: name,
            GameSource: source,
            IsSourceOverridden: false,
            ExecutablePath: @"D:\g.exe",
            LauncherPath: "",
            CommandLineArguments: "",
            ManifestPath: "",
            LastScanned: T,
            LastModified: T,
            PlatformMetadata: [],
            Tags: [.. tags],
            UserOverrides: []);

    [Fact]
    public void Tag_MatchesUserOrSidecar()
    {
        GameEntry game = Game("AC4", GameSourceKind.UbisoftConnect, "Co-op");
        var filter = new GameFilter(GameFilterKind.Tag, "Action");
        Assert.False(GameFilterMatcher.Matches(game, filter));
        Assert.True(GameFilterMatcher.Matches(game, filter, ["Action", "Anvil"]));
        Assert.True(GameFilterMatcher.Matches(game, new GameFilter(GameFilterKind.Tag, "Co-op")));
    }

    [Fact]
    public void Label_MatchesStoreDisplayName()
    {
        GameEntry game = Game("AC4", GameSourceKind.UbisoftConnect);
        Assert.True(GameFilterMatcher.Matches(game, new GameFilter(GameFilterKind.Label, "Ubisoft Connect")));
        Assert.False(GameFilterMatcher.Matches(game, new GameFilter(GameFilterKind.Label, "Steam")));
    }

    [Fact]
    public void Wildcard_MatchesName()
    {
        GameEntry game = Game("Assassin's Creed IV", GameSourceKind.Steam);
        Assert.True(GameFilterMatcher.Matches(game, new GameFilter(GameFilterKind.Wildcard, "creed")));
    }

    [Fact]
    public void CollectOptions_IncludesSidecarTagsAndStores()
    {
        GameEntry game = Game("AC4", GameSourceKind.Steam, "Co-op");
        IReadOnlyList<GameFilterOption> options = GameFilterMatcher.CollectOptions(
            [(game, (IEnumerable<string>)["Action"])]);
        Assert.Contains(options, o => o.Kind == GameFilterKind.Tag && o.Value == "Action");
        Assert.Contains(options, o => o.Kind == GameFilterKind.Tag && o.Value == "Co-op");
        Assert.Contains(options, o => o.Kind == GameFilterKind.Label && o.Value == "Steam");
    }
}
