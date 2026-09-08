using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Pin test (Plan 125 final stage): TitleSourceValues constants must equal EXACTLY
/// the persisted games.json TitleSource strings. These values are on-disk data —
/// refactors may change the C# references, never the values this test asserts.
/// </summary>
public sealed class TitleSourceValuesContractTests
{
    [Fact]
    public void ConstantValues_EqualThePersistedTitleSourceValues()
    {
        Assert.Equal("GogInfo", TitleSourceValues.GogInfo);
        Assert.Equal("EaInstallLog", TitleSourceValues.EaInstallLog);
        Assert.Equal("EpicItemManifest", TitleSourceValues.EpicItemManifest);
        Assert.Equal("FolderExeMatch", TitleSourceValues.FolderExeMatch);
        Assert.Equal("PeFileDescription", TitleSourceValues.PeFileDescription);
        Assert.Equal("UbisoftReadme", TitleSourceValues.UbisoftReadme);
        Assert.Equal("PcgwPick", TitleSourceValues.PcgwPick);
        Assert.Equal("UserOverride", TitleSourceValues.UserOverride);
    }

    [Fact]
    public void ConstantValues_AreAllDistinct_NoTwoValuesAliasOneDerivation()
    {
        string[] values =
        [
            TitleSourceValues.GogInfo, TitleSourceValues.EaInstallLog,
            TitleSourceValues.EpicItemManifest, TitleSourceValues.FolderExeMatch,
            TitleSourceValues.PeFileDescription, TitleSourceValues.UbisoftReadme,
            TitleSourceValues.PcgwPick, TitleSourceValues.UserOverride,
        ];

        Assert.Equal(values.Length, values.Distinct().Count());
    }
}