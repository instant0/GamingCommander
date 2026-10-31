using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>Steam locator: a valid Steam library folder, not the Steam.exe install path.</summary>
internal sealed class SteamLauncherLocator : ILauncherLocator
{
    private readonly Func<IEnumerable<string>> _configuredRoots;
    private readonly SteamInstallPathLocator _installPathLocator;

    public SteamLauncherLocator(
        Func<IEnumerable<string>> configuredRoots,
        IRegistryReader registry)
    {
        _configuredRoots = configuredRoots;
        _installPathLocator = new SteamInstallPathLocator(registry);
    }

    public GameSourceKind Kind => GameSourceKind.Steam;

    public LauncherDiscoveryResult Probe()
    {
        // Configured roots are the authoritative, registry-free source: if one already has the
        // Steam library structure, no registry read is needed.
        string? source = null;
        foreach (string root in _configuredRoots())
        {
            if (TryLibrary(root, out source))
                return Found(root, source);
        }

        // Only when no configured root is a Steam library: one registry value tells us where the
        // main library index is. This is the only reason to read the registry here.
        string? installPath = _installPathLocator.FindInstallPath();
        if (string.IsNullOrWhiteSpace(installPath))
            return LauncherDiscoveryResult.NotFound(Kind, "No Steam library configured or found.");

        if (TryLibrary(installPath, out source))
            return Found(installPath, source);

        return LauncherDiscoveryResult.NotFound(
            Kind, $"Steam path '{installPath}' has no steamapps library structure.");
    }

    private LauncherDiscoveryResult Found(string root, string source) =>
        new(
            Kind,
            Found: true,
            InstallPath: root, // the library root, not the Steam.exe folder
            InventorySource: source,
            LauncherReadOnlySafety.Safe,
            source.EndsWith("libraryfolders.vdf", StringComparison.OrdinalIgnoreCase)
                ? "Steam library index found."
                : "Steam library folder found.");

    /// <summary>
    /// A Steam library is <c>steamapps</c> plus at least one of: <c>libraryfolders.vdf</c>,
    /// <c>common</c>, or an <c>appmanifest_*.acf</c>. The vdf is optional for a single library; it is
    /// the index that links other libraries.
    /// </summary>
    private static bool TryLibrary(string root, out string? source)
    {
        source = null;
        if (string.IsNullOrWhiteSpace(root))
            return false;

        string steamapps = Path.Combine(root, "steamapps");
        if (!Directory.Exists(steamapps))
            return false;

        string vdf = Path.Combine(steamapps, "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            source = vdf;
            return true;
        }

        if (Directory.Exists(Path.Combine(steamapps, "common")))
        {
            source = steamapps;
            return true;
        }

        try
        {
            if (Directory.EnumerateFiles(steamapps, "appmanifest_*.acf").Any())
            {
                source = steamapps;
                return true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // An unreadable library is not a valid source; no exception escapes.
        }

        return false;
    }
}

/// <summary>GOG locator: the Galaxy client path, used as the inventory hint.</summary>
internal sealed class GogLauncherLocator : ILauncherLocator
{
    private const string GalaxyPathsKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths";

    private readonly IRegistryReader _registry;
    private readonly IReadOnlyList<string> _defaultPaths;

    public GogLauncherLocator(IRegistryReader registry, IEnumerable<string>? defaultPaths = null)
    {
        _registry = registry;
        _defaultPaths = defaultPaths?.ToList() ?? DefaultInstallPaths().ToList();
    }

    public GameSourceKind Kind => GameSourceKind.Gog;

