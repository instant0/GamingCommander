using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for DetectionTierDeriver (Plan 127 A3) — tier + provenance derived from the
/// signals a scanner already collected.
/// </summary>
public sealed class DetectionTierDeriverTests
{
    [Fact]
    public void Derive_Steam_IsLocked()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Steam);

        Assert.Equal(IdentificationTier.Locked, d.Tier);
        Assert.Equal("Acf", d.ManifestKind);
        Assert.Contains("SteamStructural", d.Signals);
    }

    [Fact]
    public void Derive_EpicGlobal_IsLocked()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Epic, epicStatus: "Installed");

        Assert.Equal(IdentificationTier.Locked, d.Tier);
        Assert.Contains("EpicItemGlobal", d.Signals);
    }

    [Fact]
    public void Derive_EpicLocalOnly_IsSecure()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Epic, epicStatus: "Orphaned");

        Assert.Equal(IdentificationTier.Secure, d.Tier);
        Assert.Equal("EpicItem", d.ManifestKind);
        Assert.Contains("EpicItemLocal", d.Signals);
    }

    [Fact]
    public void Derive_Gog_IsSecure()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Gog);

        Assert.Equal(IdentificationTier.Secure, d.Tier);
        Assert.Equal("GogInfo", d.ManifestKind);
    }

    [Theory]
    [InlineData(GameSourceKind.EaApp)]
    [InlineData(GameSourceKind.UbisoftConnect)]
    [InlineData(GameSourceKind.BattleNet)]
    [InlineData(GameSourceKind.Rockstar)]
    public void Derive_OtherStores_AreSecure(GameSourceKind source)
    {
        Assert.Equal(IdentificationTier.Secure, DetectionTierDeriver.Derive(source).Tier);
    }

    [Fact]
    public void Derive_StandaloneWithPeEvidence_IsCandidate()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Standalone, peFileDescription: "Some Game");

        Assert.Equal(IdentificationTier.Candidate, d.Tier);
        Assert.Contains("ExeEvidence", d.Signals);
    }

    [Fact]
    public void Derive_StandaloneWithCandidates_IsCandidate()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Standalone, candidateExes: ["a.exe", "b.exe"]);

        Assert.Equal(IdentificationTier.Candidate, d.Tier);
        Assert.Equal(["a.exe", "b.exe"], d.CandidateExes);
    }

    [Fact]
    public void Derive_StandaloneNoEvidence_IsUnknown()
    {
        Assert.Equal(IdentificationTier.Unknown, DetectionTierDeriver.Derive(GameSourceKind.Standalone).Tier);
    }

    [Fact]
    public void Derive_RecordsTitleSourceAsSignal()
    {
        var d = DetectionTierDeriver.Derive(GameSourceKind.Gog, titleSource: TitleSourceValues.GogInfo);

        Assert.Equal(TitleSourceValues.GogInfo, d.TitleSource);
        Assert.Contains(TitleSourceValues.GogInfo, d.Signals);
    }
}
