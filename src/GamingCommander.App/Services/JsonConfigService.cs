using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of IConfigService. Reads/writes settings.json with DTO mapping.
/// Library (anchor) definitions live in libraries.json (ILibrariesService), not here.
/// </summary>
public sealed class JsonConfigService : IConfigService
{
    private readonly JsonDocumentStore<ConfigDto> _store;

    /// <summary>Creates a new config service targeting the specified JSON file path.</summary>
    /// <param name="configPath">Full path of settings.json.</param>
    /// <param name="warningSink">Optional receiver for store read warnings.</param>
    public JsonConfigService(string configPath, Action<string>? warningSink = null)
    {
        _store = new JsonDocumentStore<ConfigDto>(configPath, warningSink);
    }

    /// <summary>Loads application configuration from disk. Returns defaults if file missing.</summary>
    public AppConfig Load()
    {
        StoreReadResult<ConfigDto> result = _store.Read(() => new ConfigDto());

        ConfigDto loaded = result.Value ?? new ConfigDto();
        IReadOnlyList<string> hiddenFolders = loaded.HiddenFolders ?? [];
        return new AppConfig(
            HiddenFolders: hiddenFolders,
            IsFirstRun: result.Status == StoreReadStatus.Missing,
            LastSeenVersion: loaded.LastSeenVersion,
            EnableOnlineMetadata: loaded.EnableOnlineMetadata,
            ScanDepth: loaded.ScanDepth > 0 ? loaded.ScanDepth : 3,
            StorePolicies: MapPolicies(loaded.StorePolicies))
        {
            LauncherDiscovery = MapLauncherDiscovery(
                loaded.LauncherDiscovery,
                fileMissing: result.Status == StoreReadStatus.Missing),
            SelectedLaunchers = MapSelectedLaunchers(loaded.SelectedLaunchers),
        };
    }

    /// <summary>Serializes and persists the application configuration to disk.</summary>
    public void Save(AppConfig config)
    {
        var dto = new ConfigDto
        {
            HiddenFolders = config.HiddenFolders.ToList(),
            IsFirstRun = config.IsFirstRun,
            LastSeenVersion = config.LastSeenVersion,
            EnableOnlineMetadata = config.EnableOnlineMetadata,
            ScanDepth = config.ScanDepth,
            StorePolicies = MapPolicies(config.StorePolicies),
            LauncherDiscovery = config.LauncherDiscovery.ToString(),
            SelectedLaunchers = LauncherKinds.All
                .Where(config.SelectedLaunchers.Contains)
                .Select(kind => kind.ToString())
                .ToList(),
        };

        _store.Write(dto);
    }

    /// <summary>
    /// Missing file: the user has not chosen yet, so nothing may be probed. An existing file
    /// without the field predates this setting and keeps the legacy "discover everything".
    /// </summary>
    private static LauncherDiscoverySetting MapLauncherDiscovery(string? name, bool fileMissing)
    {
        if (!string.IsNullOrWhiteSpace(name)
            && Enum.TryParse(name, ignoreCase: true, out LauncherDiscoverySetting parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        return fileMissing
            ? LauncherDiscoverySetting.Unset
            : LauncherDiscoverySetting.Automatic;
    }

    private static IReadOnlyList<GameSourceKind> MapSelectedLaunchers(List<string>? names)
    {
        if (names is null || names.Count == 0)
            return [];

        var selected = new HashSet<GameSourceKind>();
        foreach (string? name in names)
        {
            if (TryParseLauncher(name, out GameSourceKind kind))
                selected.Add(kind);
        }

        return LauncherKinds.All.Where(selected.Contains).ToList();
    }

    private static StorePolicySettings MapPolicies(StorePolicySettingsDto? dto)
    {
        var stores = new List<StorePolicy>();
        var seen = new HashSet<GameSourceKind>();
        foreach (StorePolicyDto store in dto?.Stores ?? [])
        {
            if (store is null
                || !TryParseLauncher(store.Kind, out GameSourceKind kind)
                || !seen.Add(kind))
            {
                continue;
            }

            stores.Add(Normalize(new StorePolicy(
                kind,
                store.IndexEnabled,
                store.CheckOnLaunch,
                store.EnrichNewGames,
                store.SkipUnchangedManifests,
                store.InventoryPathOverride)));
        }

        return new StorePolicySettings { Stores = stores };
    }

    private static StorePolicySettingsDto MapPolicies(StorePolicySettings? settings)
    {
        return new StorePolicySettingsDto
        {
            Stores = (settings?.Stores ?? [])
                .Where(policy => policy is not null && LauncherKinds.IsLauncher(policy.Kind))
                .Select(policy =>
                {
                    StorePolicy normalized = Normalize(policy);
                    return new StorePolicyDto
                    {
                        Kind = normalized.Kind.ToString(),
                        IndexEnabled = normalized.IndexEnabled,
                        CheckOnLaunch = normalized.CheckOnLaunch,
                        EnrichNewGames = normalized.EnrichNewGames,
                        SkipUnchangedManifests = normalized.SkipUnchangedManifests,
                        InventoryPathOverride = normalized.InventoryPathOverride,
                    };
                })
                .ToList(),
        };
    }

    private static bool TryParseLauncher(string? name, out GameSourceKind kind)
    {
        kind = default;
        return !string.IsNullOrWhiteSpace(name)
            && Enum.TryParse(name, ignoreCase: true, out kind)
            && LauncherKinds.IsLauncher(kind);
    }

    private static StorePolicy Normalize(StorePolicy policy) =>
        policy.IndexEnabled
            ? policy
            : policy with { CheckOnLaunch = false, EnrichNewGames = false };

    private sealed class ConfigDto
    {
        public List<string>? HiddenFolders { get; set; }
        public bool IsFirstRun { get; set; }
        public string? LastSeenVersion { get; set; }
        public bool EnableOnlineMetadata { get; set; }
        public int ScanDepth { get; set; } = 3;
        public StorePolicySettingsDto? StorePolicies { get; set; }
        public string? LauncherDiscovery { get; set; }
        public List<string>? SelectedLaunchers { get; set; }
    }

    private sealed class StorePolicySettingsDto
    {
        public List<StorePolicyDto>? Stores { get; set; }
    }

    private sealed class StorePolicyDto
    {
        public string? Kind { get; set; }
        public bool IndexEnabled { get; set; }
        public bool CheckOnLaunch { get; set; }
        public bool EnrichNewGames { get; set; }
        public bool SkipUnchangedManifests { get; set; } = true;
        public string? InventoryPathOverride { get; set; }
    }
}
