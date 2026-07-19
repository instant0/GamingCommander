using GamingCommander.Core.Services;
using Xunit;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Tests for GameEntryId — deterministic MD5-based ID generation for game entries.
/// </summary>
public sealed class GameEntryIdTests
{
    // ════════════════════════════════════════════════════════════════
    //  Determinism
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Compute_SameInputs_ReturnsSameId()
    {
        string id1 = GameEntryId.ComputeId("D:\\SteamLibrary", "Cyberpunk2077");
        string id2 = GameEntryId.ComputeId("D:\\SteamLibrary", "Cyberpunk2077");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Compute_CalledThreeTimes_ReturnsSameId()
    {
        string id1 = GameEntryId.ComputeId("C:\\Games", "MyGame");
        string id2 = GameEntryId.ComputeId("C:\\Games", "MyGame");
        string id3 = GameEntryId.ComputeId("C:\\Games", "MyGame");

        Assert.Equal(id1, id2);
        Assert.Equal(id2, id3);
    }

    // ════════════════════════════════════════════════════════════════
    //  Uniqueness
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Compute_DifferentFolders_ReturnsDifferentIds()
    {
        string id1 = GameEntryId.ComputeId("D:\\SteamLibrary", "GameA");
        string id2 = GameEntryId.ComputeId("D:\\SteamLibrary", "GameB");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Compute_DifferentRoots_ReturnsDifferentIds()
    {
        string id1 = GameEntryId.ComputeId("D:\\SteamLibrary", "MyGame");
        string id2 = GameEntryId.ComputeId("E:\\GOG Games", "MyGame");

        Assert.NotEqual(id1, id2);
    }

    // ════════════════════════════════════════════════════════════════
    //  Format
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Compute_Returns16CharHexString()
    {
        string id = GameEntryId.ComputeId("D:\\SteamLibrary", "Cyberpunk2077");

        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }

    [Fact]
    public void Compute_ReturnsLowercaseHex()
    {
        string id = GameEntryId.ComputeId("D:\\SteamLibrary", "Test");

        Assert.Equal(id, id.ToLowerInvariant());
    }

    // ════════════════════════════════════════════════════════════════
    //  Edge Cases
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Compute_EmptyFolderName_HandledGracefully()
    {
        string id = GameEntryId.ComputeId("D:\\SteamLibrary", "");

        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }

    [Fact]
    public void Compute_SpecialCharacters_HandledGracefully()
    {
        string id = GameEntryId.ComputeId("D:\\My Games & Apps", "Dark Souls III (2016)");

        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }
}
