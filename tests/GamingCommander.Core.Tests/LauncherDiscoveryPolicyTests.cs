using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Pins the launcher discovery policy matrix: what each Setup setting allows to be read.
/// </summary>
public sealed class LauncherDiscoveryPolicyTests
{
    [Fact]
    public void Automatic_AllowsEveryLauncher()
    {
        var policy = new LauncherDiscoveryPolicy(LauncherDiscoverySetting.Automatic);

        Assert.All(LauncherKinds.All, kind => Assert.True(policy.AllowsDiscovery(kind)));
        Assert.False(policy.AllowsDiscovery(GameSourceKind.Standalone));
        Assert.False(policy.AllowsDiscovery(GameSourceKind.SteamEmu));
    }

    [Fact]
    public void Selected_AllowsOnlySelectedLaunchers()
    {
        var policy = new LauncherDiscoveryPolicy(
            LauncherDiscoverySetting.Selected,
            [GameSourceKind.Steam, GameSourceKind.Epic, GameSourceKind.Steam]);

        Assert.True(policy.AllowsDiscovery(GameSourceKind.Steam));
        Assert.True(policy.AllowsDiscovery(GameSourceKind.Epic));
        Assert.False(policy.AllowsDiscovery(GameSourceKind.Gog));
        Assert.Equal([GameSourceKind.Steam, GameSourceKind.Epic], policy.SelectedLaunchers);
    }

    [Theory]
    [InlineData(LauncherDiscoverySetting.Unset)]
    [InlineData(LauncherDiscoverySetting.FileOnly)]
    public void UnsetAndFileOnly_AllowNoLauncherReads(LauncherDiscoverySetting setting)
    {
        var policy = new LauncherDiscoveryPolicy(setting, LauncherKinds.All);

        Assert.All(LauncherKinds.All, kind => Assert.False(policy.AllowsDiscovery(kind)));
        Assert.False(policy.AllowsRegistryFallback);
        Assert.False(policy.AllowsEpicCatalog);
        Assert.False(policy.AllowsStorePolicies);
    }

    [Fact]
    public void RegistryEpicAndPolicies_FollowTheSetting()
    {
        var automatic = new LauncherDiscoveryPolicy(LauncherDiscoverySetting.Automatic);
        Assert.True(automatic.AllowsRegistryFallback);
        Assert.True(automatic.AllowsEpicCatalog);
        Assert.True(automatic.AllowsStorePolicies);

        var selectedSteam = new LauncherDiscoveryPolicy(
            LauncherDiscoverySetting.Selected, [GameSourceKind.Steam]);
        Assert.True(selectedSteam.AllowsRegistryFallback);
        Assert.False(selectedSteam.AllowsEpicCatalog);
        Assert.True(selectedSteam.AllowsStorePolicies);

        var selectedEpic = new LauncherDiscoveryPolicy(
            LauncherDiscoverySetting.Selected, [GameSourceKind.Epic]);
        Assert.True(selectedEpic.AllowsEpicCatalog);
    }
}
