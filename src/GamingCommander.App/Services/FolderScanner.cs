using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

public sealed class FolderScanner
{
    private readonly IReadOnlySet<string> _hiddenFolderNames;
    private readonly IReadOnlyList<string> _noiseExePatterns;
    private readonly IReadOnlySet<string> _noiseDirectoryPatterns;
    private readonly IReadOnlyList<string> _launcherPatterns;
    private readonly IReadOnlyList<BlacklistTierEntry> _tieredNoiseExePatterns;

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

    /// <summary>Creates a scanner with default hardcoded noise patterns (for tests and simple cases).</summary>
    public FolderScanner()
        : this([], DefaultNoiseExePatterns, [], DefaultLauncherPatterns, [])
    {
    }

    /// <summary>Creates a scanner with custom hidden folder names and default noise patterns.</summary>
    public FolderScanner(IEnumerable<string> hiddenFolderNames)
        : this(hiddenFolderNames, DefaultNoiseExePatterns, [], DefaultLauncherPatterns, [])
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
            DefaultLauncherPatterns,
            blacklist.TieredExePatterns)
    {
    }

    private FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlyList<string> noiseDirectoryPatterns,
        IReadOnlyList<string> launcherPatterns,
        IReadOnlyList<BlacklistTierEntry> tieredNoiseExePatterns)
    {
        _hiddenFolderNames = new HashSet<string>(hiddenFolderNames, StringComparer.OrdinalIgnoreCase);
        _noiseExePatterns = noiseExePatterns;
        _noiseDirectoryPatterns = new HashSet<string>(noiseDirectoryPatterns, StringComparer.OrdinalIgnoreCase);
        _launcherPatterns = launcherPatterns;
        _tieredNoiseExePatterns = tieredNoiseExePatterns;
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

        foreach (DirectoryInfo subDir in FileSystemHelper.GetDirectoriesSafe(rootPath))
        {
            if (_hiddenFolderNames.Count > 0 && _hiddenFolderNames.Contains(subDir.Name))
                continue;

            if (IsNoiseDirectory(subDir.Name))
                continue;

            // Skip architecture subdirectories (Win32, Win64, x86, etc.) —
            // these are never games, just contain platform-specific binaries.
            if (FileSystemHelper.NoiseSubDirNames.Contains(subDir.Name))
                continue;

            // Pass 1: Check for launcher/store signals at this folder level
            GameSourceKind signalType = StoreSignalDetector.DetectType(subDir);

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
            ScanContainerChildren(entries, subDir, rootPath, defaultType, depth: 0);
        }

        return entries;
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

        // 3 — Standalone (Unreal layout): Engine/ + */Binaries/{platform}/*.exe, or Binaries/{platform}/*.exe at root
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

    /// <summary>Checks for Steam emulator deep signals: root-level INI, child-level DLLs, and UE Steamworks path.</summary>
    private static bool HasSteamEmuDeepSignal(DirectoryInfo dir)
    {
        try
        {
            // Check root
            if (File.Exists(Path.Combine(dir.FullName, "steam_emu.ini")))
                return true;

            // Check immediate children
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (File.Exists(Path.Combine(child.FullName, "steam_emu.ini")))
                    return true;
            }

            // UE Steamworks path: Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/
            string steamworksPath = Path.Combine(dir.FullName, "Engine", "Binaries", "ThirdParty", "Steamworks");
            if (Directory.Exists(steamworksPath))
            {
                foreach (string steamworksVersionDir in Directory.GetDirectories(steamworksPath))
                {
                    string win64 = Path.Combine(steamworksVersionDir, "Win64");
                    if (Directory.Exists(win64) && File.Exists(Path.Combine(win64, "steam_emu.ini")))
                        return true;
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>Checks for legacy Ubisoft launcher signals: UbiStats.dll or Ubisoft.ini.</summary>
    private static bool HasUbisoftLegacySignal(DirectoryInfo dir)
    {
        try
        {
            // Check root
            if (File.Exists(Path.Combine(dir.FullName, "UbiStats.dll")))
                return true;

            // Check immediate children
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (File.Exists(Path.Combine(child.FullName, "UbiStats.dll")))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>UE platform directory names under Binaries/. Matches T66's ExecutableDiscovery.</summary>
    private static readonly string[] s_uePlatformNames = ["Win64", "Win32", "WinGDK", "Steam"];

    /// <summary>
    /// Checks for Unreal Engine directory layout:
    /// - UE4-5: Engine/ folder with child/Binaries/{platform}/*.exe
    /// - UE3: Binaries/{platform}/*.exe directly at root (no Engine/ needed)
    /// </summary>
    private bool HasUnrealLayoutSignal(DirectoryInfo dir)
    {
        // Fast path: UE3 — Binaries/ at root
        if (HasBinariesAtRoot(dir))
            return true;

        // UE4-5: need Engine/ directory
        string enginePath = Path.Combine(dir.FullName, "Engine");
        if (!Directory.Exists(enginePath))
            return false;

        // Check for any child with Binaries/{platform}/*.exe
        try
        {
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (child.Name == "Engine") continue;
                foreach (string platform in s_uePlatformNames)
                {
                    string platPath = Path.Combine(child.FullName, "Binaries", platform);
                    if (!Directory.Exists(platPath)) continue;
                    foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
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

    /// <summary>
    /// UE3 fast path: Binaries/ directly at root with platform subdirs.
    /// Games like Unreal Tournament 3, Gothic 3 use this layout.
    /// </summary>
    private static bool HasBinariesAtRoot(DirectoryInfo dir)
    {
        string binariesPath = Path.Combine(dir.FullName, "Binaries");
        if (!Directory.Exists(binariesPath))
            return false;

        try
        {
            foreach (string platform in s_uePlatformNames)
            {
                string platPath = Path.Combine(binariesPath, platform);
                if (!Directory.Exists(platPath)) continue;
                foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                    // Use static noise check (no instance needed for root-level signal)
                    if (!FileSystemHelper.IsNoiseExeName(name, FolderScanner.DefaultNoiseExePatterns))
                        return true;
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>Checks if the game folder contains non-noise executables at the root level.</summary>
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

    /// <summary>Checks for .lnk shortcut files at the root level that point to executables.</summary>
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
    //  children have game signals (store markers, exes, UE layout).
    //  Organization folders (≥2 game children) recurse into all children.
    // ════════════════════════════════════════════════════════════════

    /// <summary>Non-game folder names to skip during container recursion.</summary>
    private static readonly HashSet<string> s_nonGameFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Soundtrack", "Soundtracks", "Original Soundtrack",
        "Manuals", "Manual", "Item Data", "Misc", "Bonus Content",
        "Artwork", "Wallpapers", "Music",
        "Redist", "Support", "Tools", "_CommonRedist", "CommonRedist",
        "vcredist", "dotnet", "directx", "physx", "installer",
        "_installer", "install", "easyanticheat", "devtools", "docs",
        "licenses", "steam controller configs", "steamworks shared",
        "dlc", "program files", "windowsapps", "squirreltemp",
        "portable", "uninstall",
    };

    /// <summary>
    /// Recursively scans child directories of a container (store/publisher folder) for game entries.
    /// Bounded to maxDepth 2 (container → child → grandchild).
    /// </summary>
    private void ScanContainerChildren(
        List<GameEntry> entries, DirectoryInfo containerDir,
        string rootPath, GameSourceKind defaultType, int depth = 0)
    {
        if (depth > 1) return; // Bounded: max 2 levels

        var children = FileSystemHelper.GetDirectoriesSafe(containerDir.FullName);

        // First pass: count children with game signals (for organization detection)
        int gameSignalCount = 0;
        foreach (DirectoryInfo child in children)
        {
            if (IsNonGameFolder(child)) continue;
            if (StoreSignalDetector.DetectType(child) != GameSourceKind.Unknown
                || HasRootExecutableSignal(child)
                || HasUnrealLayoutSignal(child))
            {
                gameSignalCount++;
            }
        }

        // Second pass: process children
        foreach (DirectoryInfo child in children)
        {
            if (_hiddenFolderNames.Contains(child.Name))
                continue;
            if (IsNonGameFolder(child))
                continue;

            GameSourceKind childType = StoreSignalDetector.DetectType(child);

            // Tier 1 — Store signals (GOG, EA, Ubisoft, etc.) — always promote
            if (childType != GameSourceKind.Unknown)
            {
                AddGameEntry(entries, child, rootPath, childType, defaultType);
                continue;
            }

            // Organization folder: ≥2 game children → recurse into all, promote standalone
            if (gameSignalCount >= 2)
            {
                if (HasRootExecutableSignal(child) || HasUnrealLayoutSignal(child))
                {
                    AddGameEntry(entries, child, rootPath, GameSourceKind.Standalone, defaultType);
                    continue;
                }
                ScanContainerChildren(entries, child, rootPath, defaultType, depth + 1);
                continue;
            }

            // Single game child or publisher pattern: only recurse (don't promote standalone)
            if (gameSignalCount == 1 && (HasRootExecutableSignal(child) || HasUnrealLayoutSignal(child)))
            {
                // Single game child — this IS the game, but parent isn't a container
                // Don't add it here; let it be found by the caller's own scanning logic
                continue;
            }

            // Publisher folder pattern: only subdirs, no files → recurse
            if (gameSignalCount == 0)
            {
                FileInfo[] files = child.GetFiles("*", SearchOption.TopDirectoryOnly);
                if (files.Length == 0 && child.GetDirectories().Length > 0)
                {
                    ScanContainerChildren(entries, child, rootPath, defaultType, depth + 1);
                    continue;
                }
            }
        }
    }

    /// <summary>Checks if a folder is clearly not a game (non-game name, data-only, etc.).</summary>
    private static bool IsNonGameFolder(DirectoryInfo dir)
    {
        return s_nonGameFolderNames.Contains(dir.Name)
            || FileSystemHelper.NoiseSubDirNames.Contains(dir.Name);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    /// <summary>Checks if a directory name matches known noise patterns (saves, mods, workshops, etc.).</summary>
    private bool IsNoiseDirectory(string dirName)
    {
        return FileSystemHelper.IsNoiseDirectory(dirName, _noiseDirectoryPatterns);
    }

    /// <summary>
    /// Checks if an executable name (without extension) matches any noise pattern.
    /// Used by fallback detection (HasRootExecutableSignal, HasUnrealLayoutSignal) to
    /// filter non-game executables before deciding if a folder is a game.
    /// </summary>
    private bool IsNoiseExeName(string name)
    {
        return FileSystemHelper.IsNoiseExeName(name, _noiseExePatterns);
    }

    /// <summary>
    /// Returns the severity tier for a noise pattern.
    /// Lower tier = higher severity (Tier 1 = universal noise like uninstallers).
    /// Returns 999 if pattern not found in the tiered list.
    /// </summary>
    internal int GetExePatternTier(string pattern)
    {
        foreach (var entry in _tieredNoiseExePatterns)
        {
            if (pattern.Contains(entry.Pattern, StringComparison.OrdinalIgnoreCase))
                return entry.Tier;
        }
        return 999;
    }

    /// <summary>Creates a GameEntry from a scanned folder and adds it to the results list.</summary>
    private void AddGameEntry(
        List<GameEntry> entries, DirectoryInfo subDir,
        string rootPath, GameSourceKind resolvedType, GameSourceKind rootDefault)
    {
        bool isOverride = resolvedType != rootDefault;
        string[] exeFiles = FileSystemHelper.GetFilesSafe(subDir, "*.exe");
        string? exePath = ExecutableDiscovery.FindPrimaryExecutable(
            subDir, exeFiles, _noiseExePatterns, _noiseDirectoryPatterns, _launcherPatterns, GetExePatternTier);

        // LNK fallback — if no exe found, try resolving from .lnk shortcuts
        if (string.IsNullOrEmpty(exePath))
        {
            exePath = LnkParser.ResolveExeFromLnk(subDir);
        }

        string? launcherPath = ExecutableDiscovery.FindLauncherExecutable(subDir, exePath, _launcherPatterns);
        string manifestPath = ExecutableDiscovery.FindEpicManifest(subDir);
        string displayName = FileSystemHelper.NormalizeDisplayName(subDir.Name);
        string id = GameEntryId.ComputeId(rootPath, subDir.Name);
        var platformMetadata = new Dictionary<string, string>();
        string commandLineArgs = string.Empty;

        // GOG enrichment: parse goggame-*.info for title, exe, args, and game ID
        if (resolvedType == GameSourceKind.Gog
            && GogInfoParser.TryParse(subDir, _noiseDirectoryPatterns, out var gogInfo)
            && gogInfo is not null)
        {
            // Title: GOG .info is the official source
            if (!string.IsNullOrEmpty(gogInfo.Title))
            {
                platformMetadata["AutoDetectedTitle"] = displayName;
                displayName = gogInfo.Title;
                platformMetadata["TitleSource"] = "GogInfo";
            }

            // Exe: GOG .info is a fallback when ExecutableDiscovery finds nothing
            if (string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(gogInfo.ExePath))
            {
                exePath = gogInfo.ExePath;
            }

            // Launch args
            if (!string.IsNullOrEmpty(gogInfo.LaunchArgs))
            {
                commandLineArgs = gogInfo.LaunchArgs;
            }

            // Platform metadata
            platformMetadata["GogGameId"] = gogInfo.GameId;
        }

        entries.Add(new GameEntry(
            Id: id,
            FolderName: subDir.Name,
            DisplayName: displayName,
            GameSource: resolvedType,
            IsSourceOverridden: isOverride,
            ExecutablePath: exePath ?? string.Empty,
            LauncherPath: launcherPath ?? string.Empty,
            CommandLineArguments: commandLineArgs,
            ManifestPath: manifestPath,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: FileSystemHelper.GetLastWriteTimeSafe(subDir),
            PlatformMetadata: platformMetadata));
    }

}
