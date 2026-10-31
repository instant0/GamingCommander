using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class LauncherDiscoveryServiceTests
{
    [Fact]
    public void DiscoverAll_ReturnsOneRowPerSupportedLauncherInOrder()
    {
        var service = new LauncherDiscoveryService([]);

        IReadOnlyList<LauncherDiscoveryResult> results = service.DiscoverAll();

        Assert.Equal(LauncherKinds.All, results.Select(r => r.Kind));
        Assert.All(results, r => Assert.False(r.Found));
        Assert.All(results, r => Assert.Equal(LauncherReadOnlySafety.Unsupported, r.Safety));
    }

    [Fact]
    public void FoundLocator_FieldsArePreserved()
    {
        var locator = new FakeLocator(
            GameSourceKind.Steam,
            new LauncherDiscoveryResult(
                GameSourceKind.Steam, true, @"D:\Steam", @"D:\Steam\steamapps\libraryfolders.vdf",
                LauncherReadOnlySafety.Safe, "Found"));

        LauncherDiscoveryResult result = new LauncherDiscoveryService([locator]).Discover(GameSourceKind.Steam);

        Assert.True(result.Found);
        Assert.Equal(@"D:\Steam", result.InstallPath);
        Assert.Equal(@"D:\Steam\steamapps\libraryfolders.vdf", result.InventorySource);
        Assert.Equal(LauncherReadOnlySafety.Safe, result.Safety);
    }

    [Fact]
    public void MissingLocator_IsUnsupportedNotOmitted()
    {
        var service = new LauncherDiscoveryService([]);

        LauncherDiscoveryResult result = service.Discover(GameSourceKind.Epic);

        Assert.False(result.Found);
        Assert.Equal(GameSourceKind.Epic, result.Kind);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, result.Safety);
        Assert.Contains("No locator", result.Reason);
    }

    [Fact]
    public void ThrowingLocator_DoesNotStopTheOthers()
    {
        var throwing = new ThrowingLocator(GameSourceKind.Gog);
        var found = new FakeLocator(
            GameSourceKind.Steam,
            new LauncherDiscoveryResult(
                GameSourceKind.Steam, true, @"D:\Steam", @"D:\Steam\steamapps\libraryfolders.vdf",
                LauncherReadOnlySafety.Safe, "Found"));

        IReadOnlyList<LauncherDiscoveryResult> results =
            new LauncherDiscoveryService([throwing, found]).DiscoverAll();

        LauncherDiscoveryResult gog = results.Single(r => r.Kind == GameSourceKind.Gog);
        LauncherDiscoveryResult steam = results.Single(r => r.Kind == GameSourceKind.Steam);

        Assert.False(gog.Found);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, gog.Safety);
        Assert.Contains(nameof(InvalidOperationException), gog.Reason);
        Assert.True(steam.Found);
    }

    [Fact]
    public void DuplicateKind_FirstLocatorWins()
    {
        var first = new FakeLocator(
            GameSourceKind.Steam,
            new LauncherDiscoveryResult(
                GameSourceKind.Steam, true, @"D:\First", @"D:\First\steamapps\libraryfolders.vdf",
                LauncherReadOnlySafety.Safe, "First"));
        var second = new FakeLocator(
            GameSourceKind.Steam,
            new LauncherDiscoveryResult(
                GameSourceKind.Steam, true, @"E:\Second", @"E:\Second\steamapps\libraryfolders.vdf",
                LauncherReadOnlySafety.Safe, "Second"));

        LauncherDiscoveryResult result =
            new LauncherDiscoveryService([first, second]).Discover(GameSourceKind.Steam);

        Assert.Equal(@"D:\First", result.InstallPath);
    }

    [Fact]
    public void Discover_MatchesDiscoverAllRow()
    {
        var service = new LauncherDiscoveryService([]);

        LauncherDiscoveryResult single = service.Discover(GameSourceKind.Xbox);
        LauncherDiscoveryResult all = service.DiscoverAll().Single(r => r.Kind == GameSourceKind.Xbox);

        Assert.Equal(all, single);
    }

    [Fact]
    public void Supports_OnlyRegisteredLocators()
    {
        var service = new LauncherDiscoveryService([
            new FakeLocator(GameSourceKind.Steam, LauncherDiscoveryResult.NotFound(GameSourceKind.Steam, "none")),
        ]);

        Assert.True(service.Supports(GameSourceKind.Steam));
        Assert.False(service.Supports(GameSourceKind.Epic));
    }

    [Fact]
    public void DiscoverForPolicy_FileOnly_ProbesNothing()
    {
        var locator = new CountingLocator(GameSourceKind.Steam);
        var service = new LauncherDiscoveryService([locator]);

        IReadOnlyList<LauncherDiscoveryResult> results = service.DiscoverForPolicy(
            new LauncherDiscoveryPolicy(LauncherDiscoverySetting.FileOnly));

        Assert.Empty(results);
        Assert.Equal(0, locator.Probes);
    }

    [Fact]
    public void DiscoverForPolicy_Selected_ProbesOnlySelectedKinds()
    {
        var steam = new CountingLocator(GameSourceKind.Steam);
        var epic = new CountingLocator(GameSourceKind.Epic);
        var gog = new CountingLocator(GameSourceKind.Gog);
        var service = new LauncherDiscoveryService([steam, epic, gog]);

        IReadOnlyList<LauncherDiscoveryResult> results = service.DiscoverForPolicy(
            new LauncherDiscoveryPolicy(
                LauncherDiscoverySetting.Selected,
                [GameSourceKind.Steam, GameSourceKind.Gog]));

        Assert.Equal([GameSourceKind.Steam, GameSourceKind.Gog], results.Select(r => r.Kind));
        Assert.Equal(1, steam.Probes);
        Assert.Equal(1, gog.Probes);
        Assert.Equal(0, epic.Probes);
    }

    [Fact]
    public void DiscoverForPolicy_SelectedMissingLocator_IsUnsupported()
    {
        var service = new LauncherDiscoveryService([]);

        LauncherDiscoveryResult result = Assert.Single(service.DiscoverForPolicy(
            new LauncherDiscoveryPolicy(
                LauncherDiscoverySetting.Selected,
                [GameSourceKind.EaApp])));

        Assert.Equal(GameSourceKind.EaApp, result.Kind);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, result.Safety);
        Assert.False(result.Found);
    }

    private sealed class CountingLocator(GameSourceKind kind) : ILauncherLocator
    {
        public int Probes { get; private set; }

        public GameSourceKind Kind => kind;

        public LauncherDiscoveryResult Probe()
        {
            Probes++;
            return LauncherDiscoveryResult.NotFound(Kind, "none");
        }
    }

    private sealed class FakeLocator(GameSourceKind kind, LauncherDiscoveryResult result) : ILauncherLocator
    {
        public GameSourceKind Kind => kind;

        public LauncherDiscoveryResult Probe() => result;
    }

    private sealed class ThrowingLocator(GameSourceKind kind) : ILauncherLocator
    {
        public GameSourceKind Kind => kind;

        public LauncherDiscoveryResult Probe() => throw new InvalidOperationException("boom");
    }
}
