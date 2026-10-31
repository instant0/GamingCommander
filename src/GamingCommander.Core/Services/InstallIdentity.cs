using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Identity seams for a physical install. A game is identified by its full path, or by a
/// store id when the store provides one; a folder name is display text and, at most, a weak
/// relocation hint scoped to one store. Names are never identity on their own.
/// </summary>
public static class InstallIdentity
{
    /// <summary>
    /// Canonical key for a physical folder. Empty input -> empty string (callers skip it).
    /// The key is a comparison form only — never hand it to the filesystem or storage.
    /// </summary>
    public static string PathKey(string? fullPath) => PathText.NormalizeForCompare(fullPath);

    /// <summary>
    /// Scope for the weak relocation hint: the store plus the folder name. Two entries match
    /// on this key only when they belong to the same store and the name is unique on both
    /// sides. Empty folder name -> empty string (callers skip it). Case is preserved; the
    /// dictionaries that use it compare with <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </summary>
    public static string WeakFolderKey(GameSourceKind kind, string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return string.Empty;

        return $"{kind}\u0000{folderName.Trim()}";
    }
}
