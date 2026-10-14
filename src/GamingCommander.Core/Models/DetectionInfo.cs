namespace GamingCommander.Core.Models;

/// <summary>
/// Detection provenance for a game entry (Plan 127 A3 / Plan 123 §0.1): why the entry is
/// considered a game and how confident we are. Replaced the legacy platform-metadata string bag.
/// </summary>
public sealed record DetectionInfo
{
    /// <summary>Confidence tier derived from the identifying signals.</summary>
    public IdentificationTier Tier { get; init; } = IdentificationTier.Unknown;

    /// <summary>Where the current display title came from (see <see cref="TitleSourceValues"/>).</summary>
    public string TitleSource { get; init; } = string.Empty;

    /// <summary>The title that was replaced when enrichment set <see cref="TitleSource"/> (audit trail).</summary>
    public string AutoDetectedTitle { get; init; } = string.Empty;

    /// <summary>PE FileDescription of the primary exe (title candidate, E7).</summary>
    public string PeFileDescription { get; init; } = string.Empty;

    /// <summary>Kind of manifest that identified the game (e.g. <c>EpicItem</c>, <c>Acf</c>, <c>GogInfo</c>); empty when none.</summary>
    public string ManifestKind { get; init; } = string.Empty;

    /// <summary>Signal identifiers that contributed (e.g. <c>SteamStructural</c>, <c>EpicItemGlobal</c>).</summary>
    public List<string> Signals { get; init; } = [];

    /// <summary>Candidate executable file names considered during detection.</summary>
    public List<string> CandidateExes { get; init; } = [];

    /// <summary>When this detection was produced.</summary>
    public DateTimeOffset? IdentifiedAt { get; init; }
}
