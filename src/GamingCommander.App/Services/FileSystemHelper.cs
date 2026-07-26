namespace GamingCommander.App.Services;

/// <summary>
/// Safe filesystem operations that return defaults on failure instead of throwing exceptions.
/// Used by FolderScanner and SteamLibraryScanner to handle permissions, locked files,
/// and removed directories gracefully on Windows.
/// </summary>
internal static class FileSystemHelper
{
    /// <summary>
    /// Subdirectory names to skip during executable search (platform-specific binary dirs, redists, etc.).
    /// Used by both FolderScanner (top-level scan) and ExecutableDiscovery (deep exe search).
    /// </summary>
    internal static readonly IReadOnlySet<string> NoiseSubDirNames = new HashSet<string>(
    [
        "__redist", "_commonredist", "commonredist", "redist", "directx",
        "vcredist", "dotnet", "physx", "support", "_installer", "install",
        "installer", "easyanticheat", "devtools", "docs", "licenses",
        "steam controller configs", "steamworks shared",
        // Store launcher directories — these are games stores, not games themselves
        // NOTE: "blizzard" and "battle.net" REMOVED — they are publisher containers with game subdirs
        "epic games", "origin", "uplay", "gog galaxy",
        "ea app", "rockstar games",
    ], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns subdirectories of the given path, or an empty array if access fails.
    /// Catches UnauthorizedAccessException, IOException, and other filesystem errors
    /// that are common on Windows when scanning game libraries.
    /// </summary>
    internal static DirectoryInfo[] GetDirectoriesSafe(string path)
    {
        try
        {
            return new DirectoryInfo(path).GetDirectories();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Returns files matching a pattern in a directory (top-level only), or an empty array if access fails.
    /// Catches filesystem errors common when scanning game libraries on Windows.
    /// </summary>
    internal static string[] GetFilesSafe(DirectoryInfo dir, string pattern)
    {
        try
        {
            return dir.GetFiles(pattern, SearchOption.TopDirectoryOnly)
                .Select(f => f.FullName)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Returns the last write time of a directory, or DateTimeOffset.MinValue if access fails.
    /// Catches filesystem errors that occur when scanning directories with restricted permissions.
    /// </summary>
    internal static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)
    {
        try
        {
            return dir.LastWriteTime;
        }
        catch
        {
            return DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Returns all files in a directory (top-level only), or an empty array if access fails.
    /// Catches UnauthorizedAccessException, IOException, and other filesystem errors
    /// common on Windows when scanning game libraries.
    /// </summary>
    internal static FileInfo[] GetFilesSafe(DirectoryInfo dir)
    {
        try
        {
            return dir.GetFiles("*", SearchOption.TopDirectoryOnly);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Checks if an executable name (without extension) matches any noise pattern.
    /// Used by FolderScanner, ExecutableDiscovery, and SteamLibraryScanner to filter
    /// non-game executables (installers, redistributables, launchers, etc.).
    /// </summary>
    internal static bool IsNoiseExeName(string name, IReadOnlyList<string> patterns)
    {
        return patterns.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a directory name matches known noise patterns (saves, mods, workshops, etc.).
    /// Used by FolderScanner and ExecutableDiscovery to skip non-game directories.
    /// </summary>
    internal static bool IsNoiseDirectory(string dirName, IReadOnlySet<string> patterns)
    {
        if (patterns.Count == 0)
            return false;

        string lower = dirName.ToLowerInvariant();
        return patterns.Any(p => lower.Contains(p));
    }

    /// <summary>
    /// Normalizes a game folder name into a human-readable display name.
    /// Strips common edition suffixes (Remastered, Definitive Edition, etc.)
    /// and replaces underscores/hyphens with spaces.
    /// </summary>
    internal static string NormalizeDisplayName(string folderName)
    {
        return folderName
            .Replace("Remastered", "")
            .Replace("Definitive Edition", "")
            .Replace("Enhanced Edition", "")
            .Replace("Ultimate Edition", "")
            .Replace("Special Edition", "")
            .Replace("GOTY", "")
            .Replace("Edition", "")
            .Replace("_", " ")
            .Replace("-", " ")
            .Trim();
    }
}
