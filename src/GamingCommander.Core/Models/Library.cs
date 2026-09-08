namespace GamingCommander.Core.Models;

/// <summary>
/// A Library (also called an anchor): a unique, named top-level catalog
/// (e.g. "Steam", "GOG", "EPIC", "d:\games") that owns one or more physical
/// folders and has a source type. Game entries link to a library by name.
/// </summary>
public sealed record Library(
    /// <summary>Unique anchor name used as the linkage key for game entries.</summary>
    string Name,
    /// <summary>Source type for this library (Steam, Epic, Standalone, …).</summary>
    GameSourceKind Type,
    /// <summary>One or more physical library root folders that feed this library.</summary>
    IReadOnlyList<string> Folders);
