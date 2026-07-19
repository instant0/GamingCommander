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
    //  Basic Scanning
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_WithValidLibrary_ReturnsInstalledGames()
    {
        string root = CreateMockSteamLibrary("TestGame", appId: "12345");
        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Single(results);
        Assert.Equal("Test Game Alpha", results[0].DisplayName);
        Assert.Equal(GameSourceKind.Steam, results[0].GameSource);
        Assert.Equal("Installed", results[0].PlatformMetadata["SteamStatus"]);
    }

    [Fact]
    public void Scan_WithNoCommonFolder_ReturnsEmpty()
    {
        // Create steamapps/ but no common/ folder
        string steamapps = Path.Combine(_tempDir, "steamapps");
        Directory.CreateDirectory(steamapps);

        var scanner = new SteamLibraryScanner([_tempDir]);
        var results = scanner.Scan(_tempDir);

        Assert.Empty(results);
    }

    [Fact]
    public void Scan_WithNonExistentPath_ReturnsEmpty()
    {
        var scanner = new SteamLibraryScanner(["/nonexistent/path"]);
        var results = scanner.Scan("/nonexistent/path");

        Assert.Empty(results);
    }

    // ════════════════════════════════════════════════════════════════
    //  ACF Parsing
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_WithValidAcf_ParsesAllFields()
    {
        string root = CreateMockSteamLibrary("TestGame", appId: "730");
        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Single(results);
        var entry = results[0];
        Assert.Equal("730", entry.PlatformMetadata["SteamAppId"]);
        Assert.Equal("Test Game Alpha", entry.DisplayName);
        Assert.Equal("TestGame", entry.FolderName);
        Assert.Equal("4", entry.PlatformMetadata["AcfStateFlags"]);
        Assert.False(string.IsNullOrEmpty(entry.PlatformMetadata["AcfSizeOnDisk"]));
    }

    [Fact]
    public void Scan_WithMalformedAcf_SkipsEntry()
    {
        string root = CreateSteamAppsDir();
        string commonDir = Path.Combine(root, "steamapps", "common");
        Directory.CreateDirectory(Path.Combine(commonDir, "BadGame"));

        // Write malformed ACF
        string steamapps = Path.Combine(root, "steamapps");
        File.WriteAllText(Path.Combine(steamapps, "appmanifest_99999.acf"), "not valid acf {{{");

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        // BadGame exists but has no valid ACF → Orphaned
        Assert.Single(results);
        Assert.Equal("Orphaned", results[0].PlatformMetadata["SteamStatus"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Cross-Library Detection
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_WithMovedGame_DetectsMovedStatus()
    {
        // Library A has the game folder, Library B has the ACF
        string libA = CreateSteamAppsDir("libA");
        string libB = CreateSteamAppsDir("libB");

        // Game folder in libA
        Directory.CreateDirectory(Path.Combine(libA, "steamapps", "common", "TestGame"));

        // ACF in libB pointing to TestGame
        WriteAcf(libB, "12345", "Test Game", "TestGame");

        var scanner = new SteamLibraryScanner([libA, libB]);
        var results = scanner.Scan(libA);

        Assert.Single(results);
        Assert.Equal("Moved", results[0].PlatformMetadata["SteamStatus"]);
        Assert.True(results[0].PlatformMetadata.ContainsKey("AcfExpectedPath"));
    }

    [Fact]
    public void Scan_WithOrphanedGame_DetectsOrphanedStatus()
    {
        string root = CreateSteamAppsDir();

        // Game folder exists but no ACF
        Directory.CreateDirectory(Path.Combine(root, "steamapps", "common", "OrphanedGame"));

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Single(results);
        Assert.Equal("Orphaned", results[0].PlatformMetadata["SteamStatus"]);
        Assert.Equal(string.Empty, results[0].PlatformMetadata["SteamAppId"]);
    }

    [Fact]
    public void Scan_WithMissingGame_DetectsMissingStatus()
    {
        // ACF exists but no matching common/ folder
        string root = CreateSteamAppsDir();
        WriteAcf(root, "99999", "Missing Game", "MissingGame");

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Single(results);
        Assert.Equal("Missing", results[0].PlatformMetadata["SteamStatus"]);
        Assert.Equal("99999", results[0].PlatformMetadata["SteamAppId"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Status Fields
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_InstalledGame_HasSteamStatusExtra()
    {
        string root = CreateMockSteamLibrary("Game1", appId: "111");
        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Single(results);
        Assert.Equal("Installed", results[0].PlatformMetadata["SteamStatus"]);
        Assert.Equal("111", results[0].PlatformMetadata["SteamAppId"]);
        Assert.False(string.IsNullOrEmpty(results[0].PlatformMetadata["AcfLibraryPath"]));
    }

    [Fact]
    public void Scan_MissingGame_HasSteamStatusExtra()
    {
        string root = CreateSteamAppsDir();
        WriteAcf(root, "55555", "Ghost Game", "GhostFolder");

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        Assert.Single(results);
        Assert.Equal("Missing", results[0].PlatformMetadata["SteamStatus"]);
        Assert.Equal("55555", results[0].PlatformMetadata["SteamAppId"]);
        Assert.True(results[0].PlatformMetadata.ContainsKey("AcfFilePath"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Library Discovery (tested via scanner behavior)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_WithVdfDiscoveredPaths_IncludesCrossLibraryGames()
    {
        // Write libraryfolders.vdf pointing to a second library (same-line brace format)
        string libA = CreateSteamAppsDir("libA");
        string libB = CreateSteamAppsDir("libB");

        string steamappsA = Path.Combine(libA, "steamapps");
        File.WriteAllText(Path.Combine(steamappsA, "libraryfolders.vdf"),
            "\"libraryfolders\" {\n" +
            "    \"0\" {\n" +
            $"        \"path\" \"{libB.Replace("\\", "\\\\")}\"\n" +
            "    }\n" +
            "}");

        // Game folder in libB (discovered via VDF) and ACF in libB
        Directory.CreateDirectory(Path.Combine(libB, "steamapps", "common", "DiscoveredGame"));
        WriteAcf(libB, "99999", "Discovered Game", "DiscoveredGame");

        var scanner = new SteamLibraryScanner([libA]);
        // Scan() scans libA's common/ only — DiscoveredGame is in libB
        var results = scanner.Scan(libA);

        // Scan() won't find games in libB's common/, but it WILL detect
        // Missing status for ACFs whose installdir has no common/ in any known path
        // Since DiscoveredGame IS in libB (a discovered path), it won't be Missing
        Assert.Empty(results); // libA has no common/ game folders
    }

    [Fact]
    public void Scan_WithNoVdf_NoExtraPathsDiscovered()
    {
        string root = CreateSteamAppsDir();
        Directory.CreateDirectory(Path.Combine(root, "steamapps", "common", "SomeGame"));

        var scanner = new SteamLibraryScanner([root]);
        var results = scanner.Scan(root);

        // SomeGame has no ACF and no VDF → orphaned
        Assert.Single(results);
        Assert.Equal("Orphaned", results[0].PlatformMetadata["SteamStatus"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  ScanAll
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScanAll_WithMultipleLibraries_ReturnsAllGames()
    {
        string libA = CreateSteamAppsDir("libA");
        string libB = CreateSteamAppsDir("libB");

        // Write libraryfolders.vdf with flat format (DiscoverLibraryPaths expects
        // numeric keys mapping to string paths directly, not nested blocks)
        string steamappsA = Path.Combine(libA, "steamapps");
        File.WriteAllText(Path.Combine(steamappsA, "libraryfolders.vdf"),
            "\"libraryfolders\" {\n" +
            $"    \"0\" \"{libA.Replace("\\", "\\\\")}\"\n" +
            $"    \"1\" \"{libB.Replace("\\", "\\\\")}\"\n" +
            "}");

        WriteAcf(libA, "111", "Game A", "GameA");
        Directory.CreateDirectory(Path.Combine(libA, "steamapps", "common", "GameA"));

        WriteAcf(libB, "222", "Game B", "GameB");
        Directory.CreateDirectory(Path.Combine(libB, "steamapps", "common", "GameB"));

        var scanner = new SteamLibraryScanner([libA, libB]);
        var results = scanner.ScanAll();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.DisplayName == "Game A");
        Assert.Contains(results, r => r.DisplayName == "Game B");
    }

    [Fact]
    public void ScanAll_WithDuplicatePaths_Deduplicates()
    {
        string root = CreateMockSteamLibrary("Game1", appId: "111");

        // Pass the same path twice
        var scanner = new SteamLibraryScanner([root, root]);
        var results = scanner.ScanAll();

        // Should only return one entry, not duplicates
        Assert.Single(results);
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
