using GamingCommander.App.Services;
using GamingCommander.App.ViewModels;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

public sealed class LauncherPolicyViewModelTests
{
    [Fact]
    public void Construction_DoesNotProbe()
    {
        var locator = new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam());

        _ = CreateVm([locator]);

        Assert.Equal(0, locator.Probes);
    }

    [Fact]
    public void Detect_PopulatesEveryLauncherRow()
    {
        var locator = new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam());
        LauncherPolicyViewModel vm = CreateVm([locator]);

        vm.Detect();

        Assert.Equal(LauncherKinds.All.Count, vm.Rows.Count);
        Assert.Equal(1, locator.Probes);
        Assert.Equal("1 launcher found.", vm.StatusText);
    }

    [Fact]
    public void Index_DisabledWithoutReader_EvenWhenFound()
    {
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            hasReader: _ => false);
        vm.Detect();

        LauncherPolicyRow steam = vm.Rows.Single(r => r.Kind == GameSourceKind.Steam);
        steam.IndexEnabled = true;

        Assert.False(steam.CanIndex);
        Assert.False(steam.IndexEnabled);
    }

    [Fact]
    public void Index_EnabledWithReader_AndPersistsPolicy()
    {
        var policies = new FakeStorePolicyProvider();
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            policies,
            hasReader: kind => kind == GameSourceKind.Steam);
        vm.Detect();

        LauncherPolicyRow steam = vm.Rows.Single(r => r.Kind == GameSourceKind.Steam);
        steam.IndexEnabled = true;
        steam.CheckOnLaunch = true;

        Assert.True(steam.IndexEnabled);
        Assert.True(steam.CheckOnLaunch);
        Assert.True(policies.Get(GameSourceKind.Steam).IndexEnabled);
        Assert.True(policies.Get(GameSourceKind.Steam).CheckOnLaunch);
    }

    [Fact]
    public void IndexOff_ResetsDependentPolicies()
    {
        var policies = new FakeStorePolicyProvider();
        policies.Set(new StorePolicy(GameSourceKind.Steam, true, true, true));
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            policies,
            hasReader: kind => kind == GameSourceKind.Steam);
        vm.Detect();

        LauncherPolicyRow steam = vm.Rows.Single(r => r.Kind == GameSourceKind.Steam);
        Assert.True(steam.CheckOnLaunch);

        steam.IndexEnabled = false;

        Assert.False(steam.CheckOnLaunch);
        Assert.False(steam.EnrichNewGames);
        Assert.False(policies.Get(GameSourceKind.Steam).CheckOnLaunch);
    }

    [Fact]
    public void UnsupportedRow_CannotBeIndexed()
    {
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.EaApp,
                LauncherDiscoveryResult.Unsupported(GameSourceKind.EaApp, "no reader"))],
            hasReader: _ => true);
        vm.Detect();

        LauncherPolicyRow ea = vm.Rows.Single(r => r.Kind == GameSourceKind.EaApp);
        ea.IndexEnabled = true;

        Assert.False(ea.CanIndex);
        Assert.False(ea.IndexEnabled);
    }

    [Fact]
    public void SettingChoices_DescribeTheThreeSettings()
    {
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            setting: LauncherDiscoverySetting.Automatic);

        Assert.Equal(
            [LauncherDiscoverySetting.Automatic, LauncherDiscoverySetting.Selected, LauncherDiscoverySetting.FileOnly],
            vm.SettingChoices.Select(option => option.Setting));
        Assert.All(vm.SettingChoices, option =>
        {
            Assert.False(string.IsNullOrWhiteSpace(option.Title));
            Assert.False(string.IsNullOrWhiteSpace(option.Effect));
        });
        Assert.Equal(
            vm.SettingChoices.Count,
            vm.SettingChoices.Select(option => option.Effect).Distinct().Count());
        Assert.True(vm.SettingChoices.Single(o => o.Setting == LauncherDiscoverySetting.Automatic).IsChecked);
        Assert.False(vm.SettingChoices.Single(o => o.Setting == LauncherDiscoverySetting.FileOnly).IsChecked);
    }

    [Fact]
    public void SelectedChecklist_ListsOnlySupportedStoresAndMarksOthersInformational()
    {
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            setting: LauncherDiscoverySetting.Selected);

        Assert.True(vm.IsSelectedSetting);
        Assert.True(vm.ShowsLauncherSections);
        Assert.Equal([GameSourceKind.Steam], vm.SelectionOptions.Select(option => option.Kind));
        Assert.Contains(
            vm.InformationalLaunchers,
            info => info.DisplayName == GameSourceParser.ToDisplayName(GameSourceKind.EaApp));
        Assert.Contains(
            vm.InformationalLaunchers,
            info => info.DisplayName == GameSourceParser.ToDisplayName(GameSourceKind.BattleNet));
        Assert.DoesNotContain(
            vm.SelectionOptions,
            option => option.Kind == GameSourceKind.EaApp);
    }

    [Fact]
    public void SelectingLauncher_PersistsSelectionAndUpdatesContext()
    {
        var config = new InMemoryConfigService(new AppConfig([], IsFirstRun: false)
        {
            LauncherDiscovery = LauncherDiscoverySetting.Selected,
        });
        var context = new LauncherDiscoveryContext(LauncherDiscoverySetting.Selected);
        var vm = new LauncherPolicyViewModel(
            new LauncherDiscoveryService([new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())]),
            new FakeStorePolicyProvider(),
            context: context,
            configService: config);

        vm.SelectionOptions.Single(option => option.Kind == GameSourceKind.Steam).IsSelected = true;

        Assert.True(context.Policy.AllowsDiscovery(GameSourceKind.Steam));
        Assert.Equal([GameSourceKind.Steam], config.Current.SelectedLaunchers);
        Assert.Equal(LauncherDiscoverySetting.Selected, config.Current.LauncherDiscovery);
    }

    [Fact]
    public void ChooseSetting_PersistsImmediately_AndDoesNotProbe()
    {
        var locator = new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam());
        var config = new InMemoryConfigService(new AppConfig([], IsFirstRun: false)
        {
            LauncherDiscovery = LauncherDiscoverySetting.Automatic,
        });
        var context = new LauncherDiscoveryContext(LauncherDiscoverySetting.Automatic);
        var vm = new LauncherPolicyViewModel(
            new LauncherDiscoveryService([locator]),
            new FakeStorePolicyProvider(),
            context: context,
            configService: config);
        bool settingChosen = false;
        vm.SettingChosen += () => settingChosen = true;

        vm.Setting = LauncherDiscoverySetting.FileOnly;

        Assert.Equal(LauncherDiscoverySetting.FileOnly, context.Setting);
        Assert.Equal(LauncherDiscoverySetting.FileOnly, config.Current.LauncherDiscovery);
        Assert.True(settingChosen);
        Assert.Equal(0, locator.Probes);
        Assert.False(vm.ShowsLauncherSections);
        Assert.True(vm.SettingChoices.Single(o => o.Setting == LauncherDiscoverySetting.FileOnly).IsChecked);
        Assert.False(vm.SettingChoices.Single(o => o.Setting == LauncherDiscoverySetting.Automatic).IsChecked);
    }

    [Fact]
    public void FileOnly_DisablesPoliciesAndRows_KeepsTheRow()
    {
        var policies = new FakeStorePolicyProvider();
        policies.Set(new StorePolicy(GameSourceKind.Steam, IndexEnabled: true, CheckOnLaunch: true));
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            policies,
            hasReader: kind => kind == GameSourceKind.Steam);
        vm.Detect();
        LauncherPolicyRow steam = vm.Rows.Single(r => r.Kind == GameSourceKind.Steam);
        Assert.True(steam.CanIndex);

        vm.Setting = LauncherDiscoverySetting.FileOnly;

        Assert.Contains(steam, vm.Rows);
        Assert.False(steam.CanIndex);
        Assert.False(steam.IndexEnabled);
        Assert.False(steam.CanCheckOnLaunch);
        Assert.False(steam.CanEnrich);
        Assert.False(policies.Get(GameSourceKind.Steam).IndexEnabled);
        Assert.False(policies.Get(GameSourceKind.Steam).CheckOnLaunch);
    }

    [Fact]
    public void ReturnToAutomatic_EnablesNothingByItself()
    {
        var policies = new FakeStorePolicyProvider();
        LauncherPolicyViewModel vm = CreateVm(
            [new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam())],
            policies,
            setting: LauncherDiscoverySetting.FileOnly,
            hasReader: kind => kind == GameSourceKind.Steam);
        vm.Detect();
        Assert.Empty(vm.Rows);

        vm.Setting = LauncherDiscoverySetting.Automatic;
        vm.Detect();

        LauncherPolicyRow steam = vm.Rows.Single(r => r.Kind == GameSourceKind.Steam);
        Assert.True(steam.CanIndex);
        Assert.False(steam.IndexEnabled);
        Assert.False(policies.Get(GameSourceKind.Steam).IndexEnabled);
    }

    [Fact]
    public void Detect_Selected_ShowsOnlySelectedLauncherRows()
    {
        var steam = new CountingLauncherLocator(GameSourceKind.Steam, FoundSteam());
        var epic = new CountingLauncherLocator(
            GameSourceKind.Epic,
            LauncherDiscoveryResult.Unsupported(GameSourceKind.Epic, "missing"));
        LauncherPolicyViewModel vm = CreateVm(
            [steam, epic],
            setting: LauncherDiscoverySetting.Selected,
            selected: [GameSourceKind.Steam]);

        vm.Detect();

        Assert.Equal([GameSourceKind.Steam], vm.Rows.Select(row => row.Kind));
        Assert.Equal(1, steam.Probes);
        Assert.Equal(0, epic.Probes);
    }

    private static LauncherDiscoveryResult FoundSteam() => new(
        GameSourceKind.Steam, true, @"D:\Steam", @"D:\Steam\steamapps\libraryfolders.vdf",
        LauncherReadOnlySafety.Safe, "found");

    private static LauncherPolicyViewModel CreateVm(
        IReadOnlyList<CountingLauncherLocator> locators,
        IStorePolicyProvider? policies = null,
        LauncherDiscoverySetting setting = LauncherDiscoverySetting.Automatic,
        IReadOnlyList<GameSourceKind>? selected = null,
        Func<GameSourceKind, bool>? hasReader = null,
        IConfigService? config = null)
    {
        var context = new LauncherDiscoveryContext(setting, selected);
        return new LauncherPolicyViewModel(
            new LauncherDiscoveryService(locators),
            policies ?? new FakeStorePolicyProvider(),
            hasReader,
            context,
            config);
    }
}
