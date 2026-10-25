using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for StorePolicyProvider gating, normalization, and non-launcher handling.
/// </summary>
public sealed class StorePolicyProviderTests
{
    private sealed class InMemoryConfigService : IConfigService
    {
        public AppConfig Current { get; set; } = new([], IsFirstRun: false);

        public AppConfig Load() => Current;

        public void Save(AppConfig config) => Current = config;
    }

    private static (StorePolicyProvider Provider, InMemoryConfigService Config) CreateProvider()
    {
        var config = new InMemoryConfigService();
        return (new StorePolicyProvider(config), config);
    }

    [Theory]
    [InlineData(GameSourceKind.Unknown)]
    [InlineData(GameSourceKind.Standalone)]
    [InlineData(GameSourceKind.SteamEmu)]
    public void Get_NonLauncher_ReturnsDisabledDefault(GameSourceKind kind)
    {
        (StorePolicyProvider provider, _) = CreateProvider();

        StorePolicy policy = provider.Get(kind);

        Assert.Equal(kind, policy.Kind);
        Assert.False(policy.IndexEnabled);
        Assert.False(policy.CheckOnLaunch);
        Assert.False(policy.EnrichNewGames);
        Assert.True(policy.SkipUnchangedManifests);
        Assert.Null(policy.InventoryPathOverride);
    }

    [Fact]
    public void Get_MissingLauncher_ReturnsDisabledDefault()
    {
        (StorePolicyProvider provider, _) = CreateProvider();

        StorePolicy policy = provider.Get(GameSourceKind.Xbox);

        Assert.False(policy.IndexEnabled);
        Assert.False(policy.CheckOnLaunch);
        Assert.False(policy.EnrichNewGames);
        Assert.True(policy.SkipUnchangedManifests);
    }

    [Theory]
    [InlineData(GameSourceKind.Unknown)]
    [InlineData(GameSourceKind.Standalone)]
    [InlineData(GameSourceKind.SteamEmu)]
    public void Set_NonLauncher_ThrowsArgumentException(GameSourceKind kind)
    {
        (StorePolicyProvider provider, _) = CreateProvider();

        Assert.Throws<ArgumentException>(() => provider.Set(new StorePolicy(kind, IndexEnabled: true)));
    }

    [Fact]
    public void Set_DisabledIndex_ForcesCheckAndEnrichOff()
    {
        (StorePolicyProvider provider, _) = CreateProvider();

        provider.Set(new StorePolicy(
            GameSourceKind.Steam,
            IndexEnabled: false,
            CheckOnLaunch: true,
            EnrichNewGames: true));

        StorePolicy policy = provider.Get(GameSourceKind.Steam);
        Assert.False(policy.IndexEnabled);
        Assert.False(policy.CheckOnLaunch);
        Assert.False(policy.EnrichNewGames);
    }

    [Fact]
    public void Set_RoundTripsAndPreservesOtherStores()
    {
        (StorePolicyProvider provider, _) = CreateProvider();
        provider.Set(new StorePolicy(GameSourceKind.Steam, IndexEnabled: true, CheckOnLaunch: true));
        provider.Set(new StorePolicy(GameSourceKind.Gog, IndexEnabled: true, InventoryPathOverride: "D:/gog"));

        provider.Set(new StorePolicy(GameSourceKind.Steam, IndexEnabled: true, EnrichNewGames: true));

        Assert.Equal(
            new StorePolicy(GameSourceKind.Steam, true, false, true, true, null),
            provider.Get(GameSourceKind.Steam));
        Assert.Equal(
            new StorePolicy(GameSourceKind.Gog, true, false, false, true, "D:/gog"),
            provider.Get(GameSourceKind.Gog));
    }

    [Fact]
    public void GatingHelpers_RespectIndexAndCheckOnLaunch()
    {
        (StorePolicyProvider provider, _) = CreateProvider();
        provider.Set(new StorePolicy(GameSourceKind.Steam, IndexEnabled: true, CheckOnLaunch: true));
        provider.Set(new StorePolicy(GameSourceKind.Epic, IndexEnabled: true, CheckOnLaunch: false));
        provider.Set(new StorePolicy(GameSourceKind.Gog, IndexEnabled: false, CheckOnLaunch: true));

        Assert.True(provider.CanImport(GameSourceKind.Steam));
        Assert.True(provider.CanSync(GameSourceKind.Steam));

        Assert.True(provider.CanImport(GameSourceKind.Epic));
        Assert.False(provider.CanSync(GameSourceKind.Epic));

        Assert.False(provider.CanImport(GameSourceKind.Gog));
        Assert.False(provider.CanSync(GameSourceKind.Gog));

        Assert.False(provider.CanImport(GameSourceKind.Standalone));
        Assert.False(provider.CanSync(GameSourceKind.Unknown));
    }

    [Fact]
    public void StoreLists_AreFilteredAndOrderedByLauncherOrder()
    {
        (StorePolicyProvider provider, _) = CreateProvider();
        provider.Set(new StorePolicy(GameSourceKind.Rockstar, IndexEnabled: true, CheckOnLaunch: true));
        provider.Set(new StorePolicy(GameSourceKind.Steam, IndexEnabled: true, CheckOnLaunch: true));
        provider.Set(new StorePolicy(GameSourceKind.Epic, IndexEnabled: true, CheckOnLaunch: false));
        provider.Set(new StorePolicy(GameSourceKind.Gog, IndexEnabled: false, CheckOnLaunch: false));

        Assert.Equal(
            [GameSourceKind.Steam, GameSourceKind.Epic, GameSourceKind.Rockstar],
            provider.EnabledStores);
        Assert.Equal(
            [GameSourceKind.Steam, GameSourceKind.Rockstar],
            provider.CheckOnLaunchStores);
    }

    [Fact]
    public void Get_IgnoresNonLauncherRowsInStoredSettings()
    {
        (StorePolicyProvider provider, InMemoryConfigService config) = CreateProvider();
        config.Current = config.Current with
        {
            StorePolicies = new StorePolicySettings
            {
                Stores =
                [
                    new StorePolicy(GameSourceKind.Standalone, IndexEnabled: true),
                    new StorePolicy(GameSourceKind.SteamEmu, IndexEnabled: true),
                    new StorePolicy(GameSourceKind.Steam, IndexEnabled: true),
                ],
            },
        };

        Assert.False(provider.CanImport(GameSourceKind.Standalone));
        Assert.False(provider.CanImport(GameSourceKind.SteamEmu));
        Assert.True(provider.CanImport(GameSourceKind.Steam));
        Assert.Equal([GameSourceKind.Steam], provider.EnabledStores);
    }

    [Fact]
    public void ConcurrentReadsAndWrites_DoNotThrow()
    {
        (StorePolicyProvider provider, _) = CreateProvider();

        Parallel.For(0, 100, i =>
        {
            GameSourceKind kind = i % 2 == 0 ? GameSourceKind.Steam : GameSourceKind.Epic;
            provider.Set(new StorePolicy(kind, IndexEnabled: true, CheckOnLaunch: i % 3 == 0));
            _ = provider.Get(kind);
            _ = provider.EnabledStores;
            _ = provider.CanSync(kind);
        });

        Assert.True(provider.CanImport(GameSourceKind.Steam));
        Assert.True(provider.CanImport(GameSourceKind.Epic));
    }
}