    public LauncherDiscoveryResult Probe()
    {
        string? root = _registry.ReadStringValue(GalaxyPathsKey, "common")
            ?? _registry.ReadStringValue(GalaxyPathsKey, "client");

        if (string.IsNullOrWhiteSpace(root))
        {
            foreach (string candidate in _defaultPaths)
            {
                if (Directory.Exists(candidate))
                {
                    root = candidate;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(root))
            return LauncherDiscoveryResult.NotFound(Kind, "GOG Galaxy path not found.");

        string path = PathText.TrimTrailingSeparators(root);
        if (!Directory.Exists(path))
            return LauncherDiscoveryResult.NotFound(
                Kind, $"GOG Galaxy path does not exist: '{path}'.");

        return new LauncherDiscoveryResult(
            Kind,
            Found: true,
            InstallPath: path,
            InventorySource: path,
            LauncherReadOnlySafety.NeedsVerification,
            "GOG Galaxy found; per-game registry/.info is the inventory, Galaxy DB is not read.");
    }

    private static IEnumerable<string> DefaultInstallPaths()
    {
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            yield return Path.Combine(programFilesX86, "GOG Galaxy");
    }
}

/// <summary>Ubisoft locator: the per-game registry list is the inventory source.</summary>
internal sealed class UbisoftLauncherLocator : ILauncherLocator
{
    private const string InstallsKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";

    private readonly IRegistryReader _registry;

    public UbisoftLauncherLocator(IRegistryReader registry)
    {
        _registry = registry;
    }

    public GameSourceKind Kind => GameSourceKind.UbisoftConnect;

    public LauncherDiscoveryResult Probe()
    {
        IReadOnlyList<string> gameIds;
        try
        {
            gameIds = _registry.EnumerateSubKeyNames(InstallsKey);
        }
        catch (Exception ex)
        {
            return LauncherDiscoveryResult.NotFound(
                Kind, $"Ubisoft registry is unreadable ({ex.GetType().Name}).");
        }

        if (gameIds.Count == 0)
            return LauncherDiscoveryResult.NotFound(
                Kind, $"No Ubisoft install records at '{InstallsKey}'.");

        return new LauncherDiscoveryResult(
            Kind,
            Found: true,
            InstallPath: null,
            InventorySource: InstallsKey,
            LauncherReadOnlySafety.NeedsVerification,
            "Ubisoft install registry found; manifest/state are not read.");
    }
}

/// <summary>Rockstar locator: launcher install key plus the per-user Settings.json inventory hint.</summary>
internal sealed class RockstarLauncherLocator : ILauncherLocator
{
    // Exact launcher key is unverified; both hives are tried with one documented shape. Adjust
    // these two constants only.
    private const string UserKey =
        @"HKEY_CURRENT_USER\Software\Rockstar Games\Launcher";

    private const string MachineKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher";

    private const string InstallValue = "InstallFolder";

    private readonly IRegistryReader _registry;
    private readonly string _settingsPath;

    public RockstarLauncherLocator(IRegistryReader registry, string? settingsPath = null)
    {
        _registry = registry;
        _settingsPath = settingsPath ?? DefaultSettingsPath();
    }

    public GameSourceKind Kind => GameSourceKind.Rockstar;

    /// <summary>%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json — a hint, never parsed here.</summary>
    public static string DefaultSettingsPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rockstar Games", "Launcher", "Settings.json");

    public LauncherDiscoveryResult Probe()
    {
        string? install =
            _registry.ReadStringValue(UserKey, InstallValue)
            ?? _registry.ReadStringValue(MachineKey, InstallValue);

        if (string.IsNullOrWhiteSpace(install))
            return LauncherDiscoveryResult.NotFound(
                Kind, $"Rockstar launcher install folder not found under '{UserKey}' or '{MachineKey}'.");

        if (!File.Exists(_settingsPath))
            return LauncherDiscoveryResult.NotFound(
                Kind, $"Rockstar Settings.json not found at '{_settingsPath}'.");

        return new LauncherDiscoveryResult(
            Kind,
            Found: true,
            InstallPath: PathText.TrimTrailingSeparators(install),
            InventorySource: _settingsPath,
            LauncherReadOnlySafety.NeedsVerification,
            "Rockstar launcher found; Settings.json is the inventory hint (not parsed).");
    }
}

/// <summary>Xbox: marker-only detection, no package API and no readable inventory in this build.</summary>
internal sealed class XboxLauncherLocator : ILauncherLocator
{
    public GameSourceKind Kind => GameSourceKind.Xbox;

    public LauncherDiscoveryResult Probe() =>
        LauncherDiscoveryResult.Unsupported(
            Kind, "marker-only detection; no inventory reader in this build");
}

/// <summary>Epic locator: the ProgramData manifests directory, validated by at least one item.</summary>
internal sealed class EpicLauncherLocator : ILauncherLocator
{
    private readonly string _manifestsDir;

    public EpicLauncherLocator(string? manifestsDir = null)
    {
        _manifestsDir = manifestsDir ?? EpicManifestPaths.GetManifestsDir();
    }

    public GameSourceKind Kind => GameSourceKind.Epic;

    public LauncherDiscoveryResult Probe()
    {
        string manifestsDir = _manifestsDir;
        if (!Directory.Exists(manifestsDir))
            return LauncherDiscoveryResult.NotFound(
                Kind, $"Epic manifests directory not found at '{manifestsDir}'.");

        try
        {
            if (!Directory.EnumerateFiles(manifestsDir, "*.item").Any())
                return LauncherDiscoveryResult.NotFound(
                    Kind, $"Epic manifests directory has no .item files at '{manifestsDir}'.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return LauncherDiscoveryResult.NotFound(
                Kind, $"Epic manifests directory is unreadable ({ex.GetType().Name}).");
        }

        return new LauncherDiscoveryResult(
            Kind,
            Found: true,
            InstallPath: null,
            InventorySource: manifestsDir,
            LauncherReadOnlySafety.Safe,
            "Epic manifests directory found.");
    }
}
