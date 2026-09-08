using System.Text.RegularExpressions;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Discovers and scores executable files within a game directory.
/// Handles deep search (root, children, UE Binaries paths), primary exe selection,
/// launcher detection, and Epic manifest discovery.
/// </summary>
internal static partial class ExecutableDiscovery
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
    /// Parent-bound child promotion (E4/E5 — parity with detect.py, 2026-09-07).
    /// When the ONLY non-noise exes live inside a platform/build/redist child
    /// (system/, bin/, win64/, Binaries/, redist/, ...), the PARENT folder IS the
    /// game and the child is never promoted. Returns the relative path of the best
    /// exe scored against the PARENT folder name, or null when no parent-bound
    /// children hold a non-noise exe.
    /// Covers: Elex\system\ELEX.exe, Penumbra\redist\PENUMBRA.EXE.
    /// </summary>
    internal static string? FindParentBoundExe(DirectoryInfo dir, IReadOnlyList<string> noiseExePatterns)
    {
        var candidates = new List<string>();
        string root = dir.FullName;

        foreach (string childDirName in ParentBoundChildNames)
        {
            string childPath = Path.Combine(root, childDirName);
            if (!Directory.Exists(childPath))
                continue;

            try
            {
                // Direct children of the parent-bound dir
                foreach (string exe in Directory.EnumerateFiles(childPath, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    if (!IsNoiseExeByPath(exe, noiseExePatterns))
                        candidates.Add(exe);
                }
                // One level deeper (win64/binaries/, x64/, ...)
                foreach (string subDir in Directory.EnumerateDirectories(childPath))
                {
                    if (FileSystemHelper.NoiseSubDirNames.Contains(Path.GetFileName(subDir)))
                        continue;
                    foreach (string exe in Directory.EnumerateFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsNoiseExeByPath(exe, noiseExePatterns))
                            candidates.Add(exe);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        if (candidates.Count == 0)
            return null;

        // Score against the PARENT folder name (not the child) and pick the best.
        var best = candidates
            .OrderByDescending(exe => ScoreExecutable(exe, dir.Name, [], noiseExePatterns, _ => 999).Score)
            .First();
        return Path.GetRelativePath(root, best);
    }

    /// <summary>
    /// Parent-bound child dir names (mirror of detect.py _PARENT_BOUND_CHILD_DIRS).
    /// These belong to the parent game — never separate entries, never container
    /// triggers. Their exes are candidates for the parent (E4/E5).
    /// </summary>
    private static readonly string[] ParentBoundChildNames =
    [
        "system", "bin", "bin64", "win32", "win64", "x86", "x64",
        "binaries", "boot", "core", "game", "run", "app", "client",
        "engine", "crashsender", "common",
        "redist", "redistributable", "_installer", "install", "installer",
        "support", "directx", "vcredist",
    ];

    /// <summary>
    /// Terminating rule (Plan 123 E1, user model 2026-09-07; parity with
    /// detect.py `_find_exact_folder_match`).
    ///
    /// If an exe at or under *dir* (bounded depth) has a stem that EXACTLY matches
    /// the folder name, then the folder IS the game folder and that exe IS the
    /// game exe. Returns the shallowest matching relative path, or null.
    ///
    /// Two match forms (both terminating):
    ///   - TOKEN match:  stem == one folder token  (`Neverwinter_en` + `neverwinter.exe`)
    ///   - WHOLE-NAME match: stem, separators stripped, == folder name, separators
    ///     stripped (`Diablo III` + `Diablo III.exe`, `Dead Space 3` + `deadspace3.exe`)
    ///
    /// Backup exes (leading dash, "copy of", "- copy", numbered "10 org") are
    /// EXCLUDED — a backup must never satisfy the terminating rule (E5 guard:
    /// redist/PENUMBRA.EXE wins over redist/-Penumbra.exe).
    ///
    /// Search is bounded to rel depth 4 (81/102 real game exes sit at rel 1-4;
    /// deeper is noise — no deep walk). Noise-dir filters deliberately do NOT
    /// apply: the exact stem match is itself the guard (redist/penumbra,
    /// system/elex, subfolder/x64/bin/gamename.exe).
    /// </summary>
    internal static string? FindExactFolderMatch(DirectoryInfo dir, int maxRelDepth = 4)
    {
        string folderName = dir.Name.ToLowerInvariant();
        string[] folderTokens = folderName
            .Replace("_", " ").Replace("-", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string folderWhole = Regex.Replace(folderName, @"[\s_\-]+", string.Empty);
        if (folderTokens.Length == 0 && folderWhole.Length == 0)
            return null;

        static bool StemMatches(string stem, string[] tokens, string whole)
        {
            // Backups never terminate (E5 guard)
            if (stem.StartsWith("-", StringComparison.Ordinal)
                || stem.StartsWith("copy of ", StringComparison.Ordinal)
                || stem.Contains(" - copy", StringComparison.Ordinal))
            {
                return false;
            }
            // Token match
            foreach (string t in tokens)
            {
                if (stem == t)
                    return true;
            }
            // Whole-name normalized match (GAP A)
            string stemNorm = Regex.Replace(stem, @"[\s_\-]+", string.Empty);
            return stemNorm.Length > 0 && stemNorm == whole;
        }

        // Bounded walk: root + subdirs up to maxRelDepth. Track shallowest match.
        (int Depth, string RelPath)? best = null;
        string rootFull = dir.FullName;
        try
        {
            foreach (string file in Directory.EnumerateFiles(rootFull, "*.exe", SearchOption.AllDirectories))
            {
                string full = file;
                // Compute rel depth from the folder
                string rel = Path.GetRelativePath(rootFull, full);
                int depth = rel.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
                if (depth > maxRelDepth)
                    continue;

                string fname = Path.GetFileName(full);
                if (!fname.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    continue;
                // Python noise gate equivalent: skip clearly-noise exes
                if (IsNoiseExeByPath(full, NoiseSkipForTerminating))
                    continue;

                string stem = Path.GetFileNameWithoutExtension(fname).ToLowerInvariant();
                if (StemMatches(stem, folderTokens, folderWhole))
                {
                    if (best is null || depth < best.Value.Depth)
                        best = (depth, rel);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }

        return best?.RelPath;
    }

    /// <summary>
    /// Noise patterns used ONLY by the terminating rule. Kept deliberately small:
    /// the exact stem match is the guard. Installers/updaters/bootstraps that
    /// happen to share the folder name must not terminate.
    /// </summary>
    private static readonly string[] NoiseSkipForTerminating =
    [
        "install", "setup", "updater", "launcher", "bootstrap", "unins",
        "redist", "vcredist", "dxsetup", "oalinst", "crash",
    ];
    /// <summary>
    /// Removes platform/binary-type tokens from a letters+digits name key so
    /// "mygamewin64shipping" compares equal to folder key "mygame".
    /// </summary>
    private static string StripPlatformTokens(string lettersAndDigitsKey)
    {
        string result = lettersAndDigitsKey;
        foreach (string token in (string[])["win64", "win32", "wingdk", "shipping"])
            result = result.Replace(token, string.Empty, StringComparison.Ordinal);
        return result;
    }

    /// <summary>
    /// Result of finding the primary executable in a game directory.
    /// </summary>
    internal sealed record PrimaryExeResult(
        string? ExePath,
        string? FileDescription,
        IReadOnlyList<string> Candidates);

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
    internal static PrimaryExeResult FindPrimaryExecutable(
        DirectoryInfo dir,
        string[] topLevelExes,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlySet<string> noiseDirectoryPatterns,
        IReadOnlyList<string> launcherPatterns,
        Func<string, int>? tierLookup = null)
    {
        var candidates = FindExecutablesDeep(dir, noiseExePatterns, noiseDirectoryPatterns)
            .Where(p => !IsForbiddenLaunchExe(p))
            .ToList();
        if (candidates.Count == 0)
        {
            var fallback = topLevelExes
                .Where(p => !IsForbiddenLaunchExe(p) && !IsNoiseExeByPath(p, noiseExePatterns))
                .ToList();
            if (fallback.Count == 0)
                return new PrimaryExeResult(null, null, []);
            string best = fallback
                .OrderByDescending(f => { try { return new FileInfo(f).Length; } catch { return 0; } })
                .First();
            return new PrimaryExeResult(best, null, fallback);
        }

        if (candidates.Count == 1)
        {
            // Read PE metadata for single candidate to capture FileDescription
            string folderName = dir.Name;
            Func<string, int> singleLookup = tierLookup ?? (_ => 999);
            var result = ScoreExecutable(candidates[0], folderName, launcherPatterns, noiseExePatterns, singleLookup);
            return new PrimaryExeResult(candidates[0], result.FileDescription, candidates);
        }

        // Score all candidates and pick the best
        string folderNameForScoring = dir.Name;
        Func<string, int> lookup = tierLookup ?? (_ => 999);
        var scored = candidates
            .Select(exe => (Exe: exe, Result: ScoreExecutable(exe, folderNameForScoring, launcherPatterns, noiseExePatterns, lookup)))
            .OrderByDescending(x => x.Result.Score)
            .First();
        return new PrimaryExeResult(scored.Exe, scored.Result.FileDescription, candidates);
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
        if (TitleText.MatchesFolderAndExe(folderName, Path.GetFileNameWithoutExtension(exePath)))
            return true;

        string exeStem = Path.GetFileNameWithoutExtension(exePath);
        string folderLower = TitleText.ForSearch(folderName).ToLowerInvariant();
        string exeLower = exeStem.ToLowerInvariant();
        string folderKey = TitleText.LettersAndDigits(folderLower);
        string exeKey = TitleText.LettersAndDigits(exeLower);

        if (folderLower.Contains(exeLower) || exeLower.Contains(folderLower)
            || (folderKey.Length > 2 && (folderKey == exeKey || folderKey.Contains(exeKey) || exeKey.Contains(folderKey))))
            return true;

        char[] separators = [' ', '_', '-', '.', ':'];
        string[] folderTokens = folderLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        string[] exeTokens = exeLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        return folderTokens.Any(t => exeTokens.Contains(t) && t.Length > 1);
    }

    /// <summary>
    /// Searches for an Epic Games Store manifest file in .egsstore/manifests/ or .egstore/manifests/.
    /// Searches .item first (richer schema), then .mancpn, then .json (legacy).
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
                // .item preferred (richest schema), then .mancpn, then .json (legacy)
                foreach (string pattern in new[] { "*.item", "*.mancpn", "*.json" })
                {
                    foreach (FileInfo file in new DirectoryInfo(manifestsDir).GetFiles(pattern))
                    {
                        return file.FullName;
                    }
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
    /// <summary>
    /// Uninstaller / Inno Setup stubs. Never a launch target or F4 pick — even if
    /// every other exe was filtered as noise (fallback used to pick the largest, often unins000).
    /// </summary>
    internal static bool IsForbiddenLaunchExe(string? pathOrName)
    {
        if (string.IsNullOrWhiteSpace(pathOrName))
            return false;

        string name = WindowsFileStem(pathOrName);
        if (name.Length == 0)
            return false;
        if (name.StartsWith("unins", StringComparison.OrdinalIgnoreCase))
            return true;
        if (name.Contains("uninstall", StringComparison.OrdinalIgnoreCase))
            return true;
        return name.Equals("unwise", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Filename stem; treats <c>\</c> as separator so Windows paths work on Linux.</summary>
    private static string WindowsFileStem(string pathOrName)
    {
        string text = pathOrName.Trim().Replace('/', '\\');
        int slash = text.LastIndexOf('\\');
        string file = slash >= 0 ? text[(slash + 1)..] : text;
        if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return file[..^4];
        int dot = file.LastIndexOf('.');
        return dot > 0 ? file[..dot] : file;
    }

    private static bool IsNoiseExeByPath(string exePath, IReadOnlyList<string> noiseExePatterns)
    {
        if (IsForbiddenLaunchExe(exePath))
            return true;
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
