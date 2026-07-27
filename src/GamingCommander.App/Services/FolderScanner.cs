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
    private readonly IReadOnlyList<string> _peMetadataBlacklist;
    private readonly RegistryFallbackDetector? _registryFallback;

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
        // Editor/tool executables — these are development tools, not games
        "editor", "builder", "tool", "config", "settings",
    ];

    private static readonly IReadOnlyList<string> DefaultLauncherPatterns =
    [
        "launcher", "launch", "updater", "bootstrap", "redlaunch",
        "epicgameslauncher", "goggalaxy", "ea app", "ubisoft",
    ];

    /// <summary>Creates a scanner with default hardcoded noise patterns (for tests and simple cases).</summary>
    public FolderScanner()
        : this([], DefaultNoiseExePatterns, [], DefaultLauncherPatterns, [], [], null)
    {
    }

    /// <summary>Creates a scanner with custom hidden folder names and default noise patterns.</summary>
    public FolderScanner(IEnumerable<string> hiddenFolderNames)
        : this(hiddenFolderNames, DefaultNoiseExePatterns, [], DefaultLauncherPatterns, [], [], null)
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
            blacklist.TieredExePatterns,
            blacklist.PeMetadataPatterns,
            null)
    {
    }

    /// <summary>
    /// Creates a scanner with blacklist data and optional registry reader for fallback detection.
    /// When a registry reader is provided, Pass 1c classifies games via registry per-game keys
    /// when no filesystem signal is found (EA, Ubisoft, GOG, Rockstar).
    /// </summary>
    public FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        BlacklistData blacklist,
        IRegistryReader? registryReader)
        : this(
            hiddenFolderNames,
            blacklist.ExeNamePatterns.Count > 0 ? blacklist.ExeNamePatterns : DefaultNoiseExePatterns,
            blacklist.DirectoryPatterns,
            DefaultLauncherPatterns,
            blacklist.TieredExePatterns,
            blacklist.PeMetadataPatterns,
            registryReader)
    {
    }

    private FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlyList<string> noiseDirectoryPatterns,
        IReadOnlyList<string> launcherPatterns,
        IReadOnlyList<BlacklistTierEntry> tieredNoiseExePatterns,
        IReadOnlyList<string> peMetadataBlacklist,
        IRegistryReader? registryReader)
    {
        _hiddenFolderNames = new HashSet<string>(hiddenFolderNames, StringComparer.OrdinalIgnoreCase);
        _noiseExePatterns = noiseExePatterns;
        _noiseDirectoryPatterns = new HashSet<string>(noiseDirectoryPatterns, StringComparer.OrdinalIgnoreCase);
        _launcherPatterns = launcherPatterns;
        _tieredNoiseExePatterns = tieredNoiseExePatterns;
        _peMetadataBlacklist = peMetadataBlacklist;
        _registryFallback = registryReader is not null
            ? new RegistryFallbackDetector(registryReader)
            : null;
    }

    // ════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scans a library root for game folders using a 10-signal priority-ordered detection chain.
    /// Returns game entries with detected source types, executables, and metadata.
    /// </summary>
    public IReadOnlyList<GameEntry> Scan(string rootPath, GameSourceKind defaultType, CancellationToken ct = default)
    {
        if (!Directory.Exists(rootPath))
            return [];

        var entries = new List<GameEntry>();

        foreach (DirectoryInfo subDir in FileSystemHelper.GetDirectoriesSafe(rootPath))
        {
            ct.ThrowIfCancellationRequested();
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

            // NOTE: Pass 1b (parent propagation) REMOVED.
            // Detection is based on signal files inside the game folder, NOT on parent directory signals.
            // A game at Q:\random\Diablo III\ with .build.info is BattleNet — period.
            // StoreSignalDetector.DetectType() already handles this correctly.

            if (signalType != GameSourceKind.Unknown)
            {
                // Tier 1 — High confidence. Always create an entry.
                AddGameEntry(entries, subDir, rootPath, signalType, defaultType);
                continue;
            }

            // Pass 1c: Registry fallback (EA, Ubisoft, GOG, Rockstar per-game keys)
            if (_registryFallback is not null)
            {
                GameSourceKind registryType = _registryFallback.DetectType(subDir);
                if (registryType != GameSourceKind.Unknown)
                {
                    AddGameEntry(entries, subDir, rootPath, registryType, defaultType);
                    continue;
                }
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
            ContainerScanner.ScanContainerChildren(
                entries, subDir, rootPath, defaultType,
                (e, dir, rp, type) => AddGameEntry(e, dir, rp, type, defaultType),
                _hiddenFolderNames, _noiseExePatterns, ct: ct);
        }

        return entries;
    }

    // ════════════════════════════════════════════════════════════════
    //  Pass 2 — Deep Fallback Detection (Medium/Low Confidence)
    // ════════════════════════════════════════════════════════════════

    private GameSourceKind DetectFallbackType(DirectoryInfo subDir)
    {
        return FallbackSignalDetector.DetectFallbackType(subDir, _noiseExePatterns);
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
        var primaryExe = ExecutableDiscovery.FindPrimaryExecutable(
            subDir, exeFiles, _noiseExePatterns, _noiseDirectoryPatterns, _launcherPatterns, GetExePatternTier);
        string? exePath = primaryExe.ExePath;

        // LNK fallback — if no exe found, try resolving from .lnk shortcuts
        if (string.IsNullOrEmpty(exePath))
        {
            exePath = LnkParser.ResolveExeFromLnk(subDir);
        }

        string? launcherPath = ExecutableDiscovery.FindLauncherExecutable(subDir, exePath, _launcherPatterns);
        string displayName = FileSystemHelper.NormalizeDisplayName(subDir.Name);
        string id = GameEntryId.ComputeId(rootPath, subDir.Name);
        var platformMetadata = new Dictionary<string, string>();
        string commandLineArgs = string.Empty;

        // Track whether display name was enriched by a store parser (used for PE FileDescription guard)
        bool storeEnrichedDisplayName = false;

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
                storeEnrichedDisplayName = true;
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

        // EA enrichment: parse __Installer/InstallLog.txt for game name, display name, studio.
        // The Install Location field may reference an old/wrong path, but game name and studio are reliable.
        if (resolvedType == GameSourceKind.EaApp
            && EaInstallLogParser.TryParse(subDir, out var eaInfo)
            && eaInfo is not null)
        {
            // Display name: EA display name is authoritative (e.g., "Dragon Age™: Inquisition")
            if (!string.IsNullOrEmpty(eaInfo.DisplayName))
            {
                platformMetadata["AutoDetectedTitle"] = displayName;
                displayName = eaInfo.DisplayName;
                platformMetadata["TitleSource"] = "EaInstallLog";
                storeEnrichedDisplayName = true;
            }

            // Studio metadata
            if (!string.IsNullOrEmpty(eaInfo.Studio))
            {
                platformMetadata["Studio"] = eaInfo.Studio;
            }

            // Game name (non-trademarked)
            if (!string.IsNullOrEmpty(eaInfo.GameName))
            {
                platformMetadata["EaGameName"] = eaInfo.GameName;
            }
        }

        // Epic enrichment: extract metadata from .mancpn/.item files, cross-reference global manifests
        if (resolvedType == GameSourceKind.Epic)
        {
            // Strategy 1: Local identifier extraction from .egstore/ or .egsstore/
            var localIds = EpicManifestParser.ExtractLocalIdentifiers(subDir);

            // Strategy 2: Global .item cross-reference from ProgramData
            var globalItem = EpicManifestParser.CrossReferenceGlobalManifests(subDir);
            if (globalItem is not null && !string.IsNullOrEmpty(globalItem.DisplayName))
            {
                platformMetadata["AutoDetectedTitle"] = displayName;
                displayName = globalItem.DisplayName;
                platformMetadata["TitleSource"] = "EpicItemManifest";
                storeEnrichedDisplayName = true;

                // Override local namespace with correct public namespace from global .item
                localIds = new EpicManifestParser.EpicIdentifiers(
                    CatalogNamespace: globalItem.CatalogNamespace,
                    CatalogItemId: globalItem.CatalogItemId,
                    AppName: globalItem.AppName,
                    DisplayName: globalItem.DisplayName,
                    LaunchExecutable: globalItem.LaunchExecutable);
            }

            // Store GUID identifiers in platform metadata
            if (localIds is not null)
            {
                if (!string.IsNullOrEmpty(localIds.CatalogItemId))
                    platformMetadata["EpicCatalogItemId"] = localIds.CatalogItemId;
                if (!string.IsNullOrEmpty(localIds.CatalogNamespace))
                    platformMetadata["EpicCatalogNamespace"] = localIds.CatalogNamespace;
                if (!string.IsNullOrEmpty(localIds.AppName))
                    platformMetadata["EpicAppName"] = localIds.AppName;
            }

            // Resolve LaunchExecutable from .item if available (and no exe found yet)
            if (globalItem is not null
                && !string.IsNullOrEmpty(globalItem.LaunchExecutable)
                && !string.IsNullOrEmpty(globalItem.InstallLocation)
                && string.IsNullOrEmpty(exePath))
            {
                string resolvedExe = EpicManifestParser.ResolveLaunchExecutable(
                    globalItem.InstallLocation, globalItem.LaunchExecutable);
                if (!string.IsNullOrEmpty(resolvedExe))
                {
                    exePath = resolvedExe;
                }
            }
        }

        // BattleNet enrichment: extract product codename from .build.info
        if (resolvedType == GameSourceKind.BattleNet)
        {
            string? product = StoreSignalDetector.ExtractBlizzardProduct(subDir);
            if (!string.IsNullOrEmpty(product))
            {
                platformMetadata["BlizzardProduct"] = product;
            }
        }

        // PE FileDescription enrichment for non-store-enriched games (Plan 112 Step 2).
        // Uses FileDescription from the primary exe's PE metadata as the display name
        // when no store parser has provided one and the PE data passes guard conditions.
        if (!storeEnrichedDisplayName && !string.IsNullOrWhiteSpace(primaryExe.FileDescription))
        {
            string peDesc = primaryExe.FileDescription;

            // Guard: reject short strings (likely noise like single/double chars)
            bool lengthOk = peDesc.Length > 2;

            // Guard: reject generic placeholders from pe_metadata_blacklist
            bool notPlaceholder = true;
            if (_peMetadataBlacklist.Count > 0)
            {
                string peDescLower = peDesc.ToLowerInvariant();
                notPlaceholder = !_peMetadataBlacklist.Any(p => peDescLower.Contains(p));
            }

            if (lengthOk && notPlaceholder)
            {
                platformMetadata["AutoDetectedTitle"] = displayName;
                displayName = peDesc;
                platformMetadata["TitleSource"] = "PeFileDescription";
                storeEnrichedDisplayName = true;
            }
        }

        // Ubisoft Support/Readme enrichment (Plan 112 Step 3B).
        // Ubisoft games ship Support/Readme/ with publisher and game title on lines 1-2.
        // Applied only for Ubisoft Connect games and only when no store/PE enrichment was used.
        if (!storeEnrichedDisplayName && resolvedType == GameSourceKind.UbisoftConnect)
        {
            var ubiInfo = UbisoftReadmeParser.TryParse(subDir);
            if (ubiInfo?.GameTitle is not null)
            {
                platformMetadata["AutoDetectedTitle"] = displayName;
                displayName = ubiInfo.GameTitle;
                platformMetadata["TitleSource"] = "UbisoftReadme";
            }
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
            ManifestPath: string.Empty,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: FileSystemHelper.GetLastWriteTimeSafe(subDir),
            PlatformMetadata: platformMetadata,
            Tags: [],
            UserOverrides: []));
    }

}
