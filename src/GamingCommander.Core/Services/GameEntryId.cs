using System.Security.Cryptography;
using System.Text;

namespace GamingCommander.Core.Services;

/// <summary>
/// Deterministic ID generation for GameEntry records.
/// Produces a stable 16-character lowercase hex string from root path + folder name.
/// </summary>
public static class GameEntryId
{
    /// <summary>
    /// Compute a stable game entry ID from the library root path and folder name.
    /// The same inputs always produce the same ID, regardless of platform path separator.
    /// </summary>
    public static string Compute(string rootPath, string folderName)
    {
        string combined = $"{rootPath}|{folderName}";
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
