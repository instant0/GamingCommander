using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Pins <see cref="LibraryManager.SelectScannerAndScan"/> routing: Steam structure must
/// use the Steam scanner, a manifests directory must use the Epic scanner, and anything
/// else must fall through to the folder scanner.
/// </summary>
public sealed class LibraryManagerRoutingTests
{
    [Fact]
    public void SelectScannerAndScan_RoutesByStructureAndConfiguredType()
    {
        using var temp = new TempDirectory("gc_routing_");
        var libraries = new LibrariesDatabaseService(temp.Combine("libraries.json"));
        var database = new GamesDatabaseService(temp.Combine("games.json"));
        var manager = new LibraryManager(
            libraries,
            database,
            new FolderScanner(),
            new SteamLibraryScanner([]));

        // ── Steam structure (steamapps/common) → SteamLibraryScanner ──
        string steamRoot = temp.CreateDirectory("SteamLib");
        temp.CreateDirectory("SteamLib", "steamapps", "common", "SteamGame");
        temp.CreateFile(
            System.IO.Path.Combine("SteamLib", "steamapps", "appmanifest_4242.acf"),
            """
            "AppState" {
                "appid"       "4242"
                "name"        "Steam Routed Game"
                "installdir"  "SteamGame"
                "StateFlags"  "4"
            }
            """);

        GameEntry steam = Assert.Single(
            manager.SelectScannerAndScan(steamRoot, GameSourceKind.Standalone));
        Assert.Equal(GameSourceKind.Steam, steam.GameSource);
        Assert.Equal("4242", steam.Store.SteamAppId);
        Assert.Equal("Installed", steam.Acf.Status);

        // ── Manifests directory (*.item) → EpicLibraryScanner ──
        string epicRoot = temp.CreateDirectory("EpicManifests");
        temp.CreateFile(
            System.IO.Path.Combine("EpicManifests", "routed.item"),
            """
            {"DisplayName":"Epic Routed Game","InstallLocation":"/tmp/gc-routing-missing",
             "LaunchExecutable":"Game.exe","CatalogNamespace":"ns","CatalogItemId":"cat-1",
             "AppName":"app-1","bIsApplication":true,"AppCategories":["games","applications"]}
            """);

        // A known Epic row the manifests catalog does not mention must be re-used as
        // Orphaned, which proves the Epic scanner (not the folder scanner) ran.
        libraries.Upsert(new Library("Epic", GameSourceKind.Epic, [epicRoot]));
        database.SetGamesForLibrary("Epic",
        [
            new TestGame
            {
                Source = GameSourceKind.Epic,
                Library = "Epic",
                Folder = @"D:\Games\EpicOrphan",
                Title = "Epic Orphan",
            }.Build(),
        ]);

        var epicEntries = manager.SelectScannerAndScan(epicRoot, GameSourceKind.Standalone);
        GameEntry epic = Assert.Single(
            epicEntries, e => e.Store.EpicCatalogItemId == "cat-1");
        Assert.Equal(GameSourceKind.Epic, epic.GameSource);
        Assert.Equal("Epic Routed Game", epic.DisplayName);
        GameEntry orphan = Assert.Single(
            epicEntries, e => e.DisplayName == "Epic Orphan");
        Assert.Equal(InstallState.Orphaned, orphan.Install.State);

        // ── Plain folder → FolderScanner ──
        string plainRoot = temp.CreateDirectory("PlainRoot");
        string plainGame = temp.CreateDirectory("PlainRoot", "PlainGame");
        temp.CreateFile(
            System.IO.Path.Combine("PlainRoot", "PlainGame", "PlainGame.exe"),
            new byte[16]);

        GameEntry plain = Assert.Single(
            manager.SelectScannerAndScan(plainRoot, GameSourceKind.Standalone));
        Assert.Equal(GameSourceKind.Standalone, plain.GameSource);
        Assert.Equal("PlainGame", plain.FolderName);
        Assert.Equal(plainGame, plain.FolderPath);
    }
}
