namespace GamingCommander.Core.Models;

public sealed record MigrationPlanSummary(
    string GameId,
    string SourcePath,
    string TargetPath,
    MigrationMode Mode,
    bool RequiresManifestBackup,
    bool RequiresLinkCreation,
    bool IsDryRunOnly);
