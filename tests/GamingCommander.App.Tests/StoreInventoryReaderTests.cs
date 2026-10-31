using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

public sealed class StoreInventoryReaderTests
{
    // ── Steam ──────────────────────────────────────────────────────

    [Fact]
    public void Steam_AcfAndFolder_ReturnsInstalledEntry()
    {
        using var temp = new TempDirectory();
        string acf = temp.CreateFile(
            Path.Combine("steamapps", "appmanifest_570.acf"),
            Acf("570", "Dota 2", "dota 2 beta"));
        temp.CreateDirectory("steamapps", "common", "dota 2 beta");

        IReadOnlyList<StoreInventoryEntry> entries = new SteamInventoryReader([temp.Root]).Read();

        StoreInventoryEntry entry = entries.Single(e => e.StoreId == "570");
        Assert.Equal(GameSourceKind.Steam, entry.Kind);
        Assert.Equal("Dota 2", entry.Name);
        Assert.Equal(temp.Combine("steamapps", "common", "dota 2 beta"), entry.InstallPath);
        Assert.True(entry.FolderExists);
        Assert.Equal(StoreReportedState.Installed, entry.StoreState);
        var metadata = Assert.IsType<SteamInventoryMetadata>(entry.Metadata);
        Assert.Equal(acf, metadata.AcfPath);
        Assert.Equal("1700000000", metadata.LastUpdated);
    }

    [Fact]
    public void Steam_AcfWithoutFolder_StillReturnsEntry()
    {
        using var temp = new TempDirectory();
        temp.CreateFile(
            Path.Combine("steamapps", "appmanifest_570.acf"),
            Acf("570", "Dota 2", "dota 2 beta"));

        StoreInventoryEntry entry = new SteamInventoryReader([temp.Root]).Read().Single();

        Assert.Equal("570", entry.StoreId);
        Assert.False(entry.FolderExists);
        Assert.Equal(StoreReportedState.Installed, entry.StoreState);
    }

    [Fact]
    public void Steam_CommonFolderWithoutAcf_IsOrphanEntry()
    {
        using var temp = new TempDirectory();
        temp.CreateDirectory("steamapps", "common", "Mystery Game");

        StoreInventoryEntry entry = new SteamInventoryReader([temp.Root]).Read().Single();

        Assert.Equal(string.Empty, entry.StoreId);
        Assert.True(entry.FolderExists);
        Assert.Equal(StoreReportedState.Unknown, entry.StoreState);
        Assert.Null(Assert.IsType<SteamInventoryMetadata>(entry.Metadata).AcfPath);
    }

    [Fact]
    public void Steam_VdfAppsBlock_IsNotTheInstalledSet()
    {
        using var temp = new TempDirectory();
        temp.CreateFile(
            Path.Combine("steamapps", "libraryfolders.vdf"),
            """
            "libraryfolders"
            {
              "0"
              {
                "path" "PLACEHOLDER"
                "apps"
                {
                  "999" "123456"
                }
              }
            }
            """.Replace("PLACEHOLDER", temp.Root));

        IReadOnlyList<StoreInventoryEntry> entries = new SteamInventoryReader([temp.Root]).Read();

        Assert.DoesNotContain(entries, e => e.StoreId == "999");
    }

    [Fact]
    public void Steam_VdfAddsAnotherLibrary()
    {
        using var temp = new TempDirectory();
        string second = temp.CreateDirectory("second");
        temp.CreateFile(
            Path.Combine("steamapps", "libraryfolders.vdf"),
            $$"""
            "libraryfolders"
            {
              "0"
              {
                "path" "{{second}}"
              }
            }
            """);
        temp.CreateFile(
            Path.Combine("second", "steamapps", "appmanifest_730.acf"),
            Acf("730", "Counter-Strike 2", "Counter-Strike Global Offensive"));
        temp.CreateDirectory("second", "steamapps", "common", "Counter-Strike Global Offensive");

        IReadOnlyList<StoreInventoryEntry> entries = new SteamInventoryReader([temp.Root]).Read();

        StoreInventoryEntry entry = entries.Single(e => e.StoreId == "730");
        Assert.True(entry.FolderExists);
    }

    [Fact]
    public void Steam_UpdateFlag_ReportsUpdateRequiredButStillInstalled()
    {
        using var temp = new TempDirectory();
        temp.CreateFile(
            Path.Combine("steamapps", "appmanifest_570.acf"),
            Acf("570", "Dota 2", "dota 2 beta", stateFlags: "6"));
        temp.CreateDirectory("steamapps", "common", "dota 2 beta");

        StoreInventoryEntry entry = new SteamInventoryReader([temp.Root]).Read().Single();

        Assert.Equal(StoreReportedState.UpdateRequired, entry.StoreState);
        Assert.True(entry.FolderExists);
    }

    // ── GOG ────────────────────────────────────────────────────────

