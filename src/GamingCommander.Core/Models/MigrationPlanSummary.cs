namespace GamingCommander.Core.Models;

/// <summary>
/// Dry-run output describing what a migration would do.
/// </summary>
public sealed record MigrationPlanSummary(
    /// <summary>ID of the game being migrated.</summary>
    string GameId,
    /// <summary>Current installation path.</summary>
    string SourcePath,
    /// <summary>Proposed new installation path.</summary>
    string TargetPath,
    /// <summary>Migration mode (move, link, or manifest repair).</summary>
    MigrationMode Mode,
    /// <summary>True if the launcher manifest must be backed up before migration.</summary>
    bool RequiresManifestBackup,
    /// <summary>True if a symbolic link should be created at the original path (deprecated).</summary>
    bool RequiresLinkCreation,
    /// <summary>True if this is a dry-run plan (no changes will be made).</summary>
    bool IsDryRunOnly);
