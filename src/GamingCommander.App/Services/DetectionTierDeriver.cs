using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Derives <see cref="DetectionInfo"/> (tier + provenance) from the signals a scanner already
/// collected (Plan 127 A3, Plan 123 §0.1). No new detection pass.
/// </summary>
internal static class DetectionTierDeriver
{
    /// <summary>Derives the confidence tier and provenance for an entry produced by <paramref name="source"/>.</summary>
    public static DetectionInfo Derive(
        GameSourceKind source,
        string titleSource = "",
        string peFileDescription = "",
        IReadOnlyList<string>? candidateExes = null,
        string epicStatus = "")
    {
        var signals = new List<string>();
        string manifestKind = "";
        IdentificationTier tier;

        switch (source)
        {
            case GameSourceKind.Steam:
                tier = IdentificationTier.Locked;
                manifestKind = "Acf";
                signals.Add("SteamStructural");
                break;

            case GameSourceKind.Epic:
                // Global .item matched to the install location is authoritative (Locked);
                // a local .mancpn/.item only is Secure (Plan 126).
                bool global = string.Equals(epicStatus, "Installed", StringComparison.OrdinalIgnoreCase);
                tier = global ? IdentificationTier.Locked : IdentificationTier.Secure;
                signals.Add(global ? "EpicItemGlobal" : "EpicItemLocal");
                manifestKind = "EpicItem";
                break;

            case GameSourceKind.Gog:
                tier = IdentificationTier.Secure;
                manifestKind = "GogInfo";
                signals.Add("GogInfo");
                break;

            case GameSourceKind.EaApp:
            case GameSourceKind.UbisoftConnect:
            case GameSourceKind.BattleNet:
            case GameSourceKind.Xbox:
            case GameSourceKind.Rockstar:
            case GameSourceKind.SteamEmu:
                tier = IdentificationTier.Secure;
                signals.Add(source.ToString());
                break;

            case GameSourceKind.Standalone:
            default:
                bool hasEvidence =
                    !string.IsNullOrEmpty(peFileDescription)
                    || (candidateExes?.Count ?? 0) > 0
                    || !string.IsNullOrEmpty(titleSource);
                tier = hasEvidence ? IdentificationTier.Candidate : IdentificationTier.Unknown;
                if (hasEvidence)
                    signals.Add("ExeEvidence");
                break;
        }

        if (!string.IsNullOrEmpty(titleSource) && !signals.Contains(titleSource))
            signals.Add(titleSource);

        return new DetectionInfo
        {
            Tier = tier,
            TitleSource = titleSource,
            ManifestKind = manifestKind,
            Signals = signals,
            CandidateExes = candidateExes is null ? [] : [.. candidateExes],
            IdentifiedAt = DateTimeOffset.UtcNow,
        };
    }
}
