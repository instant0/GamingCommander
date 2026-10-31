using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>In-memory <see cref="IStorePolicyProvider"/> with the same defaults as the real one.</summary>
internal sealed class FakeStorePolicyProvider : IStorePolicyProvider
{
    private readonly Dictionary<GameSourceKind, StorePolicy> _policies = [];

    public StorePolicy Get(GameSourceKind kind) =>
        _policies.TryGetValue(kind, out StorePolicy? policy) ? policy : new StorePolicy(kind);

    public void Set(StorePolicy policy) => _policies[policy.Kind] = policy;

    public IReadOnlyList<GameSourceKind> EnabledStores =>
        _policies.Values
            .Where(p => p.IndexEnabled)
            .Select(p => p.Kind)
            .ToList();

    public IReadOnlyList<GameSourceKind> CheckOnLaunchStores =>
        _policies.Values
            .Where(p => p.CheckOnLaunch)
            .Select(p => p.Kind)
            .ToList();

    public bool CanImport(GameSourceKind kind) => Get(kind).IndexEnabled;

    public bool CanSync(GameSourceKind kind) => Get(kind) is { IndexEnabled: true, CheckOnLaunch: true };
}
