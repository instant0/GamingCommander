using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

public sealed class FolderScanner
{
    private readonly IReadOnlySet<string> _hiddenFolderNames;
    private readonly IReadOnlyList<string> _noiseExePatterns;
    private readonly IReadOnlySet<string> _noiseDirectoryPatterns;
    private readonly IReadOnlyList<string> _launcherPatterns;

    /// <summary>
    /// Default hardcoded patterns for backward compatibility (tests, etc.).
    /// Production code should use the constructor that accepts BlacklistData.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultNoiseExePatterns =
    [
        "launcher", "launch", "updater", "bootstrap", "redlaunch",
        "epicgameslauncher", "goggalaxy", "ea app", "eaapp", "ubisoft",
        "anticheat", "easyanticheat", "eac", "battleye", "punkbuster",
        "installer", "setup", "redist", "commonredist", "vcredist",
        "dxsetup", "oalinst", "dotnetruntime", "directx", "xna",
        "unins", "uninstall",
    ];

    private static readonly IReadOnlyList<string> DefaultLauncherPatterns =
    [
        "launcher", "launch", "updater", "bootstrap", "redlaunch",
        "epicgameslauncher", "goggalaxy", "ea app", "ubisoft",
    ];

    // Directories to skip during deep executable search
    private static readonly IReadOnlySet<string> NoiseSubDirNames = new HashSet<string>(
    [
        "__redist", "_commonredist", "commonredist", "redist", "directx",
        "vcredist", "dotnet", "physx", "support", "_installer", "install",
        "installer", "easyanticheat", "devtools", "docs", "licenses",
        "steam controller configs", "steamworks shared",
    ], StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a scanner with default hardcoded noise patterns (for tests and simple cases).</summary>
    public FolderScanner()
        : this([], DefaultNoiseExePatterns, [], DefaultLauncherPatterns)
    {
    }

    /// <summary>Creates a scanner with custom hidden folder names and default noise patterns.</summary>
    public FolderScanner(IEnumerable<string> hiddenFolderNames)
        : this(hiddenFolderNames, DefaultNoiseExePatterns, [], DefaultLauncherPatterns)
    {
    }

    /// <summary>Creates a scanner with hidden folder names and blacklist data from data/blacklist.json.</summary>
    public FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        BlacklistData blacklist)
        : this(
            hiddenFolderNames,
            blacklist.ExeNamePatterns.Count > 0 ? blacklist.ExeNamePatterns : DefaultNoiseExePatterns,
            blacklist.DirectoryPatterns,
            DefaultLauncherPatterns)
    {
    }

