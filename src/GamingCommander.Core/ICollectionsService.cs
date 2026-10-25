using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>Reads/writes named collections (Plan 127 B12).</summary>
public interface ICollectionsService
{
    /// <summary>All collections, read live from disk.</summary>
    IReadOnlyList<Collection> Collections { get; }

    /// <summary>Adds or replaces a collection by name.</summary>
    void Upsert(Collection collection);

    /// <summary>Removes a collection by name. Returns true if it was removed.</summary>
    bool Remove(string name);

    /// <summary>Gets a collection by name, or null.</summary>
    Collection? Get(string name);
}
