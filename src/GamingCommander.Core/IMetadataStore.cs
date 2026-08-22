using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// Offline sidecar for right-pane extras. Must not read or write <c>games.json</c>.
/// </summary>
public interface IMetadataStore
{
    /// <summary>Returns the merged record for a game, or null if none.</summary>
    GameMetadataRecord? Get(string gameEntryId);

    /// <summary>Inserts or replaces the merged record for a game. Creates the sidecar file if needed.</summary>
    void Upsert(string gameEntryId, GameMetadataRecord merged);
}
