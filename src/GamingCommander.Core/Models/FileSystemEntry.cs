namespace GamingCommander.Core.Models;

/// <summary>
/// A single entry in the virtual filesystem (directory, file, or parent).
/// </summary>
public sealed record FileSystemEntry(
    /// <summary>Display name of the entry.</summary>
    string Name,
    /// <summary>Absolute path to the filesystem entry.</summary>
    string FullPath,
    /// <summary>Whether this is a directory, file, or parent entry.</summary>
    FileSystemEntryKind Kind,
    /// <summary>Timestamp of the last modification.</summary>
    DateTimeOffset LastModified,
    /// <summary>File size in bytes (0 for directories).</summary>
    long SizeInBytes);
