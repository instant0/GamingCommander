namespace GamingCommander.App.Services.Metadata;

/// <summary>Source-shaped Steam Store fields (before common normalization).</summary>
public sealed record SteamStoreFacts(
    bool Success,
    string? Name,
    IReadOnlyList<string> Developers,
    IReadOnlyList<string> Publishers,
    string? ReleaseDate,
    IReadOnlyList<string> Genres,
    string? ShortDescription,
    int? MetacriticScore,
    string? SteamAppId,
    string? HeaderImage,
    string? Website);
