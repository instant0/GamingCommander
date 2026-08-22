using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// Online extras lookup. Writes only to the sidecar, never to <c>games.json</c>.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Returns cached extras, optionally refreshing from the network when
    /// <see cref="AppConfig.EnableOnlineMetadata"/> is true and the cache is stale.
    /// </summary>
    Task<GameMetadataRecord?> RefreshAsync(
        string gameEntryId,
        string? steamAppId,
        string? displayName,
        CancellationToken cancellationToken = default,
        bool force = false);
}
