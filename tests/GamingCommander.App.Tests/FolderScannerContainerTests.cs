using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for FolderScanner container detection, organization folder recognition,
/// UE layout signals, non-game folder skipping, and nested-Steam exclusion.
/// </summary>
public sealed class FolderScannerContainerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FolderScanner _scanner;

    public FolderScannerContainerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ScannerContTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _scanner = new FolderScanner();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  UE layouts: UE3 fast path (Binaries/ at root) and UE4-5 platforms
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Gothic3/Binaries/Win64", "Gothic3.exe", false)]
    [InlineData("MyGame/GameName/Binaries/Win32", "MyGame.exe", true)]
    [InlineData("GdkGame/GameName/Binaries/WinGDK", "GdkGame.exe", true)]
    public void Scan_UeLayout_DetectedAsStandalone(string relativeDir, string exeName, bool createEngine)
    {
        string gameFolder = relativeDir.Split('/')[0];
        if (createEngine)
            CreateDir(gameFolder, "Engine");
        string exeDir = CreateDir(relativeDir.Split('/'));
        CreateExe(exeDir, exeName);

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = Assert.Single(entries);
        Assert.Equal(gameFolder, entry.FolderName);
        Assert.Contains(exeName, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Organization folder detection
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_OrganizationFolder_TwoGameChildren_BothDetected()
    {
        CreateDir("EA");
        CreateExe(CreateDir("EA", "Battlefield2042"), "BF2042.exe");
        CreateExe(CreateDir("EA", "NeedForSpeed"), "NFS.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Equal(2, entries.Count);
        var names = entries.Select(e => e.FolderName).OrderBy(n => n).ToList();
        Assert.Equal("Battlefield2042", names[0]);
        Assert.Equal("NeedForSpeed", names[1]);
    }

    [Fact]
    public void Scan_OrganizationFolder_TwoUeGames_BothDetected()
    {
        CreateDir("Publisher");
        CreateDir("Publisher", "Game1", "Engine");
        CreateExe(CreateDir("Publisher", "Game1", "Game1", "Binaries", "Win64"), "Game1.exe");
        CreateDir("Publisher", "Game2", "Engine");
        CreateExe(CreateDir("Publisher", "Game2", "Game2", "Binaries", "Win64"), "Game2.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void Scan_OrganizationFolder_MixedStoreAndStandalone_AllDetected()
    {
        // Publisher/ with GOG game + standalone game: each child keeps its own source.
        CreateDir("Publisher");
        CreateDir("Publisher", "GogGame");
        File.WriteAllBytes(Path.Combine(_tempDir, "Publisher", "GogGame", "goggame.dll"), new byte[10]);
        CreateExe(CreateDir("Publisher", "StandaloneGame"), "Standalone.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Equal(2, entries.Count);
        var gog = entries.First(e => e.GameSource == GameSourceKind.Gog);
        Assert.Equal("GogGame", gog.FolderName);
        Assert.Contains(entries, e => e.GameSource == GameSourceKind.Standalone);
    }

    // ════════════════════════════════════════════════════════════════
    //  Non-game folder skipping
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Soundtrack|Manuals")]
    public void Scan_NonGameFolders_Skipped(string folderNames)
    {
        CreateDir("Publisher");
        foreach (string name in folderNames.Split('|'))
            CreateDir("Publisher", name);

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Empty(entries);
    }

    // ════════════════════════════════════════════════════════════════
    //  Publisher folder pattern (dirs-only root and files-at-root)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_PublisherDirOnlyRoot_RecurseIntoGrandchildren()
    {
        CreateDir("Publisher");
        CreateExe(CreateDir("Publisher", "SubDir"), "Game.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = Assert.Single(entries);
        Assert.Contains("Game.exe", entry.ExecutablePath);
    }

    [Fact]
    public void Scan_PublisherWithFiles_GameChildStillDetected()
    {
        string pubDir = CreateDir("Publisher");
        File.WriteAllBytes(Path.Combine(pubDir, "readme.txt"), new byte[10]);
        CreateExe(CreateDir("Publisher", "SubDir"), "Game.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = Assert.Single(entries);
        Assert.Contains("Game.exe", entry.ExecutablePath);
    }

    // ════════════════════════════════════════════════════════════════
    //  Depth bounding
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_ContainerRecursion_BoundedAtDepth2()
    {
        // A/B/C/D/Game.exe — too deep for container recursion.
        CreateDir("A");
        CreateExe(CreateDir("A", "B", "C", "D"), "DeepGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Empty(entries);
    }

    // ════════════════════════════════════════════════════════════════
    //  Platform subdirectories are not games (B29)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Win32", "Win64")]
    public void Scan_PlatformSubdirs_NotTreatedAsSeparateGames(string first, string second)
    {
        CreateDir("Publisher");
        string firstDir = CreateDir("Publisher", first);
        string secondDir = CreateDir("Publisher", second);
        CreateExe(firstDir, "SomeGame.exe");
        CreateExe(secondDir, "SomeGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.DoesNotContain(entries, e =>
            e.FolderName.Equals(first, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e =>
            e.FolderName.Equals(second, StringComparison.OrdinalIgnoreCase));
    }

    // ════════════════════════════════════════════════════════════════
    //  BattleNet container detection (B26/B31)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Diablo III", "Diablo III.exe", "Diablo")]
    public void Scan_BlizzardContainer_ClassifiedAsBattleNet(string gameFolder, string exeName, string search)
    {
        CreateDir("Blizzard");
        string gameDir = CreateDir("Blizzard", gameFolder);
        File.WriteAllText(Path.Combine(gameDir, ".build.info"),
            "Header\n" +
            "eu|1|key|key2|||tpr/diablo3|host\n");
        CreateExe(gameDir, exeName);

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var game = entries.FirstOrDefault(e =>
            e.FolderName.Contains(search, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(game);
        Assert.Equal(GameSourceKind.BattleNet, game.GameSource);
    }

    [Fact]
    public void Scan_Diablo3_WithBuildInfo_DetectedAsBattleNet()
    {
        // Standalone Blizzard game with .build.info (no Blizzard/ container).
        string gameDir = CreateDir("Diablo III");
        File.WriteAllText(Path.Combine(gameDir, ".build.info"),
            "Header\n" +
            "eu|1|key|key2|||tpr/diablo3|host\n");
        CreateExe(gameDir, "Diablo III.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var game = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Diablo", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(game);
        Assert.Equal(GameSourceKind.BattleNet, game.GameSource);
    }

    // ════════════════════════════════════════════════════════════════
    //  Container child promotion and Steam library location
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_ContainerChild_WithDeepSteamEmuIni_PromotedAsSteamEmu()
    {
        // Publisher/EmuGame/data/steam_emu.ini — the deep signal sits inside the
        // game child, so the container child must be promoted as SteamEmu.
        CreateDir("Publisher");
        string emuData = CreateDir("Publisher", "EmuGame", "data");
        File.WriteAllText(Path.Combine(emuData, "steam_emu.ini"), "AppId=1");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = Assert.Single(entries);
        Assert.Equal("EmuGame", entry.FolderName);
        Assert.Equal(GameSourceKind.SteamEmu, entry.GameSource);
    }

    [Fact]
    public void Scan_SteamLibraryChild_WithApiDllNoAcf_DetectedAsSteam()
    {
        // A child under steamapps/common is Steam by location, even with a
        // steam_api64.dll (emulator signal) and no appmanifest ACF.
        string commonDir = CreateDir("steamapps", "common");
        string gameDir = CreateDir("steamapps", "common", "GameA");
        CreateExe(gameDir, "GameA.exe");
        File.WriteAllBytes(Path.Combine(gameDir, "steam_api64.dll"), new byte[16]);

        var entries = _scanner.Scan(commonDir, GameSourceKind.Steam);

        var entry = Assert.Single(entries);
        Assert.Equal("GameA", entry.FolderName);
        Assert.Equal(GameSourceKind.Steam, entry.GameSource);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    private string CreateDir(params string[] parts)
    {
        string path = Path.Combine(new[] { _tempDir }.Concat(parts).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }

    private void CreateExe(string dir, string fileName)
    {
        File.WriteAllBytes(Path.Combine(dir, fileName), new byte[1024]);
    }
}