    private FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlyList<string> noiseDirectoryPatterns,
        IReadOnlyList<string> launcherPatterns)
    {
        _hiddenFolderNames = new HashSet<string>(hiddenFolderNames, StringComparer.OrdinalIgnoreCase);
        _noiseExePatterns = noiseExePatterns;
        _noiseDirectoryPatterns = new HashSet<string>(noiseDirectoryPatterns, StringComparer.OrdinalIgnoreCase);
        _launcherPatterns = launcherPatterns;
    }

    // ════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scans a library root for game folders using a 10-signal priority-ordered detection chain.
    /// Returns game entries with detected source types, executables, and metadata.
    /// </summary>
    public IReadOnlyList<GameEntry> Scan(string rootPath, GameSourceKind defaultType)
    {
        if (!Directory.Exists(rootPath))
            return [];

        var entries = new List<GameEntry>();

        foreach (DirectoryInfo subDir in GetDirectoriesSafe(rootPath))
        {
            if (_hiddenFolderNames.Count > 0 && _hiddenFolderNames.Contains(subDir.Name))
                continue;

            if (IsNoiseDirectory(subDir.Name))
                continue;

            // Skip architecture subdirectories (Win32, Win64, x86, etc.) —
            // these are never games, just contain platform-specific binaries.
            if (NoiseSubDirNames.Contains(subDir.Name))
                continue;

            // Pass 1: Check for launcher/store signals at this folder level
            GameSourceKind signalType = DetectType(subDir);

            if (signalType != GameSourceKind.Unknown)
            {
                // Tier 1 — High confidence. Always create an entry.
                AddGameEntry(entries, subDir, rootPath, signalType, defaultType);
                continue;
            }

            // Pass 2: Check for standalone signals (root exe, unreal layout, etc.)
            GameSourceKind fallbackType = DetectFallbackType(subDir);

            if (fallbackType != GameSourceKind.Unknown)
            {
                AddGameEntry(entries, subDir, rootPath, fallbackType, defaultType);
                continue;
            }

            // Pass 3: No signals at all — check if this is a container folder
            // (organizer whose immediate children have launcher signals)
            ScanContainerChildren(entries, subDir, rootPath, defaultType);
        }

        return entries;
    }

    // ════════════════════════════════════════════════════════════════
    //  Tier 1 — Priority-Ordered Store/Launcher Signal Detection
    //  Order matches the research docs: GOG → EA → Ubisoft Emu →
    //  Ubisoft → Epic → Blizzard → Xbox → Rockstar → Steam → Steam Emu
    // ════════════════════════════════════════════════════════════════

    private static GameSourceKind DetectType(DirectoryInfo subDir)
    {
        // 1 — GOG: goggame* files at root
        if (HasGogSignal(subDir))
            return GameSourceKind.Gog;

        // 2 — EA: __Installer/ directory at root
        if (HasEaSignal(subDir))
            return GameSourceKind.EaApp;

        // 3 — Ubisoft Emulator: uplay_loader* + INI with username/accountid
        if (HasUbisoftEmulatorSignal(subDir))
            return GameSourceKind.UbisoftConnect;

        // 4 — Ubisoft: uplay_install.manifest / uplay_r*_loader*.dll
        if (HasUbisoftSignal(subDir))
            return GameSourceKind.UbisoftConnect;

        // 5 — Epic: .egstore/ or .egsstore/ directory at root
        if (HasEpicSignal(subDir))
            return GameSourceKind.Epic;

        // 6 — Blizzard: .battle.net/ directory at root
        if (HasBlizzardSignal(subDir))
            return GameSourceKind.BattleNet;

        // 7 — Xbox: default-metadata.json at root
        if (HasXboxSignal(subDir))
            return GameSourceKind.Xbox;

        // 8 — Rockstar: title.rgl at root
        if (HasRockstarSignal(subDir))
            return GameSourceKind.Rockstar;

        // 9 — Steam Emu (strong signal): steam_api64.dll / steam_api.dll at root
        //     The actual Steam API redistributable is definitive for Steam API usage.
        if (HasSteamEmulatorSignal(subDir))
            return GameSourceKind.SteamEmu;

        // 10 — Steam Emu (weak signal): steam_appid.txt alone.
        //     Many standalone Unity/Unreal games include this for Steam integration
        //     without being actual Steam Store games. We keep it as a Tier-1 signal
        //     (not fallback) so container detection promotes children with it.
        //     Classification is SteamEmu, NOT Steam — real Steam library detection
        //     is handled by SteamLibraryScanner using structural path.
        if (HasSteamSignal(subDir))
            return GameSourceKind.SteamEmu;

        return GameSourceKind.Unknown;
    }

    // ── Signal check helpers ────────────────────────────────────

    private static bool HasGogSignal(DirectoryInfo dir)
    {
        return GetFilesSafe(dir, "goggame*").Length > 0;
    }

    private static bool HasEaSignal(DirectoryInfo dir)
    {
        return Directory.Exists(Path.Combine(dir.FullName, "__Installer"));
    }

    private static bool HasUbisoftEmulatorSignal(DirectoryInfo dir)
    {
        // Must have a uplay loader executable AND an INI with Username=/AccountId=
        bool hasLoader = false;
        try
        {
            foreach (string file in Directory.EnumerateFiles(dir.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file).ToLowerInvariant();
                if (name.StartsWith("uplay_loader") || name.StartsWith("uplay_r"))
                    hasLoader = true;
                if (hasLoader && name.EndsWith(".ini"))
                {
                    try
                    {
                        string text = File.ReadAllText(file);
                        if (text.Contains("Username=", StringComparison.OrdinalIgnoreCase) &&
                            text.Contains("AccountId=", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    catch { }
                }
            }
        }
        catch { }
        return false;
    }

    private static bool HasUbisoftSignal(DirectoryInfo dir)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(dir.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file).ToLowerInvariant();
                if (name == "uplay_install.manifest" || name == "uplay_install.state")
                    return true;
                if (name is "uplay_r1_loader64.dll" or "uplay_r2_loader64.dll"
                    or "uplay_r1_loader32.dll" or "uplay_r2_loader32.dll")
                    return true;
            }
        }
        catch { }
        return false;
    }

    private static bool HasEpicSignal(DirectoryInfo dir)
    {
        return Directory.Exists(Path.Combine(dir.FullName, ".egstore"))
            || Directory.Exists(Path.Combine(dir.FullName, ".egsstore"));
    }

    private static bool HasBlizzardSignal(DirectoryInfo dir)
    {
        return Directory.Exists(Path.Combine(dir.FullName, ".battle.net"));
    }

    private static bool HasXboxSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "default-metadata.json"));
    }

    private static bool HasRockstarSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "title.rgl"));
    }

    private static bool HasSteamSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "steam_appid.txt"));
    }

    private static bool HasSteamEmulatorSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "steam_api64.dll"))
            || File.Exists(Path.Combine(dir.FullName, "steam_api.dll"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Pass 2 — Deep Fallback Detection (Medium/Low Confidence)
    // ════════════════════════════════════════════════════════════════

    private GameSourceKind DetectFallbackType(DirectoryInfo subDir)
    {
        // 1 — Steam Emulator deep: steam_emu.ini at root, child, or UE path
        if (HasSteamEmuDeepSignal(subDir))
            return GameSourceKind.SteamEmu;

        // 2 — Ubisoft legacy: UbiStats.dll at root or immediate child
        if (HasUbisoftLegacySignal(subDir))
            return GameSourceKind.UbisoftConnect;

        // 3 — Standalone (Unreal layout): Engine/ + */Binaries/Win64/*.exe
        if (HasUnrealLayoutSignal(subDir))
            return GameSourceKind.Standalone;

        // 4 — Standalone: any non-noise .exe at root
        if (HasRootExecutableSignal(subDir))
            return GameSourceKind.Standalone;

        // 5 — Standalone: .lnk shortcut at root
        if (HasRootLnkSignal(subDir))
            return GameSourceKind.Standalone;

        return GameSourceKind.Unknown;
    }

    // ── Deep signal check helpers ───────────────────────────────

    private static bool HasSteamEmuDeepSignal(DirectoryInfo dir)
    {
        try
        {
            // Check root
            if (File.Exists(Path.Combine(dir.FullName, "steam_emu.ini")))
                return true;

            // Check immediate children
            foreach (DirectoryInfo child in GetDirectoriesSafe(dir.FullName))
            {
                if (File.Exists(Path.Combine(child.FullName, "steam_emu.ini")))
                    return true;
            }

            // UE Steamworks path: Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/
            string swPath = Path.Combine(dir.FullName, "Engine", "Binaries", "ThirdParty", "Steamworks");
            if (Directory.Exists(swPath))
            {
                foreach (string svDir in Directory.GetDirectories(swPath))
                {
                    string win64 = Path.Combine(svDir, "Win64");
                    if (Directory.Exists(win64) && File.Exists(Path.Combine(win64, "steam_emu.ini")))
                        return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static bool HasUbisoftLegacySignal(DirectoryInfo dir)
    {
        try
        {
            // Check root
            if (File.Exists(Path.Combine(dir.FullName, "UbiStats.dll")))
                return true;

            // Check immediate children
            foreach (DirectoryInfo child in GetDirectoriesSafe(dir.FullName))
            {
                if (File.Exists(Path.Combine(child.FullName, "UbiStats.dll")))
                    return true;
            }
        }
        catch { }
        return false;
    }

    private bool HasUnrealLayoutSignal(DirectoryInfo dir)
    {
        // Need Engine/ directory
        string enginePath = Path.Combine(dir.FullName, "Engine");
        if (!Directory.Exists(enginePath))
            return false;

        // Check for any child with Binaries/Win64/*.exe
        try
        {
            foreach (DirectoryInfo child in GetDirectoriesSafe(dir.FullName))
            {
                if (child.Name == "Engine") continue;
                string win64 = Path.Combine(child.FullName, "Binaries", "Win64");
                if (Directory.Exists(win64))
                {
                    foreach (string exe in Directory.EnumerateFiles(win64, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                        if (!IsNoiseExeName(name))
                            return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }

    private bool HasRootExecutableSignal(DirectoryInfo dir)
    {
        try
        {
            foreach (string exe in Directory.EnumerateFiles(dir.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                if (!IsNoiseExeName(name))
                    return true;
            }
        }
        catch { }
        return false;
    }

    private static bool HasRootLnkSignal(DirectoryInfo dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir.FullName, "*.lnk", SearchOption.TopDirectoryOnly).Any();
        }
        catch { }
        return false;
    }

    // ════════════════════════════════════════════════════════════════
    //  Pass 3 — Container Detection
    //  A container is a folder with no signals itself, but whose
    //  immediate child has Pass 1 (launcher/store) signals.
    //  Children with only standalone signals do NOT qualify.
    // ════════════════════════════════════════════════════════════════

    private void ScanContainerChildren(
        List<GameEntry> entries, DirectoryInfo containerDir,
        string rootPath, GameSourceKind defaultType)
    {
        foreach (DirectoryInfo child in GetDirectoriesSafe(containerDir.FullName))
        {
            if (_hiddenFolderNames.Contains(child.Name))
                continue;

            GameSourceKind childType = DetectType(child);

            // Only promote children with Tier 1 (launcher) signals
            if (childType != GameSourceKind.Unknown)
            {
                AddGameEntry(entries, child, rootPath, childType, defaultType);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Executable Discovery
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds all non-noise executables within a game folder, searching:
    /// 1. Root directory
    /// 2. Immediate child directories (skipping noise dirs)
    /// 3. Binaries/Win64/ and Binaries/WinGDK/ paths in children
    /// </summary>
    private List<string> FindExecutablesDeep(DirectoryInfo dir)
    {
        var candidates = new List<string>();

        try
        {
            // 1. Root-level exes
            foreach (string exe in Directory.EnumerateFiles(dir.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (!IsNonGameExe(exe))
                    candidates.Add(exe);
            }

            // 2. Immediate child directories (skip noise dirs)
            foreach (DirectoryInfo child in GetDirectoriesSafe(dir.FullName))
            {
                if (IsNoiseDirectory(child.Name) || NoiseSubDirNames.Contains(child.Name))
                    continue;

                foreach (string exe in Directory.EnumerateFiles(child.FullName, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    if (!IsNonGameExe(exe))
                        candidates.Add(exe);
                }

                // 3. Binaries/Win64/ and Binaries/WinGDK/
                string win64 = Path.Combine(child.FullName, "Binaries", "Win64");
                if (Directory.Exists(win64))
                {
                    foreach (string exe in Directory.EnumerateFiles(win64, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsNonGameExe(exe))
                            candidates.Add(exe);
                    }
                }

                string winGdk = Path.Combine(child.FullName, "Binaries", "WinGDK");
                if (Directory.Exists(winGdk))
                {
                    foreach (string exe in Directory.EnumerateFiles(winGdk, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsNonGameExe(exe))
                            candidates.Add(exe);
                    }
                }
            }
        }
        catch { }

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
    /// Score an executable for primary selection.
    /// Higher score = more likely to be the real game.
    /// </summary>
    private int ScoreExecutable(string exePath, string folderName)
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
        if (_launcherPatterns.Any(p => name.Contains(p)))
            score -= 20;

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

    private string? FindPrimaryExecutable(DirectoryInfo dir, string[] topLevelExes)
    {
        var candidates = FindExecutablesDeep(dir);
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
        return candidates
            .Select(exe => (Exe: exe, Score: ScoreExecutable(exe, folderName)))
            .OrderByDescending(x => x.Score)
            .First().Exe;
    }

    private string? FindLauncherExecutable(DirectoryInfo dir, string? primaryExe)
    {
        string[] exeFiles = GetFilesSafe(dir, "*.exe");
        if (exeFiles.Length <= 1) return null;

        foreach (string exe in exeFiles)
        {
            if (exe == primaryExe) continue;
            string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
            if (_launcherPatterns.Any(ln => name.Contains(ln)))
                return exe;
        }

        return null;
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    private bool IsNoiseDirectory(string dirName)
    {
        if (_noiseDirectoryPatterns.Count == 0)
            return false;

        string lower = dirName.ToLowerInvariant();
        return _noiseDirectoryPatterns.Any(p => lower.Contains(p));
    }

    private static bool IsNoiseExePattern(string name)
    {
        // Check against common patterns built-in (for static contexts)
        foreach (string p in DefaultNoiseExePatterns)
        {
            if (name.Contains(p))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Instance method: checks if an exe name (without extension) matches the full
    /// JSON blacklist. Used by HasRootExecutableSignal and HasUnrealLayoutSignal
    /// so presence detection uses the same data as candidate filtering.
    /// </summary>
    private bool IsNoiseExeName(string name)
    {
        return _noiseExePatterns.Any(p => name.Contains(p));
    }

    private bool IsNonGameExe(string exePath)
    {
        string name = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
        return _noiseExePatterns.Any(p => name.Contains(p));
    }

    private static bool ExeNameMatchesFolderName(string exePath, string folderName)
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

    private static string NormalizeDisplayName(string folderName)
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

    private static string FindEpicManifest(DirectoryInfo dir)
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

    private void AddGameEntry(
        List<GameEntry> entries, DirectoryInfo subDir,
        string rootPath, GameSourceKind resolvedType, GameSourceKind rootDefault)
    {
        bool isOverride = resolvedType != rootDefault;
        string[] exeFiles = GetFilesSafe(subDir, "*.exe");
        string? exePath = FindPrimaryExecutable(subDir, exeFiles);
        string? launcherPath = FindLauncherExecutable(subDir, exePath);
        string manifestPath = FindEpicManifest(subDir);
        string displayName = NormalizeDisplayName(subDir.Name);
        string id = GameEntryId.Compute(rootPath, subDir.Name);

        entries.Add(new GameEntry(
            Id: id,
            FolderName: subDir.Name,
            DisplayName: displayName,
            GameSource: resolvedType,
            Override: isOverride,
            ExecutablePath: exePath ?? string.Empty,
            LauncherPath: launcherPath ?? string.Empty,
            CmdlineArgs: string.Empty,
            ManifestPath: manifestPath,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: GetLastWriteTimeSafe(subDir),
            Extra: []));
    }

    private static DirectoryInfo[] GetDirectoriesSafe(string path)
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

    private static string[] GetFilesSafe(DirectoryInfo dir, string pattern)
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

    private static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)
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
}
