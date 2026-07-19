namespace GamingCommander.Core.Models;

/// <summary>
/// A configured library root path with its default game source type.
/// </summary>
public sealed record LibraryRoot(
    /// <summary>Absolute path to the library root directory.</summary>
    string RootPath,
    /// <summary>Default source type assigned to games found under this root.</summary>
    GameSourceKind DefaultType);
