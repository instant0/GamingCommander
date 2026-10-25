using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Policy provider backed by <see cref="IConfigService"/>. The lock keeps the background
/// sync's reads/writes consistent with settings changes from the Setup screen.
/// </summary>
public sealed class StorePolicyProvider : IStorePolicyProvider
{
    private readonly IConfigService _configService;
    private readonly object _gate = new();

    /// <summary>Creates a provider that persists policy through <paramref name="configService"/>.</summary>
    public StorePolicyProvider(IConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <inheritdoc />
    public StorePolicy Get(GameSourceKind kind)
    {
        if (!LauncherKinds.IsLauncher(kind))
            return Disabled(kind);

        lock (_gate)
        {
            return LoadPolicies().First(policy => policy.Kind == kind);
        }
    }

    /// <inheritdoc />
    public void Set(StorePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (!LauncherKinds.IsLauncher(policy.Kind))
            throw new ArgumentException($"'{policy.Kind}' is not a store launcher.", nameof(policy));

        lock (_gate)
        {
            AppConfig config = _configService.Load();
            List<StorePolicy> stores = (config.StorePolicies?.Stores ?? [])
                .Where(existing => existing is not null && LauncherKinds.IsLauncher(existing.Kind))
                .Select(Normalize)
                .ToList();

            StorePolicy normalized = Normalize(policy);
            int index = stores.FindIndex(existing => existing.Kind == normalized.Kind);
            if (index >= 0)
                stores[index] = normalized;
            else
                stores.Add(normalized);

            _configService.Save(config with
            {
                StorePolicies = new StorePolicySettings { Stores = stores },
            });
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<GameSourceKind> EnabledStores =>
        SelectStores(policy => policy.IndexEnabled);

    /// <inheritdoc />
    public IReadOnlyList<GameSourceKind> CheckOnLaunchStores =>
        SelectStores(policy => policy.IndexEnabled && policy.CheckOnLaunch);

    /// <inheritdoc />
    public bool CanImport(GameSourceKind kind) =>
        LauncherKinds.IsLauncher(kind) && Get(kind).IndexEnabled;

    /// <inheritdoc />
    public bool CanSync(GameSourceKind kind) =>
        LauncherKinds.IsLauncher(kind) && Get(kind) is { IndexEnabled: true, CheckOnLaunch: true };

    private IReadOnlyList<GameSourceKind> SelectStores(Func<StorePolicy, bool> predicate)
    {
        lock (_gate)
        {
            return LoadPolicies()
                .Where(predicate)
                .Select(policy => policy.Kind)
                .ToArray();
        }
    }

    private StorePolicy[] LoadPolicies()
    {
        StorePolicySettings? settings = _configService.Load().StorePolicies;
        var byKind = new Dictionary<GameSourceKind, StorePolicy>();
        foreach (StorePolicy policy in settings?.Stores ?? [])
        {
            if (policy is not null && LauncherKinds.IsLauncher(policy.Kind))
                byKind[policy.Kind] = Normalize(policy);
        }

        var result = new StorePolicy[LauncherKinds.All.Count];
        for (int i = 0; i < result.Length; i++)
        {
            GameSourceKind kind = LauncherKinds.All[i];
            result[i] = byKind.TryGetValue(kind, out StorePolicy? policy)
                ? policy
                : Disabled(kind);
        }

        return result;
    }

    private static StorePolicy Normalize(StorePolicy policy) =>
        policy.IndexEnabled
            ? policy
            : policy with { CheckOnLaunch = false, EnrichNewGames = false };

    private static StorePolicy Disabled(GameSourceKind kind) => new(kind);
}
