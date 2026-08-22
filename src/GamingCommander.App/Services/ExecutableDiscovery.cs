using System.Text.RegularExpressions;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

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
    /// Result of scoring an executable for primary selection.
    /// Contains the numeric score and the PE FileDescription (if read successfully).
    /// </summary>
    internal sealed record ExeScoreResult(int Score, string? FileDescription);

    /// <summary>
    /// Scores an executable for primary selection. Higher score = more likely to be the real game.
    /// Considers folder-name token match, launcher penalty, noise pattern penalty (tier-based),
    /// shipping/win64 bonus, file size, and PE metadata.
    /// </summary>
    /// <param name="exePath">Full path to the executable.</param>
    /// <param name="folderName">Name of the game folder (used for token matching).</param>
    /// <param name="launcherPatterns">Launcher/updater name substrings (used for penalty scoring).</param>
    /// <param name="noiseExePatterns">Full noise pattern list for tier-based penalty scoring.</param>
    /// <param name="tierLookup">Function to look up the severity tier for a noise pattern.</param>
    internal static ExeScoreResult ScoreExecutable(
        string exePath,
        string folderName,
        IReadOnlyList<string> launcherPatterns,
        IReadOnlyList<string> noiseExePatterns,
        Func<string, int> tierLookup)
    {
        int score = 0;
        string name = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
        string folderLower = TitleText.ForSearch(folderName).ToLowerInvariant();
        string nameKey = TitleText.LettersAndDigits(name);
        string folderKey = TitleText.LettersAndDigits(folderLower);

        // Bonus for exe name containing folder name (+15)
        if (name.Contains(folderLower) || (folderKey.Length > 2 && nameKey == folderKey))
            score += 15;
        // Bonus for folder name containing exe stem (+15)
        else if (folderLower.Contains(name) || (nameKey.Length > 2 && folderKey.Contains(nameKey)))
            score += 15;

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

        // Penalize backup copies / cracks / org groups (org_ and 12-org-game)
        if (name.Contains("copy of") || name.Contains(" - copy"))
            score -= 25;
        else if (name.Contains("org_", StringComparison.Ordinal)
                 || name.Contains("-org-", StringComparison.Ordinal)
                 || name.Contains("-org_", StringComparison.Ordinal)
                 || name.StartsWith("org_", StringComparison.Ordinal)
                 || name.StartsWith("org-", StringComparison.Ordinal)
                 || Regex.IsMatch(name, @"^\d{1,3}-org"))
            score -= 25;
        else if (name.Contains("original"))
            score -= 15;
        if (name.Contains("crack"))
            score -= 25;

        // Classic main binary names (Silent Storm, many 2000s titles)
        if (name is "game" or "start" or "play")
            score += 18;

        score += RomanNumeralBonus(name, folderLower, folderTokens);
        score += AbbreviationBonus(name, folderLower);

        // Cache FileInfo.Length — avoid redundant filesystem syscalls (Plan 112 Step 4B)
        long fileSize = 0;
        try
        {
            fileSize = new FileInfo(exePath).Length;
            if (fileSize < 100_000) // < 100KB
                score -= 15;
        }
        catch { }

        // Penalize known noise patterns with tier-based severity
        bool isHighSeverityNoise = false;
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
                isHighSeverityNoise = tier <= 4;
                break; // Only penalize once (first match)
            }
        }

        // Unreal shipping binary beats root game.exe / launcher stubs
        string pathLower = exePath.Replace('/', '\\').ToLowerInvariant();
        bool shippingName = name.Contains("-win64-shipping")
            || name.Contains("-win32-shipping")
            || name.Contains("-wingdk-shipping")
            || name.EndsWith("-shipping", StringComparison.Ordinal);
        if (shippingName)
            score += 28;
        else if (name.Contains("shipping") || name.Contains("win64"))
            score += 5;

        if (pathLower.Contains(@"\binaries\win64\")
            || pathLower.Contains(@"\binaries\win32\")
            || pathLower.Contains(@"\binaries\wingdk\")
            || pathLower.Contains(@"\shipping\"))
            score += 12;

        // File size bonus: up to +5 for very large files (>= 100MB)
        // Uses cached fileSize from above (Plan 112 Step 4B)
        score += (int)Math.Min(fileSize / 20_000_000, 5);

        // PE metadata: skip read for confirmed-noise candidates (Plan 112 Step 4C).
        // A -30 penalty from Tier 1-4 noise cannot be rescued by PE metadata bonuses.
        string? fileDescription = null;
        if (!isHighSeverityNoise)
        {
            // PE metadata scoring: penalize noise patterns in Description/InternalName.
            // Uses System.Diagnostics.FileVersionInfo — built into .NET, no external dependencies.
            // Gracefully degrades on read failure (old/broken PE headers).
            try
            {
                var peInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                string desc = (peInfo.FileDescription ?? "").ToLowerInvariant();
                string internalName = (peInfo.InternalName ?? "").ToLowerInvariant();

                // Capture FileDescription for display name enrichment (Plan 112 Step 2)
                string? rawDescription = peInfo.FileDescription;
                if (!string.IsNullOrWhiteSpace(rawDescription))
                    fileDescription = rawDescription;

                // Penalize noise in FileDescription (-25)
                if (desc.Contains("setup") || desc.Contains("microsoft") ||
                    desc.Contains("uninstall") || desc.Contains("redistributable") ||
                    desc.Contains("directx") || desc.Contains("cabinet"))
                    score -= 25;

                // Penalize noise in InternalName (-20)
                if (internalName == "setup" || internalName.Contains("launcher") ||
                    internalName.Contains("uninstall") || internalName.Contains("crash") ||
                    internalName.Contains("error"))
                    score -= 20;

                // Bonus for game-like descriptions (+10)
                if (desc.Contains("retail") || desc.Contains("client") ||
                    desc.Contains("shipping"))
                    score += 10;
            }
            catch
            {
                // PE read failed (broken header, old exe, etc.) — continue with existing score
            }
        }

        return new ExeScoreResult(score, fileDescription);
    }

    /// <summary>
    /// Plan 103: +12 when a folder digit token matches a roman numeral in the exe stem, or vice versa
    /// (e.g. folder "heroes 4" vs exe "heroesiv").
    /// </summary>
    private static int RomanNumeralBonus(string exeStem, string folderLower, string[] folderTokens)
    {
        foreach (var (digit, roman) in s_romanDigits)
        {
            bool folderHasDigit = folderTokens.Contains(digit);
            bool folderHasRoman = folderLower.Contains(roman);
            bool exeHasDigit = exeStem.Contains(digit);
            bool exeHasRoman = exeStem.Contains(roman);
            if ((folderHasDigit && exeHasRoman) || (folderHasRoman && exeHasDigit))
                return 12;
        }

        return 0;
    }

    /// <summary>
    /// Plan 103: +8 when a short exe stem (2–4 chars) is an ordered abbreviation of the folder name
    /// sharing the first letter (e.g. "hk" for "hollow knight").
    /// </summary>
    private static int AbbreviationBonus(string exeStem, string folderLower)
    {
        if (exeStem.Length is < 2 or > 4)
            return 0;
        if (folderLower.Length == 0 || exeStem[0] != folderLower[0])
            return 0;

        int fi = 0;
        foreach (char c in exeStem)
        {
            int found = folderLower.IndexOf(c, fi);
            if (found < 0)
                return 0;
            fi = found + 1;
        }

        return 8;
    }

    private static readonly (string Digit, string Roman)[] s_romanDigits =
    [
        ("2", "ii"), ("3", "iii"), ("4", "iv"), ("5", "v"),
        ("6", "vi"), ("7", "vii"), ("8", "viii"), ("9", "ix"),
    ];

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
