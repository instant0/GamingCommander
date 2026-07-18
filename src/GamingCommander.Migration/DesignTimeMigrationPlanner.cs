using GamingCommander.Core.Models;

namespace GamingCommander.Migration;

/// <summary>
/// Simple migration planner used for design-time and testing.
/// Determines manifest backup and link creation requirements based on source type.
/// </summary>
public sealed class DesignTimeMigrationPlanner : IMigrationPlanner
{
    /// <summary>
    /// Builds a dry-run plan. Steam and Epic games require manifest backup.
    /// MoveAndLink mode requires link creation (deprecated).
    /// </summary>
    public MigrationPlanSummary BuildDryRunPlan(GameRecord game, string targetPath, MigrationMode mode)
    {
        bool requiresManifestBackup = game.Source is GameSourceKind.Steam or GameSourceKind.Epic;
        bool requiresLinkCreation = mode == MigrationMode.MoveAndLink;

        return new MigrationPlanSummary(
            GameId: game.Id,
            SourcePath: game.InstallPath,
            TargetPath: targetPath,
            Mode: mode,
            RequiresManifestBackup: requiresManifestBackup,
            RequiresLinkCreation: requiresLinkCreation,
            IsDryRunOnly: true);
    }
}
