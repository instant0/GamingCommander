using System.Security.Cryptography;
using System.Text;

namespace GamingCommander.Core.Services;

/// <summary>
/// Deterministic ID generation for GameEntry records.
/// Produces a stable 16-character lowercase hex string from the physical
/// library root + physical folder path. The anchor (Library) name is metadata,
/// not identity — re-anchoring never changes a game's ID.
/// </summary>
public static class GameEntryId
{
    /// <summary>
    /// Compute a stable game entry ID from the physical library root and the
    /// physical folder path: <c>ID = MD5("{physicalLibraryRoot|folder}")</c>,
    /// independent of the anchor name. The same inputs always produce the same ID.
    /// </summary>
    public static string ComputeId(string physicalLibraryRoot, string folderPath)
    {
        string combined = $"{physicalLibraryRoot}|{folderPath}";
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
