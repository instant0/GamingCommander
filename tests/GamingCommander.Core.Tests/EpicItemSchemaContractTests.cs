using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Pin test (Plan 125 final stage): EpicItemSchema constants must equal EXACTLY the
/// Epic .item / .mancpn JSON member spellings that EpicManifestParser reads and
/// EpicItemWriter writes. Both share these constants, so a writer/reader drift is a
/// compile error; this test pins the values to the real Epic format.
/// </summary>
public sealed class EpicItemSchemaContractTests
{
    [Fact]
    public void ConstantValues_EqualTheEpicItemMemberSpellings()
    {
        Assert.Equal("DisplayName", EpicItemSchema.DisplayName);
        Assert.Equal("InstallLocation", EpicItemSchema.InstallLocation);
        Assert.Equal("LaunchExecutable", EpicItemSchema.LaunchExecutable);
        Assert.Equal("CatalogNamespace", EpicItemSchema.CatalogNamespace);
        Assert.Equal("CatalogItemId", EpicItemSchema.CatalogItemId);
        Assert.Equal("AppName", EpicItemSchema.AppName);
        Assert.Equal("bIsIncompleteInstall", EpicItemSchema.BIsIncompleteInstall);
        Assert.Equal("bIsApplication", EpicItemSchema.BIsApplication);
        Assert.Equal("AppCategories", EpicItemSchema.AppCategories);
    }

    [Fact]
    public void ConstantValues_AreAllDistinct_NoTwoMembersAliasOneSlot()
    {
        string[] values =
        [
            EpicItemSchema.DisplayName, EpicItemSchema.InstallLocation,
            EpicItemSchema.LaunchExecutable, EpicItemSchema.CatalogNamespace,
            EpicItemSchema.CatalogItemId, EpicItemSchema.AppName,
            EpicItemSchema.BIsIncompleteInstall, EpicItemSchema.BIsApplication,
            EpicItemSchema.AppCategories,
        ];

        Assert.Equal(values.Length, values.Distinct().Count());
    }
}