using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class EpicLibraryScannerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "epic_" + Guid.NewGuid().ToString("N")[..8]);

    public EpicLibraryScannerTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void Scan_SkipsAddons_KeepsBase()
    {
        File.WriteAllText(Path.Combine(_dir, "base.item"), """
            {"DisplayName":"Base Game","InstallLocation":"/tmp/does-not-exist-epic-base",
             "LaunchExecutable":"Game.exe","CatalogNamespace":"ns","CatalogItemId":"item1",
             "AppName":"app","bIsApplication":true,"AppCategories":["games","applications"]}
            """);
        File.WriteAllText(Path.Combine(_dir, "dlc.item"), """
            {"DisplayName":"DLC Pack","InstallLocation":"/tmp/does-not-exist-epic-base",
             "LaunchExecutable":"","CatalogNamespace":"ns","CatalogItemId":"item2",
             "AppName":"dlc","bIsApplication":false,"AppCategories":["addons"]}
            """);

        var games = new EpicLibraryScanner().Scan(_dir);
        Assert.Single(games);
        Assert.Equal("Base Game", games[0].DisplayName);
        Assert.Equal(GameSourceKind.Epic, games[0].GameSource);
        Assert.Equal("Missing", games[0].PlatformMetadata["EpicStatus"]);
    }

    [Fact]
    public void Scan_DedupsSameCatalogItemId()
    {
        const string body = """
            {"DisplayName":"Dishonored - Definitive Edition","InstallLocation":"/tmp/DishonoredDE",
             "LaunchExecutable":"Binaries/Win64/Dishonored.exe","CatalogNamespace":"ns",
             "CatalogItemId":"same-id","AppName":"app","bIsApplication":true,
             "AppCategories":["games","applications"]}
            """;
        File.WriteAllText(Path.Combine(_dir, "a.item"), body);
        File.WriteAllText(Path.Combine(_dir, "b.item"),
            body.Replace("Binaries/Win64", "/Binaries/Win64"));

        var games = new EpicLibraryScanner().Scan(_dir);
        Assert.Single(games);
        Assert.Equal("Dishonored - Definitive Edition", games[0].DisplayName);
    }

    [Fact]
    public void Scan_ReusesFolderScanRowForOrphan()
    {
        File.WriteAllText(Path.Combine(_dir, "only-dlc.item"), """
            {"DisplayName":"DLC","InstallLocation":"/nope","LaunchExecutable":"",
             "AppCategories":["addons"],"bIsApplication":false}
            """);
        var known = new GameEntry(
            Id: "same-id",
            FolderName: "cavestoryplus",
            DisplayName: "Cave Story+",
            GameSource: GameSourceKind.Epic,
            IsSourceOverridden: false,
            ExecutablePath: @"D:\games\cavestoryplus\CaveStory+.exe",
            LauncherPath: "",
            CommandLineArguments: "",
            ManifestPath: "",
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.UtcNow,
            PlatformMetadata: [],
            Tags: [],
            UserOverrides: []);

        var list = new EpicLibraryScanner().Scan(_dir, [(@"D:\games", known)]);
        GameEntry orphan = Assert.Single(list);
        Assert.Equal("Orphaned", orphan.PlatformMetadata["EpicStatus"]);
        Assert.Equal("same-id", orphan.Id);
        Assert.Equal(@"D:\games\cavestoryplus\CaveStory+.exe", orphan.ExecutablePath);
        Assert.Equal("Cave Story+", orphan.DisplayName);
    }
}
