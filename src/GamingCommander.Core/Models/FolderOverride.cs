namespace GamingCommander.Core.Models;

/// <summary>
/// A per-folder source type override that takes precedence over the root default.
/// </summary>
public sealed record FolderOverride(
    /// <summary>Absolute path to the folder being overridden.</summary>
    string FolderPath,
    /// <summary>The source type to assign to games in this folder.</summary>
    GameSourceKind Type);
