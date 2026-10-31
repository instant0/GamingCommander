using GamingCommander.App.Services;
using GamingCommander.App.ViewModels;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Startup composition must not probe launchers or read the registry. Discovery happens only on
/// an explicit action, so building the same objects App builds yields zero calls.
/// </summary>
public sealed class LauncherDiscoveryStartupTests
{
    [Theory]
    [InlineData(LauncherDiscoverySetting.Unset)]
    [InlineData(LauncherDiscoverySetting.Automatic)]
    [InlineData(LauncherDiscoverySetting.Selected)]
    [InlineData(LauncherDiscoverySetting.FileOnly)]
    public void Composition_DoesNotProbeOrReadTheRegistry(LauncherDiscoverySetting setting)
    {
        using var temp = new TempDirectory("gc_startup_");
        var locator = new CountingLauncherLocator(
            GameSourceKind.Steam,
            new LauncherDiscoveryResult(
                GameSourceKind.Steam, true, @"D:\Steam", @"D:\Steam\steamapps\libraryfolders.vdf",
                LauncherReadOnlySafety.Safe, "found"));
        AppConfig config = new AppConfig([], IsFirstRun: false) { LauncherDiscovery = setting };

        var context = LauncherDiscoveryContext.FromConfig(config);
        var reader = new CountingRegistryReader();
        var scanner = new FolderScanner(
            [], BlacklistData.Empty, reader, 3, () => context.Policy.AllowsRegistryFallback);
        _ = new LibraryManager(
            new LibrariesDatabaseService(temp.Combine("libraries.json")),
            new GamesDatabaseService(temp.Combine("games.json")),
            scanner,
            new SteamLibraryScanner([]),
            context);
        _ = new LauncherPolicyViewModel(
            new LauncherDiscoveryService([locator]),
            new FakeStorePolicyProvider(),
            _ => false,
            context);

        Assert.Equal(0, locator.Probes);
        Assert.Equal(0, reader.Reads);
    }
}
