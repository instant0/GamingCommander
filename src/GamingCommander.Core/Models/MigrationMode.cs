namespace GamingCommander.Core.Models;

/// <summary>
/// Determines what actions are taken when migrating a game to a new location.
/// </summary>
public enum MigrationMode
{
    /// <summary>Move game files only; no manifest repair or link creation.</summary>
    MoveOnly = 0,

    /// <summary>Move game files and create a symbolic link at the original location (deprecated).</summary>
    MoveAndLink = 1,

    /// <summary>Repair launcher registration without moving files.</summary>
    ManifestRepairOnly = 2,
}
