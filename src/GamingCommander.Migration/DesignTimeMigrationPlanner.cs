using GamingCommander.Core.Models;

namespace GamingCommander.Migration;

public sealed class DesignTimeMigrationPlanner : IMigrationPlanner
{
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
