using System.Security.Cryptography;
using System.Text;

namespace GamingCommander.Core.Services;

/// <summary>
/// Deterministic ID generation for GameEntry records.
/// Produces a stable 16-character lowercase hex string from the physical
/// library root + physical folder path. Both inputs are normalized through
/// <see cref="InstallIdentity.PathKey"/> first, so separator, trailing-separator
/// and case formatting do not create a second id for the same install.
/// The anchor (Library) name is metadata, not identity — re-anchoring never
/// changes a game's ID.
/// </summary>
public static class GameEntryId
{
    /// <summary>
    /// Compute a stable game entry ID from the physical library root and the
    /// physical folder path: <c>ID = MD5("{physicalLibraryRoot|folder}")</c>,
    /// independent of the anchor name. Inputs are compared as paths (separators,
    /// trailing separators and case are normalized), so the same physical install
    /// always produces the same ID. Persisted ids are disposable before ship.
    /// </summary>
    public static string ComputeId(string physicalLibraryRoot, string folderPath)
    {
        // NTFS paths are case-insensitive, so casing must not fork an id either.
        string combined =
            $"{InstallIdentity.PathKey(physicalLibraryRoot)}|{InstallIdentity.PathKey(folderPath)}"
                .ToLowerInvariant();
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
