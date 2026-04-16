namespace GamingCommander.Core.Models;

public sealed record LibraryRoot(
    string Path,
    GameSourceKind DefaultType);

public sealed record FolderOverride(
    string FolderPath,
    GameSourceKind Type);
