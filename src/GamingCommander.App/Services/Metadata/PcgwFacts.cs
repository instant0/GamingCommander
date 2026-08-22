namespace GamingCommander.App.Services.Metadata;

/// <summary>Source-shaped PCGamingWiki Infobox fields (before common normalization).</summary>
public sealed record PcgwFacts(
    string? PageTitle,
    string? PageUrl,
    IReadOnlyList<string> Developers,
    IReadOnlyList<string> Publishers,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Engines,
    IReadOnlyList<string> ReleaseDates);

/// <summary>One PCGW Parse: Infobox identity plus Plan 120 operator sections.</summary>
public sealed record PcgwLookupResult(PcgwFacts? Facts, PcgwSectionFacts Sections);
