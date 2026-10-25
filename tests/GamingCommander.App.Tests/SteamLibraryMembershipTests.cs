using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for SteamLibraryMembership — structural detection of a path under a
/// Steam library's steamapps/common folder, independent of ACF or DLL signals.
/// </summary>
public sealed class SteamLibraryMembershipTests
{
    [Theory]
    [InlineData(@"C:\SteamLibrary\steamapps\common\GameA")]
    [InlineData("D:/Games/SteamLibrary/steamapps/common/GameA")]
    [InlineData(@"C:\SteamLibrary\steamapps\common\GameA\binaries\win64")]
    public void IsUnderSteamLibrary_Descendant_ReturnsTrue(string path)
    {
        Assert.True(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Theory]
    [InlineData(@"C:\SteamLibrary\steamapps\common")]
    [InlineData(@"C:\SteamLibrary\steamapps\common\")]
    [InlineData("D:/Games/SteamLibrary/steamapps/common/")]
    public void IsUnderSteamLibrary_CommonFolderItself_ReturnsTrue(string path)
    {
        Assert.True(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Theory]
    [InlineData(@"C:\STEAM\STEAMAPPS\COMMON\GameA")]
    [InlineData("D:/Steam/SteamApps/Common/GameA")]
    public void IsUnderSteamLibrary_CaseInsensitive_ReturnsTrue(string path)
    {
        Assert.True(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Theory]
    [InlineData(@"C:\Steam\steamapps\commonfiles\GameA")]
    [InlineData("D:/Steam/steamapps/commonfiles/GameA")]
    [InlineData(@"C:\Steam\steamapps\commonbackup")]
    public void IsUnderSteamLibrary_SimilarSegment_ReturnsFalse(string path)
    {
        Assert.False(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Theory]
    [InlineData(@"C:\Games\GameA")]
    [InlineData("D:/Steam/steamapps/shadercache")]
    [InlineData("")]
    public void IsUnderSteamLibrary_UnrelatedPath_ReturnsFalse(string path)
    {
        Assert.False(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Fact]
    public void IsUnderSteamLibrary_KnownLibraryRoot_ReturnsTrue()
    {
        string[] roots = [@"C:\SteamLibrary", "D:/Games/SteamLibrary"];

        Assert.True(SteamLibraryMembership.IsUnderSteamLibrary(
            @"C:\SteamLibrary\steamapps\common\GameA", roots));
        Assert.True(SteamLibraryMembership.IsUnderSteamLibrary(
            "D:/Games/SteamLibrary/steamapps/common", roots));
    }

    [Fact]
    public void IsUnderSteamLibrary_KnownLibraryRootNotMatched_ReturnsFalse()
    {
        string[] roots = [@"C:\SteamLibrary"];

        Assert.False(SteamLibraryMembership.IsUnderSteamLibrary(
            @"D:\OtherLibrary\steamapps\downloading\GameA", roots));
    }
}
