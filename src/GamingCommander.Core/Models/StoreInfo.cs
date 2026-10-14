namespace GamingCommander.Core.Models;

/// <summary>
/// Typed store identifiers for a game entry (Plan 127 A2). These are the authoritative
/// natural keys used for identity reconciliation (A5) and metadata lookups. Migrated out of
/// the legacy platform-metadata string bag.
/// </summary>
public sealed record StoreInfo
{
    /// <summary>Steam numeric AppID.</summary>
    public string SteamAppId { get; init; } = string.Empty;

    /// <summary>GOG product/game id.</summary>
    public string GogGameId { get; init; } = string.Empty;

    /// <summary>Epic catalog namespace (public when resolved from a global .item).</summary>
    public string EpicCatalogNamespace { get; init; } = string.Empty;

    /// <summary>Epic catalog item id.</summary>
    public string EpicCatalogItemId { get; init; } = string.Empty;

    /// <summary>Epic AppName (authoritative source: .mancpn; see Plan 126).</summary>
    public string EpicAppName { get; init; } = string.Empty;

    /// <summary>Detected developer/publisher signal (store enrichment).</summary>
    public string Studio { get; init; } = string.Empty;

    /// <summary>EA game name (non-trademarked) from the install log.</summary>
    public string EaGameName { get; init; } = string.Empty;

    /// <summary>Blizzard product codename from <c>.build.info</c>.</summary>
    public string BlizzardProduct { get; init; } = string.Empty;

    /// <summary>Ubisoft readme game title (Plan 112 Step 3B).</summary>
    public string UbisoftReadme { get; init; } = string.Empty;

    /// <summary>True when no store identifier is present.</summary>
    public bool IsEmpty =>
        string.IsNullOrEmpty(SteamAppId)
        && string.IsNullOrEmpty(GogGameId)
        && string.IsNullOrEmpty(EpicCatalogNamespace)
        && string.IsNullOrEmpty(EpicCatalogItemId)
        && string.IsNullOrEmpty(EpicAppName);
}
