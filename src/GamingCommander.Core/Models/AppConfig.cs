namespace GamingCommander.Core.Models;

public sealed record AppConfig(
    IReadOnlyList<LibraryRoot> LibraryRoots,
    IReadOnlyList<FolderOverride> FolderOverrides,
    IReadOnlyList<string> HiddenFolders,
    bool IsFirstRun,
    string? LastSeenVersion = null,
    bool EnableOnlineMetadata = false);
