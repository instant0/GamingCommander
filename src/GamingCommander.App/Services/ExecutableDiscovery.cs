using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Discovers and scores executable files within a game directory.
/// Handles deep search (root, children, UE Binaries paths), primary exe selection,
/// launcher detection, and Epic manifest discovery.
/// </summary>
internal static class ExecutableDiscovery
{
    /// <summary>
    /// Finds all non-noise executables within a game folder, searching:
    /// 1. Root directory
    /// 2. Immediate child directories (skipping noise dirs)
    /// 3. Binaries/{Win64,Win32,WinGDK,Steam}/ paths in children
    /// 4. child/bin/ for older UE games
    /// 5. 2-level recursive fallback when no candidates found
    /// </summary>
    /// <param name="dir">The game directory to search.</param>
    /// <param name="noiseExePatterns">Executable name substrings to exclude (e.g., "launcher", "setup").</param>
    /// <param name="noiseDirectoryPatterns">Directory name substrings to exclude.</param>
    internal static List<string> FindExecutablesDeep(
        DirectoryInfo dir,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlySet<string> noiseDirectoryPatterns)
    {
        var candidates = new List<string>();

        try
        {
            // 1. Root-level exes
            foreach (string exe in Directory.EnumerateFiles(dir.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (!IsNoiseExeByPath(exe, noiseExePatterns))
                    candidates.Add(exe);
            }

            // 2. Immediate child directories (skip noise dirs)
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (IsNoiseDirectory(child.Name, noiseDirectoryPatterns) || FileSystemHelper.NoiseSubDirNames.Contains(child.Name))
                    continue;

                foreach (string exe in Directory.EnumerateFiles(child.FullName, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    if (!IsNoiseExeByPath(exe, noiseExePatterns))
                        candidates.Add(exe);
                }

                // 3. UE Binaries paths — Win64, Win32, WinGDK, Steam
                // Scans all platforms (no early break) — matches detect.py _find_game_executables behavior.
                // Missing exes = games with no launch target. Extra candidates = scoring system filters them.
                foreach (string platform in s_uePlatformNames)
                {
                    string platPath = Path.Combine(child.FullName, "Binaries", platform);
                    if (!Directory.Exists(platPath)) continue;

                    foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsNoiseExeByPath(exe, noiseExePatterns))
                            candidates.Add(exe);
                    }
                }

                // 4. Older UE games — child/bin/ (Gothic, Jagged Alliance)
                string binPath = Path.Combine(child.FullName, "bin");
                if (Directory.Exists(binPath))
                {
                    foreach (string exe in Directory.EnumerateFiles(binPath, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsNoiseExeByPath(exe, noiseExePatterns))
                            candidates.Add(exe);
                    }
                }
            }
        }
        catch { }

        // 5. BioShock pattern — root has no exes, scan 2 levels deep
        if (candidates.Count == 0)
        {
            candidates.AddRange(FindExesRecursive(dir, noiseExePatterns, noiseDirectoryPatterns, maxDepth: 2));
        }

