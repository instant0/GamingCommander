using GamingCommander.Core.Models;

namespace GamingCommander.Migration;

/// <summary>
/// Interface for building dry-run migration plans. Plans describe what would happen
/// without executing any changes.
/// </summary>
public interface IMigrationPlanner
{
    /// <summary>
    /// Builds a dry-run migration plan for moving a game to a new target path.
    /// Returns a summary of required actions (backup, link creation, etc.) without making any changes.
    /// </summary>
    MigrationPlanSummary BuildDryRunPlan(GameRecord game, string targetPath, MigrationMode mode);
}
