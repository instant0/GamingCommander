using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Dedicated scanner for Steam library paths.
///
/// Steam detection is structural: any folder under steamapps/common/ IS a Steam game.
/// ACF files (appmanifest_*.acf) in steamapps/ provide authoritative metadata.
/// Cross-referencing across multiple libraries detects moved/orphaned games.
/// </summary>
public sealed class SteamLibraryScanner
{
    /// <summary>
    /// Known Steam-internal directories under steamapps/common that are NOT games.
    /// Bug 10: "Steam Controller Configs" etc. were reported as Orphaned because
    /// SteamLibraryScanner does not consult FolderScanner's NoiseSubDirNames.
    /// This is the Steam-scanner's OWN skip list — it must never filter the literal
    /// name "steam" (installdirs are arbitrary; "Steam" could be a real game title).
    /// </summary>
    private static readonly HashSet<string> s_nonGameCommonFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam controller configs",
        "steamworks shared",
    };

    private readonly IReadOnlyList<string> _configuredSteamPaths;

    /// <summary>Creates a new scanner with the specified configured Steam library paths.</summary>
    public SteamLibraryScanner(IEnumerable<string> configuredSteamPaths)
    {
        _configuredSteamPaths = configuredSteamPaths.Select(SteamAcfParser.NormalizePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Scan a single Steam library root path. Detects games in steamapps/common/
    /// and cross-references ACF files from ALL known Steam libraries.
    /// Also detects "Missing" games — ACFs whose game files no longer exist in any library.
    /// </summary>
    public IReadOnlyList<GameEntry> Scan(string libraryRootPath)
    {
        string root = SteamAcfParser.NormalizePath(libraryRootPath);
        if (!Directory.Exists(root))
            return [];

        // Step 1: Discover ALL Steam library paths from libraryfolders.vdf + configured paths
        var allSteamPaths = DiscoverAllSteamPaths(root);

        // Step 2: Collect all ACF metadata from all discovered paths
        var acfMap = CollectAcfMap(allSteamPaths);

        // Step 3: Scan common/ folder in the requested root only
        string commonDir = Path.Combine(root, "steamapps", "common");
        var entries = new List<GameEntry>();

        if (Directory.Exists(commonDir))
        {
            foreach (DirectoryInfo gameDir in FileSystemHelper.GetDirectoriesSafe(commonDir))
            {
                string folderName = gameDir.Name;

                // Bug 10: skip Steam-internal folders (Controller Configs, etc.) before
                // status resolution so they never surface as Orphaned "games".
                if (IsNonGameCommonFolder(folderName))
                    continue;

                if (acfMap.TryGetValue(folderName, out var acfInfo))
                {
                    // ACF found — determine status
                    string acfLibrary = acfInfo.LibraryPath;
                    string status = acfLibrary.Equals(root, StringComparison.OrdinalIgnoreCase)
                        ? "Installed"
                        : "Moved";

                    entries.Add(CreateEntry(root, gameDir, folderName, acfInfo, status));
                }
                else
                {
                    // No ACF found anywhere — orphaned
                    entries.Add(CreateOrphanedEntry(root, gameDir, folderName));
                }
            }
        }

        // Step 4: Detect Missing games — ACFs whose installdir has no matching common/ folder
        foreach (var (installdir, acfInfo) in acfMap)
        {
            bool found = false;
            foreach (string steamPath in allSteamPaths)
            {
                string candidate = Path.Combine(steamPath, "steamapps", "common", installdir);
                if (Directory.Exists(candidate))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                entries.Add(CreateMissingAcfEntry(root, acfInfo));
            }
        }

        return entries;
    }

    /// <summary>
    /// Scan ALL configured Steam libraries and return a flat list.
    /// Prefer this for full refresh — it catches cross-library moves and missing games.
    /// </summary>
    public IReadOnlyList<GameEntry> ScanAll()
    {
        // Collect ACFs from ALL configured paths first
        var allSteamPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in _configuredSteamPaths)
        {
            foreach (string discoveredPath in SteamAcfParser.DiscoverLibraryPaths(path))
                allSteamPaths.Add(discoveredPath);
        }

        var acfMap = CollectAcfMap(allSteamPaths);
        var entries = new List<GameEntry>();

        foreach (string libraryPath in _configuredSteamPaths)
        {
            string commonDir = Path.Combine(libraryPath, "steamapps", "common");
            if (!Directory.Exists(commonDir)) continue;

            foreach (DirectoryInfo gameDir in FileSystemHelper.GetDirectoriesSafe(commonDir))
            {
                string folderName = gameDir.Name;

                // Bug 10: skip Steam-internal folders (Controller Configs, etc.).
                if (IsNonGameCommonFolder(folderName))
                    continue;

                if (acfMap.TryGetValue(folderName, out var acfInfo))
                {
                    string status = acfInfo.LibraryPath.Equals(libraryPath, StringComparison.OrdinalIgnoreCase)
                        ? "Installed"
                        : "Moved";
                    entries.Add(CreateEntry(libraryPath, gameDir, folderName, acfInfo, status));
                }
                else
                {
                    entries.Add(CreateOrphanedEntry(libraryPath, gameDir, folderName));
                }
            }
        }

        // Detect Missing games — ACFs whose installdir has no matching common/ folder
        foreach (var (installdir, acfInfo) in acfMap)
        {
            bool found = false;
            foreach (string steamPath in allSteamPaths)
            {
                string candidate = Path.Combine(steamPath, "steamapps", "common", installdir);
                if (Directory.Exists(candidate))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                entries.Add(CreateMissingAcfEntry(
                    acfInfo.LibraryPath, acfInfo));
            }
        }

        return entries;
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam Path Discovery
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Discovers all Steam library paths from libraryfolders.vdf and configured paths.
    /// </summary>
    private HashSet<string> DiscoverAllSteamPaths(string primaryRoot)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Always include the primary root
        paths.Add(primaryRoot);

        // Include all configured Steam paths
        foreach (string steamPath in _configuredSteamPaths)
            paths.Add(steamPath);

        // Discover paths from libraryfolders.vdf
        foreach (string discoveredPath in SteamAcfParser.DiscoverLibraryPaths(primaryRoot))
            paths.Add(discoveredPath);

        return paths;
    }

    // ════════════════════════════════════════════════════════════════
    //  ACF Collection
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds a map of AppId to ACF metadata from all known Steam library paths.
    /// </summary>
    private Dictionary<string, AcfInfo> CollectAcfMap(IEnumerable<string> steamPaths)
    {
        var map = new Dictionary<string, AcfInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in steamPaths)
        {
            string steamappsDir = Path.Combine(path, "steamapps");
            if (!Directory.Exists(steamappsDir)) continue;

            try
            {
                foreach (string acfFile in Directory.EnumerateFiles(steamappsDir, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
                {
                    var info = SteamAcfParser.ParseAcfFile(acfFile, path);
                    if (info != null && !string.IsNullOrWhiteSpace(info.Installdir))
                    {
                        // First match wins (avoids duplicate keys)
                        if (!map.ContainsKey(info.Installdir))
                            map[info.Installdir] = info;
                    }
                }
            }
            catch { }
        }

        return map;
    }

    // ════════════════════════════════════════════════════════════════
    //  GameEntry Creation
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a GameEntry for an installed Steam game from its ACF metadata.
    /// </summary>
    private static GameEntry CreateEntry(
        string libraryRoot, DirectoryInfo gameDir, string folderName,
        AcfInfo acf, string status)
    {
        string displayName = !string.IsNullOrWhiteSpace(acf.Name) ? acf.Name : FileSystemHelper.NormalizeDisplayName(folderName);
        string id = GameEntryId.ComputeId(libraryRoot, folderName);

        var extra = new Dictionary<string, string>
        {
            ["SteamStatus"] = status,
            ["SteamAppId"] = acf.AppId,
            ["AcfLibraryPath"] = acf.LibraryPath,
            ["AcfSizeOnDisk"] = acf.SizeOnDisk,
            ["AcfBuildId"] = acf.BuildId,
            ["AcfStateFlags"] = acf.StateFlags,
            ["FolderName"] = folderName,
        };

        // For Moved games, store the expected path so the UI can show cross-library context
        if (status == "Moved")
        {
            extra["AcfExpectedPath"] = Path.Combine(acf.LibraryPath, "steamapps", "common", folderName);
            extra["ActualLibraryRoot"] = libraryRoot;
            extra["AcfFilePath"] = acf.AcfFilePath;
        }

        return new GameEntry(
            Id: id,
            FolderName: folderName,
            DisplayName: displayName,
            GameSource: GameSourceKind.Steam,
            IsSourceOverridden: false,
            ExecutablePath: FindPrimaryExe(gameDir),
            LauncherPath: string.Empty,
            CommandLineArguments: $"steam://rungameid/{acf.AppId}",
            ManifestPath: acf.AcfFilePath,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: FileSystemHelper.GetLastWriteTimeSafe(gameDir),
            PlatformMetadata: extra,
            Tags: [],
            UserOverrides: []);
    }

    /// <summary>
    /// Creates a GameEntry for a Steam game whose ACF exists but game files are missing.
    /// </summary>
    private static GameEntry CreateOrphanedEntry(
        string libraryRoot, DirectoryInfo gameDir, string folderName)
    {
        string id = GameEntryId.ComputeId(libraryRoot, folderName);

        return new GameEntry(
            Id: id,
            FolderName: folderName,
            DisplayName: FileSystemHelper.NormalizeDisplayName(folderName),
            GameSource: GameSourceKind.Steam,
            IsSourceOverridden: false,
            ExecutablePath: FindPrimaryExe(gameDir),
            LauncherPath: string.Empty,
            CommandLineArguments: string.Empty,
            ManifestPath: string.Empty,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: FileSystemHelper.GetLastWriteTimeSafe(gameDir),
            PlatformMetadata: new Dictionary<string, string>
            {
                ["SteamStatus"] = "Orphaned",
                ["SteamAppId"] = string.Empty,
                ["FolderName"] = folderName,
                ["LibraryRoot"] = libraryRoot,
            },
            Tags: [],
            UserOverrides: []);
    }

    /// <summary>
    /// Create a GameEntry for an ACF whose game files no longer exist in any known library.
    /// The ACF still registers the game, but the common/ folder is gone.
    /// </summary>
    private static GameEntry CreateMissingAcfEntry(string libraryRoot, AcfInfo acf)
    {
        string id = GameEntryId.ComputeId(libraryRoot, acf.Installdir);

        return new GameEntry(
            Id: id,
            FolderName: acf.Installdir,
            DisplayName: !string.IsNullOrWhiteSpace(acf.Name) ? acf.Name : FileSystemHelper.NormalizeDisplayName(acf.Installdir),
            GameSource: GameSourceKind.Steam,
            IsSourceOverridden: false,
            ExecutablePath: string.Empty,
            LauncherPath: string.Empty,
            CommandLineArguments: $"steam://rungameid/{acf.AppId}",
            ManifestPath: acf.AcfFilePath,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.MinValue,
            PlatformMetadata: new Dictionary<string, string>
            {
                ["SteamStatus"] = "Missing",
                ["SteamAppId"] = acf.AppId,
                ["AcfLibraryPath"] = acf.LibraryPath,
                ["AcfFilePath"] = acf.AcfFilePath,
                ["AcfSizeOnDisk"] = acf.SizeOnDisk,
                ["AcfBuildId"] = acf.BuildId,
                ["AcfStateFlags"] = acf.StateFlags,
                ["FolderName"] = acf.Installdir,
                ["AcfExpectedPath"] = Path.Combine(
                    acf.LibraryPath, "steamapps", "common", acf.Installdir),
            },
            Tags: [],
            UserOverrides: []);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds the primary executable in a Steam game's common/ directory.
    /// </summary>
    private static string FindPrimaryExe(DirectoryInfo dir)
    {
        try
        {
            foreach (string exe in Directory.EnumerateFiles(dir.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                if (!IsNoiseExe(name))
                    return exe;
            }
        }
        catch { }
        return string.Empty;
    }

    /// <summary>
    /// Checks if an executable is a known Steam noise file (installer, uninstaller, etc.).
    /// Subset of the full noise list — Steam-specific to avoid false positives on
    /// common redistributable names that are valid games in other contexts.
    /// </summary>
    private static bool IsNoiseExe(string name)
    {
        // Minimal noise check for Steam library context
        return name is "unins000" or "unins001" or "setup" or "vcredist"
            or "dxsetup" or "oalinst" or "commonredist";
    }

    /// <summary>
    /// Returns true if the folder name is a known Steam-internal non-game directory
    /// under steamapps/common. Used to suppress Orphaned entries (Bug 10).
    /// Deliberately does NOT include "steam" — installdirs are arbitrary and a game
    /// may legitimately be titled Steam.
    /// </summary>
    private static bool IsNonGameCommonFolder(string folderName)
    {
        return s_nonGameCommonFolderNames.Contains(folderName);
    }
}
