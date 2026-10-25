using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class GameStoreIndexTests
{
    private static GameEntry MakeGame(
        string id,
        GameSourceKind source,
        string storeKey,
        string folderPath)
    {
        return new GameEntry(
            Identity: new IdentityInfo
            {
                Id = id,
                Library = "Test",
                DisplayName = id,
                GameSource = source,
            },
            Location: new LocationInfo { FolderPath = folderPath, FolderName = id },
            Launch: new LaunchInfo(),
            Tags: [],
            UserOverrides: [])
        {
            Store = new StoreInfo { StoreKey = storeKey },
        };
    }

    [Fact]
    public void GetMissingStoreKeys_NineKnownOfTenManifest_ReportsTheMissingKey()
    {
        List<GameEntry> known = Enumerable.Range(1, 9)
            .Select(i => MakeGame($"g{i}", GameSourceKind.EaApp, $"EaApp:{i}", $@"C:\Games\Game{i}"))
            .ToList();
        var index = GameStoreIndex.Build(known);
        string[] manifest = Enumerable.Range(1, 10)
            .Select(i => $"EaApp:{i}")
            .ToArray();

        IReadOnlyList<string> missing = index.GetMissingStoreKeys(manifest);

        Assert.Equal(["EaApp:10"], missing);
    }

    [Fact]
    public void Lookup_ByStoreKeyPathAndStoreKind_Succeeds()
    {
        GameEntry game = MakeGame("g1", GameSourceKind.Steam, "Steam:400", @"C:\Games\Half-Life\");
        var index = GameStoreIndex.Build([game]);

        Assert.Same(game, index.FindByStoreKey("steam:400"));
        Assert.Same(game, index.FindByPath(@"c:/games/half-life"));
        Assert.Same(game, index.FindByPath(@"C:\Games\Half-Life\\"));
        Assert.Same(game, Assert.Single(index.GetForStore(GameSourceKind.Steam)));
        Assert.Null(index.FindByStoreKey("Steam:999"));
        Assert.Null(index.FindByPath(@"D:\Other"));
        Assert.Empty(index.GetForStore(GameSourceKind.Gog));
    }

    [Fact]
    public void DuplicateStoreKey_IsSurfaced_AndDoesNotSilentlyOverwrite()
    {
        GameEntry first = MakeGame("g1", GameSourceKind.Steam, "Steam:400", @"C:\Games\First");
        GameEntry second = MakeGame("g2", GameSourceKind.Steam, "Steam:400", @"C:\Games\Second");
        var index = GameStoreIndex.Build([first, second]);

        IReadOnlyList<GameEntry> duplicates = Assert.Single(index.Duplicates).Value;
        Assert.Equal(["g1", "g2"], duplicates.Select(g => g.Id));
        Assert.Same(first, index.FindByStoreKey("Steam:400"));
        Assert.Single(index.ByStoreKey);
    }

    [Fact]
    public void Build_DoesNotRequirePathsToExist()
    {
        const string missingRoot = @"Z:\Definitely\Not\A\Real\Path";
        GameEntry game = MakeGame("g1", GameSourceKind.Gog, "Gog:123", missingRoot);

        var index = GameStoreIndex.Build([game]);

        Assert.False(Directory.Exists(missingRoot));
        Assert.Same(game, index.FindByPath(missingRoot));
    }

    [Fact]
    public void Build_EntriesWithoutStoreKey_AreOmittedFromPrimaryIndex()
    {
        GameEntry keyed = MakeGame("g1", GameSourceKind.Steam, "Steam:400", @"C:\Games\A");
        GameEntry unkeyed = MakeGame("g2", GameSourceKind.Standalone, string.Empty, @"C:\Games\B");

        var index = GameStoreIndex.Build([keyed, unkeyed]);

        Assert.Single(index.ByStoreKey);
        Assert.Empty(index.Duplicates);
        Assert.Same(unkeyed, Assert.Single(index.GetForStore(GameSourceKind.Standalone)));
    }

    [Fact]
    public void NormalizePath_UnifiesSeparatorsAndTrailingSeparators()
    {
        Assert.Equal("C:/Games/Test", GameStoreIndex.NormalizePath(@"C:\Games\Test\"));
        Assert.Equal("C:/Games/Test", GameStoreIndex.NormalizePath("C:/Games/Test/"));
        Assert.Equal(string.Empty, GameStoreIndex.NormalizePath("  "));
        Assert.Equal(string.Empty, GameStoreIndex.NormalizePath(null));
    }
}
