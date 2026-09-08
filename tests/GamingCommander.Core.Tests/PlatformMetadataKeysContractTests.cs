using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Pin test (Plan 125 Phase 2b): PlatformMetadataKeys constants must equal EXACTLY
/// the persisted games.json key strings. These dictionary keys are on-disk data —
/// refactors may change the C# references, never the values this test asserts.
/// </summary>
public sealed class PlatformMetadataKeysContractTests
{
    [Fact]
    public void ConstantValues_EqualThePersistedGameMetadataKeys()
    {
        // Steam
        Assert.Equal("SteamAppId", PlatformMetadataKeys.SteamAppId);
        Assert.Equal("SteamStatus", PlatformMetadataKeys.SteamStatus);
        Assert.Equal("AcfFilePath", PlatformMetadataKeys.AcfFilePath);
        Assert.Equal("AcfLibraryPath", PlatformMetadataKeys.AcfLibraryPath);
        Assert.Equal("AcfExpectedPath", PlatformMetadataKeys.AcfExpectedPath);
        Assert.Equal("AcfBuildId", PlatformMetadataKeys.AcfBuildId);
        Assert.Equal("AcfSizeOnDisk", PlatformMetadataKeys.AcfSizeOnDisk);
        Assert.Equal("AcfStateFlags", PlatformMetadataKeys.AcfStateFlags);

        // Epic
        Assert.Equal("EpicStatus", PlatformMetadataKeys.EpicStatus);
        Assert.Equal("EpicItemPath", PlatformMetadataKeys.EpicItemPath);
        Assert.Equal("EpicCatalogItemId", PlatformMetadataKeys.EpicCatalogItemId);
        Assert.Equal("EpicCatalogNamespace", PlatformMetadataKeys.EpicCatalogNamespace);
        Assert.Equal("EpicAppName", PlatformMetadataKeys.EpicAppName);

        // Shared location / executable candidates
        Assert.Equal("LibraryRoot", PlatformMetadataKeys.LibraryRoot);
        Assert.Equal("ActualLibraryRoot", PlatformMetadataKeys.ActualLibraryRoot);
        Assert.Equal("GameFolder", PlatformMetadataKeys.GameFolder);
        Assert.Equal("FolderName", PlatformMetadataKeys.FolderName);
        Assert.Equal("ExeCandidates", PlatformMetadataKeys.ExeCandidates);
        Assert.Equal("ExeCandidateCount", PlatformMetadataKeys.ExeCandidateCount);
        Assert.Equal("PeFileDescription", PlatformMetadataKeys.PeFileDescription);

        // Store title / studio enrichment
        Assert.Equal("GogGameId", PlatformMetadataKeys.GogGameId);
        Assert.Equal("Studio", PlatformMetadataKeys.Studio);
        Assert.Equal("EaGameName", PlatformMetadataKeys.EaGameName);
        Assert.Equal("BlizzardProduct", PlatformMetadataKeys.BlizzardProduct);

        // Title persistence (E8)
        Assert.Equal("TitleSource", PlatformMetadataKeys.TitleSource);
        Assert.Equal("AutoDetectedTitle", PlatformMetadataKeys.AutoDetectedTitle);
    }

    [Fact]
    public void ConstantValues_AreAllDistinct_NoTwoKeysAliasOneSlot()
    {
        string[] values =
        [
            PlatformMetadataKeys.SteamAppId, PlatformMetadataKeys.SteamStatus,
            PlatformMetadataKeys.AcfFilePath, PlatformMetadataKeys.AcfLibraryPath,
            PlatformMetadataKeys.AcfExpectedPath, PlatformMetadataKeys.AcfBuildId,
            PlatformMetadataKeys.AcfSizeOnDisk, PlatformMetadataKeys.AcfStateFlags,
            PlatformMetadataKeys.EpicStatus, PlatformMetadataKeys.EpicItemPath,
            PlatformMetadataKeys.EpicCatalogItemId, PlatformMetadataKeys.EpicCatalogNamespace,
            PlatformMetadataKeys.EpicAppName,
            PlatformMetadataKeys.LibraryRoot, PlatformMetadataKeys.ActualLibraryRoot,
            PlatformMetadataKeys.GameFolder, PlatformMetadataKeys.FolderName,
            PlatformMetadataKeys.ExeCandidates, PlatformMetadataKeys.ExeCandidateCount,
            PlatformMetadataKeys.PeFileDescription,
            PlatformMetadataKeys.GogGameId, PlatformMetadataKeys.Studio,
            PlatformMetadataKeys.EaGameName, PlatformMetadataKeys.BlizzardProduct,
            PlatformMetadataKeys.TitleSource, PlatformMetadataKeys.AutoDetectedTitle,
        ];

        Assert.Equal(values.Length, values.Distinct().Count());
    }
}