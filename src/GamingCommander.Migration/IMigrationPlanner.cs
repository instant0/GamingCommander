using GamingCommander.Core.Models;

namespace GamingCommander.Migration;

public interface IMigrationPlanner
{
    MigrationPlanSummary BuildDryRunPlan(GameRecord game, string targetPath, MigrationMode mode);
}
