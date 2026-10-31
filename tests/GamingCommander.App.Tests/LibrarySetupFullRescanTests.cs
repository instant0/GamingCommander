using GamingCommander.App.Services;
using GamingCommander.App.ViewModels;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// The Setup Full rescan action: one policy-allowed discovery, then every anchor is rescanned.
/// FileOnly skips the discovery and still scans folders.
/// </summary>
public sealed class LibrarySetupFullRescanTests
{
    [Fact]
    public async Task FullRescan_ProbesOnceThenSkipsInFileOnly_WhileKeepingGames()
    {
        using var temp = new TempDirectory("gc_fullrescan_");
        string root = temp.CreateDirectory("Root");
        temp.CreateDirectory("Root", "FullRescanGame");
        temp.CreateFile(Path.Combine("Root", "FullRescanGame", "FullRescanGame.exe"), new byte[16]);

        var libraries = new LibrariesDatabaseService(temp.Combine("libraries.json"));
        libraries.Upsert(new Library("Root", GameSourceKind.Standalone, [root]));
        var database = new GamesDatabaseService(temp.Combine("games.json"));
        var context = new LauncherDiscoveryContext(LauncherDiscoverySetting.Automatic);
        var locator = new CountingLauncherLocator(
            GameSourceKind.Steam,
            new LauncherDiscoveryResult(
                GameSourceKind.Steam, true, @"D:\Steam", @"D:\Steam\steamapps\libraryfolders.vdf",
                LauncherReadOnlySafety.Safe, "found"));
        var manager = new LibraryManager(libraries, database, new FolderScanner(), null, context);
        var vm = new LibrarySetupViewModel(
            new InMemoryConfigService(new AppConfig([], IsFirstRun: false)),
            database,
            manager,
            window: null!,
            storePolicies: new FakeStorePolicyProvider(),
            launcherDiscovery: new LauncherDiscoveryService([locator]),
            discoveryContext: context);

        await vm.FullRescanAsync();

        Assert.Equal(1, locator.Probes);
        Assert.Single(database.GetGamesForLibrary("Root"));

        context.Update(LauncherDiscoverySetting.FileOnly);
        await vm.FullRescanAsync();

        Assert.Equal(1, locator.Probes);
        Assert.Single(database.GetGamesForLibrary("Root"));
    }
}
