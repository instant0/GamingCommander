using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Detects game store type via Windows registry fallback when no filesystem signal is found.
/// Enumerates per-game registry keys for EA, Ubisoft, GOG, and Rockstar to build a map
/// of known install paths, then classifies game directories by path matching.
///
/// Two-tier matching strategy:
///   Tier 1 (exact): game directory path matches a registry install path exactly
///   Tier 2 (name):  game directory name matches a known registry game name
///                    (catches moved games where registry path is stale)
///
/// Registry key sources:
///   EA:      HKLM\...\EA Games\{gameName}\Install Dir
///   Ubisoft: HKLM\...\Ubisoft\Launcher\Installs\{gameId}\InstallDir
///   GOG:     HKLM\...\GOG.com\Games\{gameId}\path
///   Rockstar: HKLM\...\Rockstar Games\{gameName}\InstallFolder
/// </summary>
internal sealed class RegistryFallbackDetector
{
    private readonly Dictionary<string, string> _eaGamePaths;
    private readonly Dictionary<string, string> _ubiGamePaths;
    private readonly Dictionary<string, string> _gogGamePaths;
    private readonly Dictionary<string, string> _rockstarGamePaths;

    /// <summary>
    /// Combined name→source mapping for fuzzy matching.
    /// Built once from all path dictionaries during construction.
    /// Key = game directory name (e.g., "Dead Space 3"), Value = GameSourceKind.
    /// </summary>
    private readonly Dictionary<string, GameSourceKind> _nameToSource;

    public RegistryFallbackDetector(IRegistryReader registry)
    {
        _eaGamePaths = EnumerateEaGamePaths(registry);
        _ubiGamePaths = EnumerateUbisoftGamePaths(registry);
        _gogGamePaths = EnumerateGogGamePaths(registry);
        _rockstarGamePaths = EnumerateRockstarGamePaths(registry);
        _nameToSource = BuildNameToSourceMap();
    }

    /// <summary>
    /// Checks if a game directory matches any known registry install path.
    /// Tier 1: exact path match (highest confidence).
    /// Tier 2: directory name matches a known game name (medium confidence — catches moved games).
    /// Returns the detected GameSourceKind or Unknown if no match.
    /// </summary>
    public GameSourceKind DetectType(DirectoryInfo gameDir)
    {
        string gamePath = NormalizePath(gameDir.FullName);

        // ── Tier 1: Exact path match (registry path == actual path) ──

        if (MatchesAnyPath(gamePath, _eaGamePaths))
            return GameSourceKind.EaApp;

        if (MatchesAnyPath(gamePath, _ubiGamePaths))
            return GameSourceKind.UbisoftConnect;

        if (MatchesAnyPath(gamePath, _gogGamePaths))
            return GameSourceKind.Gog;

        if (MatchesAnyPath(gamePath, _rockstarGamePaths))
            return GameSourceKind.Rockstar;

        // ── Tier 2: Name match (directory name matches registry game name) ──
        // Catches the case where a game was moved (e.g., Q:\Games\Dead Space 3 → E:\Games\Dead Space 3)
        // but the registry still points to the old path. The directory name is the same.

        if (_nameToSource.TryGetValue(gameDir.Name, out GameSourceKind nameMatch))
            return nameMatch;

        return GameSourceKind.Unknown;
    }

    // ── Name-to-source map ────────────────────────────────────────

    /// <summary>
    /// Builds a combined dictionary mapping game directory names to their source kind.
    /// For EA/Rockstar: dictionary key IS the game name.
    /// For Ubisoft/GOG: dictionary key is a numeric ID — extract name from the last path segment.
    /// </summary>
    private Dictionary<string, GameSourceKind> BuildNameToSourceMap()
    {
        var map = new Dictionary<string, GameSourceKind>(StringComparer.OrdinalIgnoreCase);

        // EA: key is game name (e.g., "Dead Space 3")
        foreach (string gameName in _eaGamePaths.Keys)
            map.TryAdd(gameName, GameSourceKind.EaApp);

        // Ubisoft: key is gameId — extract game name from path's last segment
        foreach (string path in _ubiGamePaths.Values)
        {
            string gameName = ExtractGameNameFromPath(path);
            if (!string.IsNullOrEmpty(gameName))
                map.TryAdd(gameName, GameSourceKind.UbisoftConnect);
        }

        // GOG: key is gameId — extract game name from path's last segment
        foreach (string path in _gogGamePaths.Values)
        {
            string gameName = ExtractGameNameFromPath(path);
            if (!string.IsNullOrEmpty(gameName))
                map.TryAdd(gameName, GameSourceKind.Gog);
        }

        // Rockstar: key is game name (e.g., "Grand Theft Auto V Enhanced")
        foreach (string gameName in _rockstarGamePaths.Keys)
            map.TryAdd(gameName, GameSourceKind.Rockstar);

        return map;
    }

