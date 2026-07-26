using GamingCommander.Core.Models;

namespace GamingCommander.Core.Tests;

public class GameEntryTagsTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 7, 26, 14, 0, 0, TimeSpan.Zero);

    private static GameEntry MakeGame(
        List<string>? tags = null,
        Dictionary<string, string>? userOverrides = null) =>
        new(
            Id: "test-123",
            FolderName: "GameFolder",
            DisplayName: "Test Game",
            GameSource: GameSourceKind.Standalone,
            IsSourceOverridden: false,
            ExecutablePath: @"C:\Games\game.exe",
            LauncherPath: "",
            CommandLineArguments: "",
            ManifestPath: "",
            LastScanned: FixedTime,
            LastModified: FixedTime,
            PlatformMetadata: [],
            Tags: tags ?? [],
            UserOverrides: userOverrides ?? []);

    // ════════════════════════════════════════════════════════════════
    //  Default Values
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DefaultTags_IsEmpty()
    {
        var game = MakeGame();
        Assert.Empty(game.Tags);
    }

    [Fact]
    public void DefaultUserOverrides_IsEmpty()
    {
        var game = MakeGame();
        Assert.Empty(game.UserOverrides);
    }

    // ════════════════════════════════════════════════════════════════
    //  Tags with Values
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Tags_PreservesOrder()
    {
        var game = MakeGame(tags: ["RPG", "Co-op", "Story Rich"]);
        Assert.Equal(3, game.Tags.Count);
        Assert.Equal("RPG", game.Tags[0]);
        Assert.Equal("Co-op", game.Tags[1]);
        Assert.Equal("Story Rich", game.Tags[2]);
    }

    [Fact]
    public void UserOverrides_PreservesKeysAndValues()
    {
        var overrides = new Dictionary<string, string>
        {
            ["DisplayName"] = "2026-07-26T14:30:00Z",
            ["Tags"] = "2026-07-26T14:30:00Z",
        };
        var game = MakeGame(userOverrides: overrides);

        Assert.Equal(2, game.UserOverrides.Count);
        Assert.Equal("2026-07-26T14:30:00Z", game.UserOverrides["DisplayName"]);
        Assert.Equal("2026-07-26T14:30:00Z", game.UserOverrides["Tags"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Record Equality (scalar fields only — reference types use reference equality)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScalarFields_Match_ForSameData()
    {
        var game1 = MakeGame(tags: ["RPG", "Co-op"]);
        var game2 = MakeGame(tags: ["RPG", "Co-op"]);

        Assert.Equal(game1.Id, game2.Id);
        Assert.Equal(game1.DisplayName, game2.DisplayName);
        Assert.Equal(game1.GameSource, game2.GameSource);
        Assert.Equal(game1.ExecutablePath, game2.ExecutablePath);
    }

    [Fact]
    public void TagsList_ContainsSameElements()
    {
        var game1 = MakeGame(tags: ["RPG"]);
        var game2 = MakeGame(tags: ["Co-op"]);

        Assert.Single(game1.Tags);
        Assert.Single(game2.Tags);
        Assert.NotEqual(game1.Tags[0], game2.Tags[0]);
    }

    [Fact]
    public void UserOverrides_DifferentValues_Detectable()
    {
        var game1 = MakeGame(userOverrides: new() { ["DisplayName"] = "2026-01-01" });
        var game2 = MakeGame(userOverrides: new() { ["DisplayName"] = "2026-07-26" });

        Assert.NotEqual(game1.UserOverrides["DisplayName"], game2.UserOverrides["DisplayName"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  With Expression (Copy)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void WithExpression_CanUpdateTags()
    {
        var original = MakeGame(tags: ["RPG"]);
        var updated = original with { Tags = ["RPG", "Co-op"] };

        Assert.Single(original.Tags);
        Assert.Equal(2, updated.Tags.Count);
    }

    [Fact]
    public void WithExpression_CanUpdateUserOverrides()
    {
        var original = MakeGame();
        var updated = original with
        {
            UserOverrides = new Dictionary<string, string>
            {
                ["DisplayName"] = "2026-07-26T14:30:00Z"
            }
        };

        Assert.Empty(original.UserOverrides);
        Assert.Single(updated.UserOverrides);
    }
}
