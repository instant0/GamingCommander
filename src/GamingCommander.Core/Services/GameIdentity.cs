using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Builds the natural <see cref="IdentityKey"/> set for a game entry (Plan 127 A5). Keys are
/// derived from the folder name + store ids; they are rebuilt on every scan (not user state).
/// </summary>
public static class GameIdentity
{
    /// <summary>Rebuilds <see cref="IdentityInfo.IdentityKeys"/> from the entry's current signals.</summary>
    public static GameEntry WithRebuiltKeys(GameEntry g)
    {
        var keys = new List<IdentityKey>();
        if (!string.IsNullOrEmpty(g.FolderName))
            keys.Add(new IdentityKey(IdentityKeyKinds.FolderName, g.FolderName));
        if (!string.IsNullOrEmpty(g.Store.SteamAppId))
            keys.Add(new IdentityKey(IdentityKeyKinds.SteamAppId, g.Store.SteamAppId));
        if (!string.IsNullOrEmpty(g.Store.EpicCatalogItemId))
            keys.Add(new IdentityKey(IdentityKeyKinds.EpicCatalogItemId, g.Store.EpicCatalogItemId));
        if (!string.IsNullOrEmpty(g.Store.GogGameId))
            keys.Add(new IdentityKey(IdentityKeyKinds.GogGameId, g.Store.GogGameId));

        return g with { Identity = g.Identity with { IdentityKeys = keys } };
    }
}
