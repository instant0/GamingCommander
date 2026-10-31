using GamingCommander.Core.Services;
using Xunit;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Tests for GameEntryId — deterministic MD5-based ID generation for game entries.
/// </summary>
public sealed class GameEntryIdTests
{
    [Fact]
    public void Compute_SameInputs_ReturnsSameId()
    {
        string id1 = GameEntryId.ComputeId("D:\\SteamLibrary", "Cyberpunk2077");
        string id2 = GameEntryId.ComputeId("D:\\SteamLibrary", "Cyberpunk2077");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Compute_DifferentFoldersOrRoots_ReturnsDifferentIds()
    {
        Assert.NotEqual(
            GameEntryId.ComputeId("D:\\SteamLibrary", "GameA"),
            GameEntryId.ComputeId("D:\\SteamLibrary", "GameB"));
        Assert.NotEqual(
            GameEntryId.ComputeId("D:\\SteamLibrary", "MyGame"),
            GameEntryId.ComputeId("E:\\GOG Games", "MyGame"));
    }

    [Fact]
    public void Compute_Returns16CharHexString()
    {
        string id = GameEntryId.ComputeId("D:\\SteamLibrary", "Cyberpunk2077");

        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }

    [Fact]
    public void Compute_NormalizedPathFormatting_ReturnsSameId()
    {
        string baseline = GameEntryId.ComputeId(@"D:\Games", @"D:\Games\Foo");

        Assert.Equal(baseline, GameEntryId.ComputeId("D:/Games", "D:/Games/Foo"));
        Assert.Equal(baseline, GameEntryId.ComputeId(@"d:\games", @"d:\games\foo"));
        Assert.Equal(baseline, GameEntryId.ComputeId(@"D:\Games\\", @"D:\Games\Foo\"));
    }
}
