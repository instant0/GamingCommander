using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>Registry reader that returns nothing — used on non-Windows / test defaults.</summary>
public sealed class NullRegistryReader : IRegistryReader
{
    public string? ReadStringValue(string keyPath, string valueName) => null;
    public IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath) => new Dictionary<string, string>();
    public IReadOnlyList<string> EnumerateSubKeyNames(string keyPath) => [];
}

/// <summary>
/// Locates the Steam install and all Steam library roots via the registry + the
/// libraryfolders.vdf master index (Plan 123 §8.3, confirmed design 2026-09-07).
///
/// Chain: HKLM\SOFTWARE\Valve\Steam\InstallPath → &lt;InstallPath&gt;\steamapps\libraryfolders.vdf
/// → every library path (the install folder itself is library "0").
///
/// AUTHORITY RULE: the vdf is a PATH LOCATOR ONLY — it says WHERE libraries are,
/// never WHAT they contain. The ACF+folder scan (SteamLibraryScanner) decides content.
/// </summary>
public sealed class SteamInstallPathLocator
{
    private const string ValveSteamKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";
    private const string InstallPathValue = "InstallPath";
    private readonly IRegistryReader _registry;

    public SteamInstallPathLocator(IRegistryReader registry)
    {
        _registry = registry;
    }

    /// <summary>The Steam install path from the registry, or null when Steam is not installed.</summary>
    public string? FindInstallPath()
    {
        string? raw = _registry.ReadStringValue(ValveSteamKey, InstallPathValue);
        return string.IsNullOrWhiteSpace(raw) ? null : SteamAcfParser.NormalizePath(raw);
    }

    /// <summary>
    /// True when Steam is installed (registry InstallPath resolves) AND the vdf
    /// yields at least one library. Used to gate the "Add Steam" button (Q4: the
    /// button is HIDDEN when Steam is not installed — we cannot guess the vdf).
    /// </summary>
    public bool IsSteamAvailable
    {
        get
        {
            string? installPath = FindInstallPath();
            if (installPath is null)
                return false;
            return SteamAcfParser.DiscoverLibraryPaths(installPath).Count > 0;
        }
    }

    /// <summary>
    /// All Steam library roots: the install path + every path from the vdf.
    /// Returns empty when Steam is not installed or the vdf has no libraries.
    /// </summary>
    public IReadOnlyList<string> FindAllLibraries()
    {
        string? installPath = FindInstallPath();
        if (installPath is null)
            return [];

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            installPath,
        };
        foreach (string discovered in SteamAcfParser.DiscoverLibraryPaths(installPath))
            paths.Add(discovered);
        return paths.ToList();
    }
}