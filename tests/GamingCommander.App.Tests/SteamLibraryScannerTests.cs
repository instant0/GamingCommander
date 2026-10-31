using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class SteamLibraryScannerTests : IDisposable
{
    private readonly string _tempDir;

    public SteamLibraryScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SteamScannerTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  Basic scanning
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_WithValidLibrary_ReturnsInstalledGames()
    {
        string root = CreateMockSteamLibrary("TestGame", appId: "12345");
        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        var entry = Assert.Single(results);
        Assert.Equal("Test Game Alpha", entry.DisplayName);
        Assert.Equal(GameSourceKind.Steam, entry.GameSource);
        Assert.Equal("Installed", entry.Acf.Status);
        Assert.False(string.IsNullOrEmpty(entry.Acf.LibraryPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scan_WithoutLibraryPath_ReturnsEmpty(bool pathMissing)
    {
        string scanned = pathMissing ? Path.Combine(_tempDir, "missing") : _tempDir;
        if (!pathMissing)
            Directory.CreateDirectory(Path.Combine(_tempDir, "steamapps")); // no common/

        var scanner = new SteamLibraryScanner([scanned]);
        var results = scanner.Scan(scanned);

        Assert.Empty(results);
    }

    // ════════════════════════════════════════════════════════════════
    //  ACF parsing and exe discovery
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Binaries/Win64", "UeGame-Win64-Shipping.exe", "480")]
    public void Scan_FindsExeInKnownSubfolder(string relativeDir, string exeName, string appId)
    {
        string root = CreateMockSteamLibrary("Game", appId: appId);
        string exeDir = Path.Combine(root, "steamapps", "common", "Game", relativeDir);
        Directory.CreateDirectory(exeDir);
        File.WriteAllBytes(Path.Combine(exeDir, exeName), new byte[200 * 1024]);

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        var entry = Assert.Single(results);
        Assert.Contains(exeName, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(relativeDir.Split('/')[0], entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(appId, entry.Store.SteamAppId);
        Assert.Contains($"steam://rungameid/{appId}", entry.CommandLineArguments);
    }

    [Fact]
    public void Scan_WithValidAcf_ParsesAllFields()
    {
        string root = CreateMockSteamLibrary("TestGame", appId: "730");
        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        var entry = Assert.Single(results);
        Assert.Equal("730", entry.Store.SteamAppId);
        Assert.Equal("Test Game Alpha", entry.DisplayName);
        Assert.Equal("TestGame", entry.FolderName);
        Assert.Equal("4", entry.Acf.StateFlags);
        Assert.False(string.IsNullOrEmpty(entry.Acf.SizeOnDisk));
    }

    [Fact]
    public void Scan_WithMalformedAcf_SkipsEntry()
    {
        string root = CreateSteamAppsDir();
        string commonDir = Path.Combine(root, "steamapps", "common");
        Directory.CreateDirectory(Path.Combine(commonDir, "BadGame"));

        string steamapps = Path.Combine(root, "steamapps");
        File.WriteAllText(Path.Combine(steamapps, "appmanifest_99999.acf"), "not valid acf {{{");

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        // BadGame exists but has no valid ACF → Orphaned
        var entry = Assert.Single(results);
        Assert.Equal("Orphaned", entry.Acf.Status);
    }

    // ════════════════════════════════════════════════════════════════
    //  Cross-library detection
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_WithMovedGame_DetectsMovedStatus()
    {
        // Library A has the game folder, Library B has the ACF
        string libA = CreateSteamAppsDir("libA");
        string libB = CreateSteamAppsDir("libB");

        Directory.CreateDirectory(Path.Combine(libA, "steamapps", "common", "TestGame"));
        WriteAcf(libB, "12345", "Test Game", "TestGame");

        var scanner = new SteamLibraryScanner([libA, libB]);
        var results = scanner.Scan(libA);

        var entry = Assert.Single(results);
        Assert.Equal("Moved", entry.Acf.Status);
        Assert.True(entry.Acf.ExpectedPath.Length > 0);
    }

    [Fact]
    public void Scan_WithOrphanedGame_DetectsOrphanedStatus()
    {
        string root = CreateSteamAppsDir();
        Directory.CreateDirectory(Path.Combine(root, "steamapps", "common", "OrphanedGame"));

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        var entry = Assert.Single(results);
        Assert.Equal("Orphaned", entry.Acf.Status);
        Assert.Equal(string.Empty, entry.Store.SteamAppId);
    }

    [Fact]
    public void Scan_MissingAcf_OnlyOnOwningLibrary()
    {
        string libA = CreateSteamAppsDir("libA");
        string libB = CreateSteamAppsDir("libB");
        WriteAcf(libA, "99999", "Ghost", "GhostFolder");
        Directory.CreateDirectory(Path.Combine(libB, "steamapps", "common", "OtherGame"));
        WriteAcf(libB, "1", "Other", "OtherGame");

        var scanner = new SteamLibraryScanner([libA, libB]);
        var fromA = scanner.Scan(libA);
        var fromB = scanner.Scan(libB);

        Assert.Contains(fromA, g => g.Acf.Status == "Missing");
        Assert.DoesNotContain(fromB, g => g.Acf.Status == "Missing");
    }

    [Fact]
    public void Scan_WithMissingGame_DetectsMissingStatus()
    {
        // ACF exists but no matching common/ folder.
        string root = CreateSteamAppsDir();
        WriteAcf(root, "99999", "Missing Game", "MissingGame");

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        var entry = Assert.Single(results);
        Assert.Equal("Missing", entry.Acf.Status);
        Assert.Equal("99999", entry.Store.SteamAppId);
        Assert.True(entry.Acf.FilePath.Length > 0);
    }

    [Fact]
    public void Scan_MissingVdf_UsesConfiguredRootsForCrossReference()
    {
        // No libraryfolders.vdf exists. The ACF lives in the scanned root, but its game
        // folder exists only in the second CONFIGURED root. The configured root must
        // count as a known library, so the game is present rather than Missing.
        string scannedRoot = CreateSteamAppsDir("noVdfScanned");
        string configuredRoot = CreateSteamAppsDir("noVdfConfigured");
        WriteAcf(scannedRoot, "77777", "Configured Game", "ConfiguredGame");
        Directory.CreateDirectory(
            Path.Combine(configuredRoot, "steamapps", "common", "ConfiguredGame"));

        var scanner = new SteamLibraryScanner([scannedRoot, configuredRoot]);
        var results = scanner.Scan(scannedRoot);

        Assert.Empty(results);
        Assert.DoesNotContain(results, g => g.Acf.Status == "Missing");
    }

    // ════════════════════════════════════════════════════════════════
    //  Bug 10 — Steam-internal common/ folders are not games
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Steam Controller Configs")]
    public void Scan_WithSteamInternalFolder_SkipsOrphanedEntry(string folderName)
    {
        string root = CreateSteamAppsDir();
        Directory.CreateDirectory(Path.Combine(root, "steamapps", "common", folderName));

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Empty(results);
    }

    [Fact]
    public void Scan_WithGameNamedSteam_StillDetected()
    {
        // Guard: installdirs are arbitrary — a game legitimately titled "Steam"
        // must be detected, not skipped by the internal-folder filter.
        string root = CreateMockSteamLibrary("Steam", appId: "777");

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        var entry = Assert.Single(results);
        Assert.Equal("Installed", entry.Acf.Status);
        Assert.Equal("Steam", entry.FolderName);
    }

    // ════════════════════════════════════════════════════════════════
    //  P0 — Identity contract (physical-root-based IDs)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_SameInstalldirInTwoLibraries_KeepsBothAcfsAndAppIds()
    {
        // Two libraries with steamapps/common/Foo and different appids. The ACF map
        // is keyed by full install path, so neither game inherits the other's appid
        // and neither is reported as Moved. IDs are physical-root based, so the
        // identical folder name in two roots must yield distinct IDs.
        string libA = CreateMockSteamLibrary("Foo", appId: "100");
        string libB = CreateMockSteamLibrary("Foo", appId: "200");

        var scanner = new SteamLibraryScanner([libA, libB]);
        var results = scanner.Scan(libA).Concat(scanner.Scan(libB)).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, g => Assert.Equal("Installed", g.Acf.Status));
        Assert.DoesNotContain(results, g => g.Acf.Status == "Moved");

        GameEntry entryA = results.Single(g =>
            g.FolderPath == Path.Combine(libA, "steamapps", "common", "Foo"));
        GameEntry entryB = results.Single(g =>
            g.FolderPath == Path.Combine(libB, "steamapps", "common", "Foo"));
        Assert.Equal("100", entryA.Store.SteamAppId);
        Assert.Equal("200", entryB.Store.SteamAppId);
        Assert.NotEqual(entryA.Id, entryB.Id);
    }

    [Fact]
    public void Scan_AmbiguousCrossLibraryName_YieldsOrphaned()
    {
        // Two ACFs in two libraries both say installdir "Foo", but neither folder
        // exists. A third library has a folder "Foo" with no ACF. The name alone
        // cannot say which ACF belongs to it, so the folder must stay Orphaned.
        string libA = CreateSteamAppsDir("ambigA");
        string libB = CreateSteamAppsDir("ambigB");
        string libC = CreateSteamAppsDir("ambigC");
        WriteAcf(libA, "111", "Foo A", "Foo");
        WriteAcf(libB, "222", "Foo B", "Foo");
        Directory.CreateDirectory(Path.Combine(libC, "steamapps", "common", "Foo"));

        var scanner = new SteamLibraryScanner([libA, libB, libC]);
        var results = scanner.Scan(libC);

        var entry = Assert.Single(results);
        Assert.Equal("Orphaned", entry.Acf.Status);
        Assert.Equal(string.Empty, entry.Store.SteamAppId);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a mock Steam library with a single game.
    /// </summary>
    private string CreateMockSteamLibrary(string gameFolderName, string appId = "12345")
    {
        string root = Path.Combine(_tempDir, "steam_lib_" + Guid.NewGuid().ToString("N")[..6]);
        CreateSteamAppsDir(root);
        WriteAcf(root, appId, "Test Game Alpha", gameFolderName);
        Directory.CreateDirectory(Path.Combine(root, "steamapps", "common", gameFolderName));
        return root;
    }

    /// <summary>
    /// Creates a mock steamapps directory structure.
    /// </summary>
    private string CreateSteamAppsDir(string? subDir = null)
    {
        string root = subDir != null
            ? Path.Combine(_tempDir, subDir)
            : Path.Combine(_tempDir, "steam_lib_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(Path.Combine(root, "steamapps", "common"));
        return root;
    }

    /// <summary>
    /// Writes a mock ACF file for a Steam game.
    /// Note: VDF parser requires `{` on the same line as the key.
    /// </summary>
    private static void WriteAcf(string libraryRoot, string appId, string name, string installdir)
    {
        string acfContent =
            "\"AppState\" {\n" +
            $"    \"appid\"         \"{appId}\"\n" +
            $"    \"name\"          \"{name}\"\n" +
            $"    \"installdir\"    \"{installdir}\"\n" +
            $"    \"StateFlags\"    \"4\"\n" +
            $"    \"LastUpdated\"   \"1700000000\"\n" +
            $"    \"SizeOnDisk\"    \"5000000000\"\n" +
            $"    \"buildid\"       \"12345678\"\n" +
            "}";

        string steamapps = Path.Combine(libraryRoot, "steamapps");
        Directory.CreateDirectory(steamapps);
        File.WriteAllText(Path.Combine(steamapps, $"appmanifest_{appId}.acf"), acfContent);
    }
}
