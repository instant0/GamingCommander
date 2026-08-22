namespace GamingCommander.Core.Models;

/// <summary>
/// Right-pane extras stored in <c>data/games_metadata.json</c>, never in games.json.
/// All fields nullable — missing means “not known”, not empty string overwrite.
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
    public string? Engine { get; init; }
    public int? MetacriticScore { get; init; }
    public string? SteamAppId { get; init; }
    public string? GogGameId { get; init; }
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
