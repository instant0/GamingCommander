using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for FolderScanner container detection, organization folder recognition,
/// and UE layout signals (Win32, WinGDK, Steam) added in T68.
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
    //  UE3 Fast Path — Binaries/ at root
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_Ue3BinariesAtRoot_DetectedAsStandalone()
    {
        // Game/Binaries/Win32/Game.exe (UE3 layout, no Engine/)
        string gameDir = CreateDir("UnrealTournament3");
        string win32 = CreateDir("UnrealTournament3", "Binaries", "Win32");
        CreateExe(win32, "UT3.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Single(entries);
        Assert.Equal("UnrealTournament3", entries[0].FolderName);
        Assert.Contains("UT3.exe", entries[0].ExecutablePath);
    }

    [Fact]
    public void Scan_Ue3BinariesWin64AtRoot_DetectedAsStandalone()
    {
        string gameDir = CreateDir("Gothic3");
        string win64 = CreateDir("Gothic3", "Binaries", "Win64");
        CreateExe(win64, "Gothic3.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Single(entries);
        Assert.Contains("Gothic3.exe", entries[0].ExecutablePath);
    }

    [Fact]
    public void Scan_Ue3BinariesSteam_DetectedAsStandalone()
    {
        string gameDir = CreateDir("SteamGame");
        string steam = CreateDir("SteamGame", "Binaries", "Steam");
        CreateExe(steam, "SteamGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Single(entries);
        Assert.Contains("SteamGame.exe", entries[0].ExecutablePath);
    }

    // ════════════════════════════════════════════════════════════════
    //  UE4-5 with multiple platforms
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_Ue4Win32_DetectedAsStandalone()
    {
        // Game/Engine/ exists + Game/GameName/Binaries/Win32/Game.exe
        CreateDir("MyGame", "Engine");
        string win32 = CreateDir("MyGame", "GameName", "Binaries", "Win32");
        CreateExe(win32, "MyGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Single(entries);
        Assert.Contains("MyGame.exe", entries[0].ExecutablePath);
    }

    [Fact]
    public void Scan_Ue4WinGDK_DetectedAsStandalone()
    {
        CreateDir("GdkGame", "Engine");
        string wingdk = CreateDir("GdkGame", "GameName", "Binaries", "WinGDK");
        CreateExe(wingdk, "GdkGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Single(entries);
        Assert.Contains("GdkGame.exe", entries[0].ExecutablePath);
    }

    // ════════════════════════════════════════════════════════════════
    //  Organization folder detection
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_OrganizationFolder_TwoGameChildren_BothDetected()
    {
        // EA/ folder with two standalone games
        string eaDir = CreateDir("EA");
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
        // Publisher/ with two UE4 games
        string pubDir = CreateDir("Publisher");
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
        // Publisher/ with GOG game + standalone game
        string pubDir = CreateDir("Publisher");
        // GOG game: has goggame.dll
        CreateDir("Publisher", "GogGame");
        File.WriteAllBytes(Path.Combine(_tempDir, "Publisher", "GogGame", "goggame.dll"), new byte[10]);
        // Standalone game: has exe
        CreateExe(CreateDir("Publisher", "StandaloneGame"), "Standalone.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Equal(2, entries.Count);
        var gog = entries.First(e => e.GameSource == GameSourceKind.Gog);
        Assert.Equal("GogGame", gog.FolderName);
    }

    // ════════════════════════════════════════════════════════════════
    //  Non-game folder skipping
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_NonGameFolders_Skipped()
    {
        // Publisher/ with Soundtrack and Manuals folders — should be skipped
        string pubDir = CreateDir("Publisher");
        CreateDir("Publisher", "Soundtrack");
        CreateDir("Publisher", "Manuals");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Empty(entries);
    }

    [Fact]
    public void Scan_NonGameFolderRedist_Skipped()
    {
        string pubDir = CreateDir("Publisher");
        CreateDir("Publisher", "_CommonRedist");
        CreateDir("Publisher", "vcredist");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Empty(entries);
    }

    // ════════════════════════════════════════════════════════════════
    //  Publisher folder pattern (dirs-only root)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_PublisherDirOnlyRoot_RecurseIntoGrandchildren()
    {
        // Publisher/ has only subdirs, no files → recurse
        // Publisher/SubDir/Game.exe → should be found
        string pubDir = CreateDir("Publisher");
        CreateExe(CreateDir("Publisher", "SubDir"), "Game.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Single(entries);
        Assert.Contains("Game.exe", entries[0].ExecutablePath);
    }

    [Fact]
    public void Scan_PublisherWithFiles_GameChildStillDetected()
    {
        // Publisher/ has files at root (not a dirs-only publisher pattern),
        // but SubDir has an exe — should still be detected.
        string pubDir = CreateDir("Publisher");
        File.WriteAllBytes(Path.Combine(pubDir, "readme.txt"), new byte[10]);
        CreateExe(CreateDir("Publisher", "SubDir"), "Game.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        // SubDir has an exe and gameSignalCount=1 → promoted as standalone
        Assert.Single(entries);
        Assert.Contains("Game.exe", entries[0].ExecutablePath);
    }

    // ════════════════════════════════════════════════════════════════
    //  Depth bounding
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_ContainerRecursion_BoundedAtDepth2()
    {
        // A/B/C/D/Game.exe — depth 3 from root, should NOT be found via container recursion
        CreateDir("A");
        CreateExe(CreateDir("A", "B", "C", "D"), "DeepGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        // A has no signals, Pass 3 runs. gameSignalCount=0 (no children with signals).
        // B has no exe at root, no UE layout. So B is not detected.
        // C/D/Game.exe is too deep for container recursion.
        Assert.Empty(entries);
    }

    // ════════════════════════════════════════════════════════════════
    //  Phase 1 — Plan 114 NonGameFolder Additions (B29, B30)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_Win32Win64Subdirs_NotTreatedAsSeparateGames()
    {
        // B29: Win32/ and Win64/ at game level are platform dirs, not separate games.
        // Container scanner should skip them via s_nonGameFolderNames.
        string publisherDir = CreateDir("Publisher");
        string win32 = CreateDir("Publisher", "Win32");
        string win64 = CreateDir("Publisher", "Win64");
        CreateExe(win32, "SomeGame.exe");
        CreateExe(win64, "SomeGame.exe");

        // Publisher/ is a container (no signal, children have exes).
        // Before fix: Win32 and Win64 each produce a game entry (2 total).
        // After fix: Win32 and Win64 are non-game folders → skipped → 0 entries.
        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.DoesNotContain(entries, e =>
            e.FolderName.Equals("Win32", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e =>
            e.FolderName.Equals("Win64", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Scan_x86x64Subdirs_NotTreatedAsSeparateGames()
    {
        // B29: x86/ and x64/ at game level are platform dirs, not separate games.
        string publisherDir = CreateDir("Publisher");
        string x86 = CreateDir("Publisher", "x86");
        string x64 = CreateDir("Publisher", "x64");
        CreateExe(x86, "SomeGame.exe");
        CreateExe(x64, "SomeGame.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.DoesNotContain(entries, e =>
            e.FolderName.Equals("x86", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e =>
            e.FolderName.Equals("x64", StringComparison.OrdinalIgnoreCase));
    }

    // ════════════════════════════════════════════════════════════════
    //  Phase 2 — BattleNet Container Detection (B26/B31)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_BlizzardContainer_Diablo3_ClassifiedAsBattleNet()
    {
        // B26: Diablo III inside Blizzard/ container with .build.info → BattleNet
        string blizzardDir = CreateDir("Blizzard");
        string diabloDir = CreateDir("Blizzard", "Diablo III");
        File.WriteAllText(Path.Combine(diabloDir, ".build.info"),
            "Header\n" +
            "eu|1|key|key2|||tpr/diablo3|host\n");
        CreateExe(diabloDir, "Diablo III.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var diablo = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Diablo", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(diablo);
        Assert.Equal(GameSourceKind.BattleNet, diablo.GameSource);
    }

    [Fact]
    public void Scan_BlizzardContainer_Warcraft_ClassifiedAsBattleNet()
    {
        // B26: World of Warcraft inside Blizzard/ container with .build.info → BattleNet
        string blizzardDir = CreateDir("Blizzard");
        string wowDir = CreateDir("Blizzard", "World of Warcraft");
        File.WriteAllText(Path.Combine(wowDir, ".build.info"),
            "Header\n" +
            "eu|1|key|key2|||agent|host\n");
        CreateExe(wowDir, "Wow.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var wow = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Warcraft", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(wow);
        Assert.Equal(GameSourceKind.BattleNet, wow.GameSource);
    }

    [Fact]
    public void Scan_Diablo3_WithBuildInfo_DetectedAsBattleNet()
    {
        // B26: Standalone Diablo III with .build.info → BattleNet (no Blizzard/ container)
        string diabloDir = CreateDir("Diablo III");
        File.WriteAllText(Path.Combine(diabloDir, ".build.info"),
            "Header\n" +
            "eu|1|key|key2|||tpr/diablo3|host\n");
        CreateExe(diabloDir, "Diablo III.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var diablo = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Diablo", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(diablo);
        Assert.Equal(GameSourceKind.BattleNet, diablo.GameSource);
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
