namespace GamingCommander.Core.Models;

/// <summary>
/// A library root with its associated game entries.
/// </summary>
public sealed record GameRoot(
    /// <summary>Absolute path to the library root directory.</summary>
    string RootPath,
    /// <summary>Default source type assigned to games under this root.</summary>
    GameSourceKind DefaultType,
    /// <summary>Game entries discovered under this root.</summary>
    List<GameEntry> Games);