    [Fact]
    public void Gog_RegistryRecord_ImportsWithExeAndMetadata()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("The Witcher 3");
        string exe = temp.CreateFile(Path.Combine("The Witcher 3", "witcher3.exe"), "exe");
        var registry = new MockRegistryReader(GogRegistry(
            "1207658924", install, "The Witcher 3", exe: exe));

        StoreInventoryEntry entry = new GogInventoryReader(registry).Read().Single();

        Assert.Equal(GameSourceKind.Gog, entry.Kind);
        Assert.Equal("1207658924", entry.StoreId);
        Assert.Equal("The Witcher 3", entry.Name);
        Assert.Equal(install, entry.InstallPath);
        Assert.Equal(exe, entry.LaunchExe);
        Assert.True(entry.FolderExists);
        Assert.True(entry.ExeExists);
        var metadata = Assert.IsType<GogInventoryMetadata>(entry.Metadata);
        Assert.Equal("12345", metadata.BuildId);
        Assert.Equal("2020-01-01", metadata.InstallDate);
        Assert.True(metadata.FromRegistryOnly);
    }

    [Fact]
    public void Gog_InfoExe_IsPreferred()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("Game");
        string registryExe = temp.CreateFile(Path.Combine("Game", "old.exe"), "old");
        string infoExe = temp.CreateFile(Path.Combine("Game", "bin", "game.exe"), "new");
        temp.CreateFile(
            Path.Combine("Game", "goggame-120.info"),
            """
            {
              "gameId": "120",
              "rootGameId": "120",
              "name": "Info Name",
              "playTasks": [
                { "isPrimary": true, "path": "bin/game.exe", "arguments": "-dx12" }
              ]
            }
            """);
        var registry = new MockRegistryReader(GogRegistry(
            "120", install, "Registry Name", exe: registryExe));

        StoreInventoryEntry entry = new GogInventoryReader(registry).Read().Single();

        Assert.Equal("Info Name", entry.Name);
        Assert.Equal(infoExe, entry.LaunchExe);
        var metadata = Assert.IsType<GogInventoryMetadata>(entry.Metadata);
        Assert.False(metadata.FromRegistryOnly);
        Assert.Equal("-dx12", metadata.LaunchArguments);
    }

    [Fact]
    public void Gog_MissingFolder_StillImports()
    {
        using var temp = new TempDirectory();
        string missing = temp.Combine("gone");
        var registry = new MockRegistryReader(GogRegistry("120", missing, "Gone Game"));

        StoreInventoryEntry entry = new GogInventoryReader(registry).Read().Single();

        Assert.Equal("120", entry.StoreId);
        Assert.False(entry.FolderExists);
        Assert.Null(entry.LaunchExe);
        Assert.False(entry.ExeExists);
    }

    [Fact]
    public void Gog_NoRegistry_ReturnsEmpty()
    {
        IReadOnlyList<StoreInventoryEntry> entries =
            new GogInventoryReader(new MockRegistryReader("")).Read();

        Assert.Empty(entries);
    }

    [Fact]
    public void Gog_MultipleInfoFiles_PreferMainGame()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("Game");
        string mainExe = temp.CreateFile(Path.Combine("Game", "main.exe"), "main");
        temp.CreateFile(Path.Combine("Game", "dlc.exe"), "dlc");
        temp.CreateFile(
            Path.Combine("Game", "goggame-120.info"),
            """
            {
              "gameId": "120",
              "rootGameId": "120",
              "name": "Main Game",
              "playTasks": [ { "isPrimary": true, "path": "main.exe" } ]
            }
            """);
        temp.CreateFile(
            Path.Combine("Game", "goggame-120-dlc.info"),
            """
            {
              "gameId": "120dlc",
              "rootGameId": "120",
              "name": "DLC",
              "playTasks": [ { "isPrimary": true, "path": "dlc.exe" } ]
            }
            """);
        var registry = new MockRegistryReader(GogRegistry("120", install, "Game"));

        StoreInventoryEntry entry = new GogInventoryReader(registry).Read().Single();

        Assert.Equal("Main Game", entry.Name);
        Assert.Equal(mainExe, entry.LaunchExe);
    }

    // ── helpers ────────────────────────────────────────────────────

    private static string Acf(
        string appId,
        string name,
        string installDir,
        string stateFlags = "4") =>
        $$"""
        "AppState"
        {
          "appid" "{{appId}}"
          "name" "{{name}}"
          "installdir" "{{installDir}}"
          "StateFlags" "{{stateFlags}}"
          "LastUpdated" "1700000000"
          "SizeOnDisk" "100"
          "buildid" "123"
        }
        """;

    private static string GogRegistry(
        string id,
        string path,
        string name,
        string? exe = null) =>
        $"""
        [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\{id}]
        "path"=str(2):"{path}"
        "gameName"=str(2):"{name}"
        "gameID"=str(2):"{id}"
        "BUILDID"=str(2):"12345"
        "INSTALLDATE"=str(2):"2020-01-01"
        "launchCommand"=str(2):"game.exe"
        {(exe is null ? string.Empty : $"\"exe\"=str(2):\"{exe}\"\r\n")}
        """;
}
