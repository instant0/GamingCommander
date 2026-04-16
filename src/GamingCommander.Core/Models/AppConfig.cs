namespace GamingCommander.Core.Models;

public sealed record AppConfig(
    IReadOnlyList<LibraryRoot> LibraryRoots,
    IReadOnlyList<FolderOverride> FolderOverrides,
    bool IsFirstRun);
