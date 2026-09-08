using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// Loads and saves the Library (anchor) database from libraries.json.
/// A library is a named catalog (e.g. "Steam", "GOG", "d:\games") owning one or
/// more physical folders with a source type.
/// </summary>
public interface ILibrariesService
{
    /// <summary>All configured libraries (anchors).</summary>
    IReadOnlyList<Library> Libraries { get; }

    /// <summary>Adds or replaces a library by name.</summary>
    void Upsert(Library library);

    /// <summary>Removes a library by name. Returns true if it was removed.</summary>
    bool Remove(string name);

    /// <summary>Gets a library by name, or null.</summary>
    Library? Get(string name);
}
