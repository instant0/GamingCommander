namespace GamingCommander.Core.Models;

public enum FileSystemEntryKind
{
    Directory,
    File,
    ParentDirectory,
}

public sealed record FileSystemEntry(
    string Name,
    string FullPath,
    FileSystemEntryKind Kind,
    DateTimeOffset LastModified,
    long Size);
