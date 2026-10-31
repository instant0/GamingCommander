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
        StoreReadResult<ConfigDto> result = _store.Read(
            () => new ConfigDto(),
            dto => SchemaVersioning.IsReadable(dto.Version));
        if (result.Status == StoreReadStatus.Unreadable)
        {
            return new AppConfig(HiddenFolders: [], IsFirstRun: true);
        }

        ConfigDto loaded = result.Value ?? new ConfigDto();
        IReadOnlyList<string> hiddenFolders = loaded.HiddenFolders ?? [];
        return new AppConfig(
            HiddenFolders: hiddenFolders,
            IsFirstRun: result.Status == StoreReadStatus.Missing,
            LastSeenVersion: loaded.LastSeenVersion,
            EnableOnlineMetadata: loaded.EnableOnlineMetadata,
            ScanDepth: loaded.ScanDepth > 0 ? loaded.ScanDepth : 3,
            StorePolicies: MapPolicies(loaded.StorePolicies));
    }

    /// <summary>Serializes and persists the application configuration to disk.</summary>
    public void Save(AppConfig config)
    {
        var dto = new ConfigDto
        {
            Version = SchemaVersioning.Current,
            HiddenFolders = config.HiddenFolders.ToList(),
            IsFirstRun = config.IsFirstRun,
            LastSeenVersion = config.LastSeenVersion,
            EnableOnlineMetadata = config.EnableOnlineMetadata,
            ScanDepth = config.ScanDepth,
            StorePolicies = MapPolicies(config.StorePolicies),
        };

        _store.Write(dto);
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
        public int Version { get; set; } = SchemaVersioning.Current;
        public List<string>? HiddenFolders { get; set; }
        public bool IsFirstRun { get; set; }
        public string? LastSeenVersion { get; set; }
        public bool EnableOnlineMetadata { get; set; }
        public int ScanDepth { get; set; } = 3;
        public StorePolicySettingsDto? StorePolicies { get; set; }
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
