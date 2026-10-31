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
    [InlineData(@"C:\SteamLibrary\steamapps\common\GameA\binaries\win64")]
    [InlineData(@"C:\SteamLibrary\steamapps\common")]
    [InlineData(@"C:\SteamLibrary\steamapps\common\")]
    public void IsUnderSteamLibrary_UnderCommon_ReturnsTrue(string path)
    {
        Assert.True(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Theory]
    [InlineData(@"C:\Steam\steamapps\commonfiles\GameA")]
    [InlineData("")]
    public void IsUnderSteamLibrary_NotUnderCommon_ReturnsFalse(string path)
    {
        Assert.False(SteamLibraryMembership.IsUnderSteamLibrary(path));
    }

    [Theory]
    [InlineData(@"C:\SteamLibrary\steamapps\common\GameA", true)]
    [InlineData(@"D:\OtherLibrary\steamapps\downloading\GameA", false)]
    public void IsUnderSteamLibrary_KnownLibraryRoot_ScopesMatch(string path, bool expected)
    {
        string[] roots = [@"C:\SteamLibrary", "D:/Games/SteamLibrary"];

        Assert.Equal(expected, SteamLibraryMembership.IsUnderSteamLibrary(path, roots));
    }
}
