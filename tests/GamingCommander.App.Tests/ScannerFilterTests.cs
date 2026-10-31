using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Scanner-level noise filtering: hidden folders, JSON-blacklisted exe names,
/// noise directories, Steam library location, primary-exe preference, and the
/// nested-Steam exclusion (Bug 13b). Non-game/no-marker skips are owned by
/// FolderScannerContainerTests.
/// </summary>
public sealed class ScannerFilterTests
{
    private static readonly string MockRoot = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "testdata", "mock");

    [Fact]
    public void PrimaryExe_WithAntiCheatInstaller_PrefersNameMatch()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // AntiCheatZeta contains GameZeta.exe (name match) and a larger
        // easyanticheat_setup.exe (filtered as non-game noise).
        var entry = results.FirstOrDefault(g =>
            g.FolderName.Equals("AntiCheatZeta", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(entry);
        Assert.Contains("GameZeta.exe", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("easyanticheat", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HiddenFolders_AreSkipped()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner(new[] { "SteamEmuEpsilon" });
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("SteamEmuEpsilon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SteamGame_UnderLibrary_IsDetectedAsSteam()
    {
        // A folder under steamapps/common is Steam by location even when its only
        // signal is steam_appid.txt.
        string root = Path.Combine(MockRoot, "steam", "steamapps", "common");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Steam);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameAlpha", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.Steam);
    }

    /// <summary>
    /// Bug 5: the instance noise check must see JSON-blacklisted exe patterns.
    /// One noise-exe test owns that branch (positive and negative in the same tree).
    /// </summary>
    [Fact]
    public void Scan_FolderWithJsonBlacklistedNoise_IsExcluded()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("BlenderProject/blender.exe", new byte[1]);
        temp.CreateFile("ValidGame/ValidGame.exe", new byte[1]);

        var blacklist = new BlacklistData(
            ExeNamePatterns: ["blender", "python", "scummvm", "server", "editor"],
            TieredExePatterns: [],
            DirectoryPatterns: [],
            PeMetadataPatterns: [],
            PcgwTitleNoise: []);
        var scanner = new FolderScanner([], blacklist);

        var results = scanner.Scan(temp.Root, GameSourceKind.Standalone);

        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("BlenderProject", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("ValidGame", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Scan_NoiseDirectory_IsExcluded()
    {
        // B24: the ARC store/launcher directory is noise, not a game.
        using var temp = new TempDirectory();
        temp.CreateFile("arc/arc.exe", new byte[1]);
        temp.CreateFile("ValidGame/ValidGame.exe", new byte[1]);

        var scanner = new FolderScanner();
        var results = scanner.Scan(temp.Root, GameSourceKind.Standalone);

        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("arc", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("ValidGame", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Bug 13b: a child that IS a Steam library must not be emitted as a Standalone
    /// game even without a "steam" folder name. Steam games are found by adding the
    /// path as a Steam library root instead.
    /// </summary>
    [Fact]
    public void Scan_LibraryRootChildFolder_IsExcludedFromStandalone()
    {
        using var temp = new TempDirectory();
        temp.CreateDirectory("Games", "steamapps", "common", "SomeSteamGame");
        temp.CreateFile("Games/SomeGame.exe", new byte[1]);

        var scanner = new FolderScanner();
        var results = scanner.Scan(temp.Root, GameSourceKind.Standalone);

        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("Games", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Bug 13b: the Steam application folder itself (name "steam") is also excluded.
    /// </summary>
    [Fact]
    public void Scan_SteamApplicationFolder_IsExcludedFromStandalone()
    {
        using var temp = new TempDirectory();
        temp.CreateDirectory("Steam", "steamapps", "common", "SomeSteamGame");
        temp.CreateFile("Steam/steam.exe", new byte[1]);
        temp.CreateFile("ValidGame/ValidGame.exe", new byte[1]);

        var scanner = new FolderScanner();
        var results = scanner.Scan(temp.Root, GameSourceKind.Standalone);

        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("Steam", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("ValidGame", StringComparison.OrdinalIgnoreCase));
    }
}
