using System.Security.Cryptography;
using System.Text;

namespace GamingCommander.Core.Services;

/// <summary>
/// Deterministic ID generation for GameEntry records.
/// Produces a stable 16-character lowercase hex string from library (anchor)
/// name + physical folder path.
/// </summary>
public static class GameEntryId
{
    /// <summary>
    /// Compute a stable game entry ID from the library (anchor) name and the
    /// physical folder path. The same inputs always produce the same ID.
    /// </summary>
    public static string ComputeId(string libraryName, string folderPath)
    {
        string combined = $"{libraryName}|{folderPath}";
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
