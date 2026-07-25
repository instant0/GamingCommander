using System.Text;
using System.Text.RegularExpressions;

namespace GamingCommander.App.Services;

/// <summary>
/// Parses Windows .lnk shortcut files to extract target executable names.
/// Uses binary decoding (latin-1) + regex instead of COM interop for cross-platform safety.
/// </summary>
internal static partial class LnkParser
{
    // DLLs and patterns to skip (not real game exes)
    private static readonly HashSet<string> s_skipExeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam_api.dll", "steam_api64.dll", "eos.dll", "upc.dll",
    };

    /// <summary>
    /// Extracts the target .exe filename from a .lnk shortcut file.
    /// Returns true if a valid exe name was found.
    /// </summary>
    internal static bool TryGetExeName(string lnkPath, out string? exeName)
    {
        exeName = null;
        try
        {
            byte[] data = File.ReadAllBytes(lnkPath);
            // .lnk files use legacy encoding — latin-1 preserves all byte values as characters
            string text = Encoding.Latin1.GetString(data);
            var matches = LnkExeRegex().Matches(text);
            if (matches.Count == 0) return false;

            // Pick longest candidate (most likely the real game exe)
            string? best = null;
            foreach (Match m in matches)
            {
                string candidate = m.Value;
                if (s_skipExeNames.Contains(candidate)) continue;
                if (best is null || candidate.Length > best.Length)
                    best = candidate;
            }

            exeName = best;
            return best is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the actual .exe path from .lnk files in the game root.
    /// Searches subdirectories up to maxDepth for the target exe.
    /// Handles backup renames (-Name.exe, "copy of Name.exe").
    /// Returns the resolved exe path, or null if not found.
    /// </summary>
    internal static string? ResolveExeFromLnk(DirectoryInfo gameDir, int maxDepth = 3)
    {
        try
        {
            foreach (string lnkPath in Directory.EnumerateFiles(gameDir.FullName, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                if (!TryGetExeName(lnkPath, out string? exeName) || exeName is null)
                    continue;

                string exeLower = exeName.ToLowerInvariant();
                string exeStem = exeLower[..exeLower.LastIndexOf('.')]; // e.g., "penumbra"

                // Search subdirs for the exe (exact match first, then fuzzy)
                string? fuzzyMatch = null;
                foreach (string exePath in FindExesInSubdirs(gameDir, maxDepth))
                {
                    string foundName = Path.GetFileName(exePath).ToLowerInvariant();
                    if (foundName == exeLower)
                        return exePath; // Exact match — return immediately

                    // Fuzzy: backup renames
                    if (fuzzyMatch is null)
                    {
                        if (foundName.StartsWith('-') && foundName[1..] == exeLower)
                            fuzzyMatch = exePath;
                        else if (foundName.StartsWith("copy of ") && foundName[8..] == exeLower)
                            fuzzyMatch = exePath;
                        else if (exeStem.Length > 2 && foundName.Contains(exeStem) && foundName.EndsWith(".exe"))
                            fuzzyMatch = exePath;
                    }
                }

                if (fuzzyMatch is not null)
                    return fuzzyMatch;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Searches for all non-noise executables in subdirectories up to maxDepth.
    /// Returns candidates for exact/fuzzy matching by the caller.
    /// </summary>
    private static IEnumerable<string> FindExesInSubdirs(
        DirectoryInfo root, int maxDepth, int depth = 0)
    {
        if (depth > maxDepth) yield break;

        // Check the root directory itself (matches Python's os.walk starting at root)
        if (depth == 0)
        {
            foreach (string file in Directory.EnumerateFiles(root.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (!FileSystemHelper.IsNoiseExeName(Path.GetFileNameWithoutExtension(file), Array.Empty<string>()))
                    yield return file;
            }
        }

        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(root.FullName))
        {
            if (FileSystemHelper.NoiseSubDirNames.Contains(child.Name))
                continue;

            foreach (string file in Directory.EnumerateFiles(child.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (!FileSystemHelper.IsNoiseExeName(Path.GetFileNameWithoutExtension(file), Array.Empty<string>()))
                    yield return file;
            }

            // Recurse
            foreach (string found in FindExesInSubdirs(child, maxDepth, depth + 1))
                yield return found;
        }
    }

    /// <summary>
    /// Regex pattern for extracting .exe filenames from .lnk binary data.
    /// Matches: GameName.exe, game_name.exe, game-name.exe, etc.
    /// </summary>
    [GeneratedRegex(@"([A-Za-z0-9_\-\.]+\.exe)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LnkExeRegex();
}
