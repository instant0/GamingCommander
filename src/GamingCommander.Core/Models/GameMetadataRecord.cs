namespace GamingCommander.Core.Models;

/// <summary>
/// Right-pane extras stored in <c>data/games_metadata.json</c>, never in games.json.
/// All fields nullable — missing means “not known”, not empty string overwrite.
/// <para>
/// <b>Ownership (Plan 127 A6):</b> this sidecar owns <b>fetched enrichment only</b>. It is
/// never authoritative for identity, store ids, or engine — those live on
/// <see cref="GameEntry"/> (<c>Store</c> / <c>GameEngine</c>). The <c>Fetched*</c> fields here are
/// hints discovered from PCGW/Steam and may differ from (or exist without) the detected values.
/// </para>
/// </summary>
public sealed record GameMetadataRecord
{
    /// <summary>Matches <see cref="GameEntry.Id"/>.</summary>
    public string GameEntryId { get; init; } = string.Empty;

    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string? ReleaseDate { get; init; }
    public string? Genre { get; init; }
    public string? Description { get; init; }

    /// <summary>Fetched engine name (PCGW). Not authoritative — the detected engine is <see cref="GameEntry.GameEngine"/>.</summary>
    public string? FetchedEngine { get; init; }

    public int? MetacriticScore { get; init; }

    /// <summary>Fetched Steam AppID (PCGW/Steam). Not authoritative — the detected id is <c>GameEntry.Store.SteamAppId</c>.</summary>
    public string? FetchedSteamAppId { get; init; }

    /// <summary>Fetched GOG id (PCGW). Not authoritative — the detected id is <c>GameEntry.Store.GogGameId</c>.</summary>
    public string? FetchedGogGameId { get; init; }

    public string? CoverArtUrl { get; init; }
    public string? OfficialWebsite { get; init; }
    public string? PcGamingWikiUrl { get; init; }
    public string? LastMetadataSource { get; init; }
    public DateTimeOffset? LastUpdated { get; init; }

    /// <summary>PCGW operator extras (paths, cmdline catalog, video). Null when never fetched.</summary>
    public GameMetadataDetails? Details { get; init; }

    /// <summary>True when at least one display field is present.</summary>
    public bool HasDisplayableExtras =>
        !string.IsNullOrWhiteSpace(Developer)
        || !string.IsNullOrWhiteSpace(Publisher)
        || !string.IsNullOrWhiteSpace(Genre)
        || !string.IsNullOrWhiteSpace(ReleaseDate)
        || MetacriticScore.HasValue
        || !string.IsNullOrWhiteSpace(PcGamingWikiUrl)
        || !string.IsNullOrWhiteSpace(Description)
        || Details?.HasAny == true;
}