    /// <summary>
    /// Extracts the game name (last directory segment) from a registry path.
    /// Handles both Windows (\) and Linux (/) path separators.
    /// </summary>
    private static string ExtractGameNameFromPath(string path)
    {
        // Normalize to platform separators first so Path.GetFileName works
        string normalized = path.Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFileName(normalized);
    }

    // ── Per-game path enumeration ──────────────────────────────────

    /// <summary>
    /// EA: enumerate HKLM\SOFTWARE\WOW6432Node\EA Games\{gameName}
    /// and read "Install Dir" from each subkey.
    /// Also checks HKLM\SOFTWARE\Electronic Arts\EA Games\{gameName}.
    /// </summary>
    private static Dictionary<string, string> EnumerateEaGamePaths(IRegistryReader registry)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        EnumerateEaSubKeys(registry, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games", paths);
        EnumerateEaSubKeys(registry, @"HKEY_LOCAL_MACHINE\SOFTWARE\Electronic Arts\EA Games", paths);

        return paths;
    }

    private static void EnumerateEaSubKeys(
        IRegistryReader registry, string basePath,
        Dictionary<string, string> paths)
    {
        foreach (string subKeyName in registry.EnumerateSubKeyNames(basePath))
        {
            string subKeyPath = basePath + @"\" + subKeyName;
            string? installDir = registry.ReadStringValue(subKeyPath, "Install Dir");
            if (!string.IsNullOrEmpty(installDir))
                paths[subKeyName] = NormalizePath(installDir);
        }
    }

    /// <summary>
    /// Ubisoft: enumerate HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\{gameId}
    /// and read "InstallDir" from each subkey.
    /// </summary>
    private static Dictionary<string, string> EnumerateUbisoftGamePaths(IRegistryReader registry)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";

        foreach (string gameId in registry.EnumerateSubKeyNames(basePath))
        {
            string subKeyPath = basePath + @"\" + gameId;
            string? installDir = registry.ReadStringValue(subKeyPath, "InstallDir");
            if (!string.IsNullOrEmpty(installDir))
                paths[gameId] = NormalizePath(installDir);
        }

        return paths;
    }

    /// <summary>
    /// GOG: enumerate HKLM\SOFTWARE\WOW6432Node\GOG.com\Games\{gameId}
    /// and read "path" from each subkey.
    /// </summary>
    private static Dictionary<string, string> EnumerateGogGamePaths(IRegistryReader registry)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games";

        foreach (string gameId in registry.EnumerateSubKeyNames(basePath))
        {
            string subKeyPath = basePath + @"\" + gameId;
            string? gamePath = registry.ReadStringValue(subKeyPath, "path");
            if (!string.IsNullOrEmpty(gamePath))
                paths[gameId] = NormalizePath(gamePath);
        }

        return paths;
    }

    /// <summary>
    /// Rockstar: enumerate HKLM\SOFTWARE\WOW6432Node\Rockstar Games\{gameName}
    /// and read "InstallFolder" from each subkey (skip "Launcher" subkey).
    /// </summary>
    private static Dictionary<string, string> EnumerateRockstarGamePaths(IRegistryReader registry)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games";

        foreach (string gameName in registry.EnumerateSubKeyNames(basePath))
        {
            // Skip the Launcher subkey — it's the launcher itself, not a game
            if (gameName.Equals("Launcher", StringComparison.OrdinalIgnoreCase))
                continue;

            string subKeyPath = basePath + @"\" + gameName;
            string? installFolder = registry.ReadStringValue(subKeyPath, "InstallFolder");
            if (!string.IsNullOrEmpty(installFolder))
                paths[gameName] = NormalizePath(installFolder);
        }

        return paths;
    }

    // ── Path matching helpers ──────────────────────────────────────

    /// <summary>
    /// Checks if the given path matches any value in the path dictionary.
    /// Uses case-insensitive comparison with normalized paths.
    /// </summary>
    private static bool MatchesAnyPath(string gamePath, Dictionary<string, string> registryPaths)
    {
        foreach (string knownPath in registryPaths.Values)
        {
            if (string.Equals(gamePath, knownPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Normalizes a path for consistent comparison.
    /// Converts forward slashes to backslashes, trims trailing separators.
    /// </summary>
    internal static string NormalizePath(string path)
    {
        string normalized = path.Replace('/', '\\');
        normalized = normalized.TrimEnd('\\');
        return normalized;
    }
}
