using GamingCommander.Core.Models;
using GamingCommander.Migration;

namespace GamingCommander.Migration.Tests;

public sealed class DesignTimeMigrationPlannerTests
{
    [Fact]
    public void SteamMoveAndLinkPreviewRequiresBackupAndLink()
    {
        GameRecord game = new(
            Id: "steam-570",
            Title: "Dota 2",
            Source: GameSourceKind.Steam,
            InstallPath: @"D:\SteamLibrary\steamapps\common\dota 2 beta",
            LaunchTarget: "steam://run/570",
            ExecutablePath: @"D:\SteamLibrary\steamapps\common\dota 2 beta\game\bin\win64\dota2.exe",
            LastModified: DateTimeOffset.FromUnixTimeSeconds(1700000000),
            SupportsPointerInteraction: true,
            SupportsKeyboardOnlyFlow: true);

        DesignTimeMigrationPlanner planner = new();

        MigrationPlanSummary plan = planner.BuildDryRunPlan(game, @"F:\MigratedGames\Dota 2", MigrationMode.MoveAndLink);

        Assert.True(plan.RequiresManifestBackup);
        Assert.True(plan.RequiresLinkCreation);
        Assert.True(plan.IsDryRunOnly);
    }
}
