namespace GamingCommander.Core.Models;

/// <summary>
/// Per-store opt-in policy. All flags default off except <see cref="SkipUnchangedManifests"/>,
/// which defaults on because it only avoids redundant work.
/// </summary>
/// <param name="Kind">The launcher this policy applies to.</param>
/// <param name="IndexEnabled">Whether the store may be indexed at all.</param>
/// <param name="CheckOnLaunch">Whether the store is checked for changes at startup.</param>
/// <param name="EnrichNewGames">Whether newly imported games get metadata enrichment.</param>
/// <param name="SkipUnchangedManifests">Whether unchanged store manifests may be skipped.</param>
/// <param name="InventoryPathOverride">Optional user-supplied inventory location.</param>
public sealed record StorePolicy(
    GameSourceKind Kind,
    bool IndexEnabled = false,
    bool CheckOnLaunch = false,
    bool EnrichNewGames = false,
    bool SkipUnchangedManifests = true,
    string? InventoryPathOverride = null);
