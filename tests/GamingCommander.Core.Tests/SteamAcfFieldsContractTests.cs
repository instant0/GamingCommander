using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Pin test (Plan 125 final stage): SteamAcfFields constants must equal EXACTLY the
/// Steam ACF / libraryfolders.vdf field spellings (see docs/research/steam_acf_schema.md).
/// SteamAcfParser and SteamAcfWriter share these constants, so a writer/reader drift is
/// a compile error; this test pins the values to the real Steam format so nobody
/// "corrects" a constant and silently breaks the written .acf files.
/// </summary>
public sealed class SteamAcfFieldsContractTests
{
    [Fact]
    public void ConstantValues_EqualTheSteamAcfFieldSpellings()
    {
        Assert.Equal("AppState", SteamAcfFields.AppState);
        Assert.Equal("appid", SteamAcfFields.AppId);
        Assert.Equal("Universe", SteamAcfFields.Universe);
        Assert.Equal("name", SteamAcfFields.Name);
        Assert.Equal("installdir", SteamAcfFields.InstallDir);
        Assert.Equal("StateFlags", SteamAcfFields.StateFlags);
        Assert.Equal("LastUpdated", SteamAcfFields.LastUpdated);
        Assert.Equal("SizeOnDisk", SteamAcfFields.SizeOnDisk);
        Assert.Equal("buildid", SteamAcfFields.BuildId);
        Assert.Equal("path", SteamAcfFields.Path);
    }

    [Fact]
    public void ConstantValues_AreAllDistinct_NoTwoFieldsAliasOneSlot()
    {
        string[] values =
        [
            SteamAcfFields.AppState, SteamAcfFields.AppId, SteamAcfFields.Universe,
            SteamAcfFields.Name, SteamAcfFields.InstallDir, SteamAcfFields.StateFlags,
            SteamAcfFields.LastUpdated, SteamAcfFields.SizeOnDisk, SteamAcfFields.BuildId,
            SteamAcfFields.Path,
        ];

        Assert.Equal(values.Length, values.Distinct().Count());
    }
}