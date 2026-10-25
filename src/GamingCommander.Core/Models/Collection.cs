namespace GamingCommander.Core.Models;

/// <summary>
/// A named collection/playlist of games, optionally driven by a smart filter query (Plan 127 B12).
/// Stored in <c>data/collections.json</c>.
/// </summary>
public sealed record Collection
{
    /// <summary>Display name (unique key).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Explicitly included game ids (for manual playlists).</summary>
    public List<string> GameIds { get; init; } = [];

    /// <summary>
    /// Smart-filter query (free text, same syntax as the F8 filter); when non-empty the collection
    /// is evaluated dynamically instead of using <see cref="GameIds"/>.
    /// </summary>
    public string SmartQuery { get; init; } = string.Empty;

    /// <summary>True when this collection is driven by <see cref="SmartQuery"/>.</summary>
    public bool IsSmart => !string.IsNullOrWhiteSpace(SmartQuery);
}
