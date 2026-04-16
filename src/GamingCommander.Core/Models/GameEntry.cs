namespace GamingCommander.Core.Models;

public sealed record GameEntry(
    string Id,
    string FolderName,
    string DisplayName,
    GameSourceKind GameSource,
    bool Override,
    string ExecutablePath,
    string LauncherPath,
    string CmdlineArgs,
    string ManifestPath,
    DateTimeOffset LastScanned,
    DateTimeOffset LastModified,
    Dictionary<string, string> Extra);

public sealed record GameRoot(
    string RootPath,
    GameSourceKind DefaultType,
    List<GameEntry> Games);

public sealed record GamesDatabase(
    List<GameRoot> Roots);
