using System.Globalization;
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
    private readonly IReadOnlyList<string> _configuredSteamPaths;
    private static readonly IReadOnlyList<string> RequiredAcfFields =
    ["appid", "name", "installdir", "StateFlags", "LastUpdated", "SizeOnDisk", "buildid"];

    public SteamLibraryScanner(IEnumerable<string> configuredSteamPaths)
    {
        _configuredSteamPaths = configuredSteamPaths.Select(NormalizePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Scan a single Steam library root path. Detects games in steamapps/common/
    /// and cross-references ACF files from ALL known Steam libraries.
    /// </summary>
    public IReadOnlyList<GameEntry> Scan(string libraryRootPath)
    {
        string root = NormalizePath(libraryRootPath);
        if (!Directory.Exists(root))
            return [];

        // Step 1: Discover ALL Steam library paths from libraryfolders.vdf + configured paths
        var allSteamPaths = DiscoverAllSteamPaths(root);

        // Step 2: Collect all ACF metadata from all discovered paths
        var acfMap = CollectAcfMap(allSteamPaths);

        // Step 3: Scan common/ folder in the requested root only
        string commonDir = Path.Combine(root, "steamapps", "common");
        if (!Directory.Exists(commonDir))
            return [];

        var entries = new List<GameEntry>();

        foreach (DirectoryInfo gameDir in GetDirectoriesSafe(commonDir))
        {
            string folderName = gameDir.Name;

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

        return entries;
    }

    /// <summary>
    /// Scan ALL configured Steam libraries and return a flat list.
    /// Prefer this for full refresh — it catches cross-library moves.
    /// </summary>
    public IReadOnlyList<GameEntry> ScanAll()
    {
        // Collect ACFs from ALL configured paths first
        var allSteamPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in _configuredSteamPaths)
        {
            foreach (string discoveredPath in DiscoverLibraryPaths(path))
                allSteamPaths.Add(discoveredPath);
        }

        var acfMap = CollectAcfMap(allSteamPaths);
        var entries = new List<GameEntry>();

        foreach (string libraryPath in _configuredSteamPaths)
        {
            string commonDir = Path.Combine(libraryPath, "steamapps", "common");
            if (!Directory.Exists(commonDir)) continue;

            foreach (DirectoryInfo gameDir in GetDirectoriesSafe(commonDir))
            {
                string folderName = gameDir.Name;

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

        return entries;
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam Path Discovery
    // ════════════════════════════════════════════════════════════════

    private HashSet<string> DiscoverAllSteamPaths(string primaryRoot)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Always include the primary root
        paths.Add(primaryRoot);

        // Include all configured Steam paths
        foreach (string p in _configuredSteamPaths)
            paths.Add(p);

        // Discover paths from libraryfolders.vdf
        foreach (string discoveredPath in DiscoverLibraryPaths(primaryRoot))
            paths.Add(discoveredPath);

        return paths;
    }

    /// <summary>
    /// Parse libraryfolders.vdf to discover all Steam library paths.
    /// Format: numbered keys like "1" "D:\\SteamLibrary", "2" "E:\\SteamLibrary"
    /// </summary>
    private List<string> DiscoverLibraryPaths(string libraryRoot)
    {
        var paths = new List<string>();
        string vdfPath = Path.Combine(libraryRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath)) return paths;

        try
        {
            string text = File.ReadAllText(vdfPath);
            var parsed = VdfParser.Parse(text);

            // Navigate into the root block (usually "LibraryFolders")
            var block = parsed;
            if (block.Count == 1 && block.Values.First() is Dictionary<string, object> inner)
                block = inner;

            foreach (var kvp in block)
            {
                // Numeric keys like "1", "2", "3" hold path values
                if (int.TryParse(kvp.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    if (kvp.Value is string pathStr && !string.IsNullOrWhiteSpace(pathStr))
                    {
                        paths.Add(NormalizePath(pathStr));
                    }
                }
            }
        }
        catch
        {
            // Silently return empty on parse failure
        }

        return paths;
    }

    // ════════════════════════════════════════════════════════════════
    //  ACF Collection
    // ════════════════════════════════════════════════════════════════

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
                    var info = ParseAcfFile(acfFile, path);
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

    private static AcfInfo? ParseAcfFile(string acfPath, string libraryPath)
    {
        try
        {
            string text = File.ReadAllText(acfPath);
            var fields = VdfParser.ExtractFields(text, RequiredAcfFields.ToArray());
            if (fields == null) return null;

            string installdir = fields.GetValueOrDefault("installdir", string.Empty);
            if (string.IsNullOrWhiteSpace(installdir)) return null;

            return new AcfInfo(
                LibraryPath: libraryPath,
                AcfFilePath: acfPath,
                AppId: fields.GetValueOrDefault("appid", string.Empty),
                Name: fields.GetValueOrDefault("name", string.Empty),
                Installdir: installdir,
                StateFlags: fields.GetValueOrDefault("StateFlags", string.Empty),
                LastUpdated: fields.GetValueOrDefault("LastUpdated", string.Empty),
                SizeOnDisk: fields.GetValueOrDefault("SizeOnDisk", string.Empty),
                BuildId: fields.GetValueOrDefault("buildid", string.Empty));
        }
        catch
        {
            return null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  GameEntry Creation
    // ════════════════════════════════════════════════════════════════

    private static GameEntry CreateEntry(
        string libraryRoot, DirectoryInfo gameDir, string folderName,
        AcfInfo acf, string status)
    {
        string displayName = !string.IsNullOrWhiteSpace(acf.Name) ? acf.Name : NormalizeDisplayName(folderName);
        string id = GameEntryId.Compute(libraryRoot, folderName);

        return new GameEntry(
            Id: id,
            FolderName: folderName,
            DisplayName: displayName,
            GameSource: GameSourceKind.Steam,
            Override: false,
            ExecutablePath: FindPrimaryExe(gameDir),
            LauncherPath: string.Empty,
            CmdlineArgs: $"steam://rungameid/{acf.AppId}",
            ManifestPath: acf.AcfFilePath,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: GetLastWriteTimeSafe(gameDir),
            Extra: new Dictionary<string, string>
            {
                ["SteamStatus"] = status,
                ["SteamAppId"] = acf.AppId,
                ["AcfLibraryPath"] = acf.LibraryPath,
                ["AcfSizeOnDisk"] = acf.SizeOnDisk,
                ["AcfBuildId"] = acf.BuildId,
                ["AcfStateFlags"] = acf.StateFlags,
            });
    }

    private static GameEntry CreateOrphanedEntry(
        string libraryRoot, DirectoryInfo gameDir, string folderName)
    {
        string id = GameEntryId.Compute(libraryRoot, folderName);

        return new GameEntry(
            Id: id,
            FolderName: folderName,
            DisplayName: NormalizeDisplayName(folderName),
            GameSource: GameSourceKind.Steam,
            Override: false,
            ExecutablePath: FindPrimaryExe(gameDir),
            LauncherPath: string.Empty,
            CmdlineArgs: string.Empty,
            ManifestPath: string.Empty,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: GetLastWriteTimeSafe(gameDir),
            Extra: new Dictionary<string, string>
            {
                ["SteamStatus"] = "Orphaned",
                ["SteamAppId"] = string.Empty,
            });
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

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

    private static bool IsNoiseExe(string name)
    {
        // Minimal noise check for Steam library context
        return name is "unins000" or "unins001" or "setup" or "vcredist"
            or "dxsetup" or "oalinst" or "commonredist";
    }

    private static string NormalizePath(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string NormalizeDisplayName(string folderName)
    {
        return folderName
            .Replace("_", " ")
            .Replace("-", " ")
            .Trim();
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

    // ════════════════════════════════════════════════════════════════
    //  Internal Types
    // ════════════════════════════════════════════════════════════════

    private sealed record AcfInfo(
        string LibraryPath,
        string AcfFilePath,
        string AppId,
        string Name,
        string Installdir,
        string StateFlags,
        string LastUpdated,
        string SizeOnDisk,
        string BuildId);
}
