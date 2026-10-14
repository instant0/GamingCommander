namespace GamingCommander.Core.Models;

/// <summary>
/// A natural key used to re-match a game after it has moved or been renamed (Plan 127 A5).
/// Strong keys (store ids) match regardless of disk state; the weak <c>FolderName</c> key only
/// matches when the old folder is gone (external move).
/// </summary>
public sealed record IdentityKey(string Kind, string Value);

/// <summary>Kind values for <see cref="IdentityKey.Kind"/>.</summary>
public static class IdentityKeyKinds
{
    /// <summary>Weak key: the installation folder name.</summary>
    public const string FolderName = "FolderName";

    /// <summary>Strong key: Steam numeric AppID.</summary>
    public const string SteamAppId = "SteamAppId";

    /// <summary>Strong key: Epic catalog item id.</summary>
    public const string EpicCatalogItemId = "EpicCatalogItemId";

    /// <summary>Strong key: GOG product id.</summary>
    public const string GogGameId = "GogGameId";
}
