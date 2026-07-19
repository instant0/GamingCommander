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
            ScanContainerChildren(entries, subDir, rootPath, defaultType);
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

    /// <summary>Checks for Unreal Engine directory layout: Engine/ folder with Binaries/Win64/ containing exes.</summary>
    private bool HasUnrealLayoutSignal(DirectoryInfo dir)
    {
        // Need Engine/ directory
        string enginePath = Path.Combine(dir.FullName, "Engine");
        if (!Directory.Exists(enginePath))
            return false;

        // Check for any child with Binaries/Win64/*.exe
        try
        {
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
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
    //  immediate child has Pass 1 (launcher/store) signals.
    //  Children with only standalone signals do NOT qualify.
    // ════════════════════════════════════════════════════════════════

    /// <summary>Recursively scans child directories of a container (store/publisher folder) for game entries.</summary>
    private void ScanContainerChildren(
        List<GameEntry> entries, DirectoryInfo containerDir,
        string rootPath, GameSourceKind defaultType)
    {
        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(containerDir.FullName))
        {
            if (_hiddenFolderNames.Contains(child.Name))
                continue;

            GameSourceKind childType = StoreSignalDetector.DetectType(child);

            // Only promote children with Tier 1 (launcher) signals
            if (childType != GameSourceKind.Unknown)
            {
                AddGameEntry(entries, child, rootPath, childType, defaultType);
            }
        }
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
        string? launcherPath = ExecutableDiscovery.FindLauncherExecutable(subDir, exePath, _launcherPatterns);
        string manifestPath = ExecutableDiscovery.FindEpicManifest(subDir);
        string displayName = FileSystemHelper.NormalizeDisplayName(subDir.Name);
        string id = GameEntryId.ComputeId(rootPath, subDir.Name);

        entries.Add(new GameEntry(
            Id: id,
            FolderName: subDir.Name,
            DisplayName: displayName,
            GameSource: resolvedType,
            IsSourceOverridden: isOverride,
            ExecutablePath: exePath ?? string.Empty,
            LauncherPath: launcherPath ?? string.Empty,
            CommandLineArguments: string.Empty,
            ManifestPath: manifestPath,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: FileSystemHelper.GetLastWriteTimeSafe(subDir),
            PlatformMetadata: []));
    }

}