        // Deduplicate by full path
        var seen = new HashSet<string>();
        var unique = new List<string>();
        foreach (string exe in candidates)
        {
            if (seen.Add(exe))
                unique.Add(exe);
        }
        return unique;
    }

    /// <summary>
    /// UE platform directory names under Binaries/.
    /// Matches detect.py _find_game_executables (Win64, WinGDK) + _find_exe_in_subdirs (Win32, Steam).
    /// Linux dropped (Windows-only app).
    /// </summary>
    private static readonly string[] s_uePlatformNames = ["Win64", "Win32", "WinGDK", "Steam"];

    /// <summary>
    /// Walks subdirectories up to maxDepth, collecting non-noise executables.
    /// Used as a fallback when explicit path probes find nothing (BioShock pattern).
    /// Matches detect.py _add_exes_recursive with max_depth=2.
    /// </summary>
    private static List<string> FindExesRecursive(
        DirectoryInfo dir,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlySet<string> noiseDirectoryPatterns,
        int maxDepth,
        int depth = 0)
    {
        var results = new List<string>();
        if (depth > maxDepth) return results;

        try
        {
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (IsNoiseDirectory(child.Name, noiseDirectoryPatterns)
                    || FileSystemHelper.NoiseSubDirNames.Contains(child.Name))
                    continue;

                // Collect exes from this directory
                foreach (string exe in Directory.EnumerateFiles(child.FullName, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    if (!IsNoiseExeByPath(exe, noiseExePatterns))
                        results.Add(exe);
                }

                // Recurse if within depth limit
                if (depth < maxDepth)
                {
                    results.AddRange(FindExesRecursive(child, noiseExePatterns, noiseDirectoryPatterns, maxDepth, depth + 1));
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return results;
    }

    /// <summary>
    /// Scores an executable for primary selection. Higher score = more likely to be the real game.
    /// Considers folder-name token match, launcher penalty, noise pattern penalty (tier-based),
    /// shipping/win64 bonus, and file size.
    /// </summary>
    /// <param name="exePath">Full path to the executable.</param>
    /// <param name="folderName">Name of the game folder (used for token matching).</param>
    /// <param name="launcherPatterns">Launcher/updater name substrings (used for penalty scoring).</param>
    /// <param name="noiseExePatterns">Full noise pattern list for tier-based penalty scoring.</param>
    /// <param name="tierLookup">Function to look up the severity tier for a noise pattern.</param>
    internal static int ScoreExecutable(
        string exePath,
        string folderName,
        IReadOnlyList<string> launcherPatterns,
        IReadOnlyList<string> noiseExePatterns,
        Func<string, int> tierLookup)
    {
        int score = 0;
        string name = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
        string folderLower = folderName.ToLowerInvariant();

        // Folder name token match (+10 per matching token)
        char[] separators = [' ', '_', '-', '.', ':'];
        string[] folderTokens = folderLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in folderTokens)
        {
            if (token.Length > 1 && name.Contains(token))
                score += 10;
        }

        // Penalize launcher/updater/bootstrapper stubs (-20)
        if (launcherPatterns.Any(p => name.Contains(p)))
            score -= 20;

        // Penalize known noise patterns with tier-based severity
        foreach (string pattern in noiseExePatterns)
        {
            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                int tier = tierLookup(pattern);
                // Tier 1-5: -30 (universal noise, always non-game)
                // Tier 6-10: -20 (likely non-game)
                // Tier 11-15: -10 (possibly non-game)
                // Tier 16+: -5 (might be legitimate)
                int penalty = tier switch
                {
                    <= 5 => -30,
                    <= 10 => -20,
                    <= 15 => -10,
                    _ => -5
                };
                score += penalty;
                break; // Only penalize once (first match)
            }
        }

        // Bonus for "shipping" or "win64" in name (+5)
        if (name.Contains("shipping") || name.Contains("win64"))
            score += 5;

        // File size bonus: up to +10 for very large files (>= 100MB)
        try
        {
            long size = new FileInfo(exePath).Length;
            score += (int)Math.Min(size / 10_000_000, 10);
        }
        catch { }

        return score;
    }

    /// <summary>
    /// Finds the primary executable in a game directory by deep-searching and scoring candidates.
    /// Falls back to the largest top-level exe if deep search finds nothing.
    /// </summary>
    /// <param name="dir">The game directory.</param>
    /// <param name="topLevelExes">Fallback: top-level executables if deep search finds nothing.</param>
    /// <param name="noiseExePatterns">Executable name substrings to exclude.</param>
    /// <param name="noiseDirectoryPatterns">Directory name substrings to exclude.</param>
    /// <param name="launcherPatterns">Launcher name substrings for scoring penalty.</param>
    /// <param name="tierLookup">Function to look up the severity tier for a noise pattern.</param>
    internal static string? FindPrimaryExecutable(
        DirectoryInfo dir,
        string[] topLevelExes,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlySet<string> noiseDirectoryPatterns,
        IReadOnlyList<string> launcherPatterns,
        Func<string, int>? tierLookup = null)
    {
        var candidates = FindExecutablesDeep(dir, noiseExePatterns, noiseDirectoryPatterns);
        if (candidates.Count == 0)
        {
            // Fallback: if even deep search found nothing, try top-level exes
            if (topLevelExes.Length == 0) return null;
            return topLevelExes
                .OrderByDescending(f => new FileInfo(f).Length)
                .First();
        }

        if (candidates.Count == 1)
            return candidates[0];

        // Score all candidates and pick the best
        string folderName = dir.Name;
        Func<string, int> lookup = tierLookup ?? (_ => 999);
        return candidates
            .Select(exe => (Exe: exe, Score: ScoreExecutable(exe, folderName, launcherPatterns, noiseExePatterns, lookup)))
            .OrderByDescending(x => x.Score)
            .First().Exe;
    }

    /// <summary>
    /// Finds a launcher executable (e.g., GameLauncher.exe) among the game's root-level executables.
    /// Returns null if no launcher is found or if there's only one exe.
    /// </summary>
    /// <param name="dir">The game directory.</param>
    /// <param name="primaryExe">The primary exe to exclude from launcher search.</param>
    /// <param name="launcherPatterns">Launcher name substrings to match.</param>
    internal static string? FindLauncherExecutable(
        DirectoryInfo dir,
        string? primaryExe,
        IReadOnlyList<string> launcherPatterns)
    {
        string[] exeFiles = FileSystemHelper.GetFilesSafe(dir, "*.exe");
        if (exeFiles.Length <= 1) return null;

        foreach (string exe in exeFiles)
        {
            if (exe == primaryExe) continue;
            string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
            if (launcherPatterns.Any(ln => name.Contains(ln)))
                return exe;
        }

        return null;
    }

    /// <summary>
    /// Checks if an executable name (without extension) matches the folder name
    /// via bidirectional substring or token matching.
    /// </summary>
    internal static bool ExeNameMatchesFolderName(string exePath, string folderName)
    {
        string exeStem = Path.GetFileNameWithoutExtension(exePath);
        string folderLower = folderName.ToLowerInvariant();
        string exeLower = exeStem.ToLowerInvariant();

        if (folderLower.Contains(exeLower) || exeLower.Contains(folderLower))
            return true;

        char[] separators = [' ', '_', '-', '.', ':'];
        string[] folderTokens = folderLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        string[] exeTokens = exeLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        return folderTokens.Any(t => exeTokens.Contains(t) && t.Length > 1);
    }

    /// <summary>
    /// Searches for an Epic Games Store manifest file (.egsstore/manifests/*.json or .egstore/manifests/*.json).
    /// Returns the full path to the first manifest found, or empty string.
    /// </summary>
    internal static string FindEpicManifest(DirectoryInfo dir)
    {
        string[] egsPaths =
        [
            Path.Combine(dir.FullName, ".egsstore", "manifests"),
            Path.Combine(dir.FullName, ".egstore", "manifests"),
            Path.Combine(dir.FullName, "manifests"),
        ];

        foreach (string manifestsDir in egsPaths)
        {
            if (!Directory.Exists(manifestsDir)) continue;

            try
            {
                foreach (FileInfo jsonFile in new DirectoryInfo(manifestsDir).GetFiles("*.json"))
                {
                    return jsonFile.FullName;
                }
            }
            catch { }
        }

        return string.Empty;
    }

    // ── Private helpers ───────────────────────────────────────

    /// <summary>
    /// Checks if an executable file path matches any noise pattern. Extracts filename before checking.
    /// </summary>
    private static bool IsNoiseExeByPath(string exePath, IReadOnlyList<string> noiseExePatterns)
    {
        string name = Path.GetFileNameWithoutExtension(exePath);
        return FileSystemHelper.IsNoiseExeName(name, noiseExePatterns);
    }

    /// <summary>
    /// Checks if a directory name matches known noise patterns (saves, mods, etc.).
    /// </summary>
    private static bool IsNoiseDirectory(string dirName, IReadOnlySet<string> noiseDirectoryPatterns)
    {
        return FileSystemHelper.IsNoiseDirectory(dirName, noiseDirectoryPatterns);
    }
}
