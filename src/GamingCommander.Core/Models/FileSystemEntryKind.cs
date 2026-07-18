namespace GamingCommander.Core.Models;

/// <summary>
/// Type of filesystem entry in the virtual filesystem model.
/// </summary>
public enum FileSystemEntryKind
{
    /// <summary>A browsable directory (game folder or library root).</summary>
    Directory,

    /// <summary>A file entry (game executable).</summary>
    File,

    /// <summary>The '..' parent directory entry.</summary>
    ParentDirectory,
}
