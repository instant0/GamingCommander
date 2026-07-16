using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class MockDataIntegrationTests
{
    private static readonly string MockRoot = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "data", "mock");

    /// <summary>
    /// Scan the standalone mock root. Expected entries:
    ///   - StandaloneGameDelta  (exe + launcher exe → Standalone, root default type)
    ///   - SteamEmuEpsilon      (exe + steam_api64.dll → SteamEmu)
    ///   - AntiCheatZeta        (exe + steam_api64.dll + anti-cheat → SteamEmu)
    ///   - PublisherCollection  (container with SubGameEta child that has steam_appid.txt → Steam override)
    /// Excluded:
    ///   - _installer           (only noise exes: setup.exe, vcredist_x64.exe)
    ///   - redist               (only noise exes: dxwebsetup.exe, oalinst.exe)
    ///   - documentation        (no exe, no markers)
    /// </summary>
    [Fact]
    public void StandaloneRoot_ScansCorrectGames()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // Should include game folders
        Assert.Contains(results, g =>
            g.FolderName.Equals("StandaloneGameDelta", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("SteamEmuEpsilon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("AntiCheatZeta", StringComparison.OrdinalIgnoreCase));

        // Should NOT include folders with only noise executables
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("_installer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("redist", StringComparison.OrdinalIgnoreCase));

        // Should NOT include folders with no exe and no markers
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("documentation", StringComparison.OrdinalIgnoreCase));

        // SteamEmuEpsilon should be detected as SteamEmu (has steam_api64.dll at root)
        Assert.Contains(results, g =>
            g.FolderName.Equals("SteamEmuEpsilon", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);

        // AntiCheatZeta should be detected as SteamEmu (has steam_api64.dll at root)
        Assert.Contains(results, g =>
            g.FolderName.Equals("AntiCheatZeta", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);

        // StandaloneGameDelta should be Standalone (no signals, root default type)
        Assert.Contains(results, g =>
            g.FolderName.Equals("StandaloneGameDelta", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.Standalone);

        // PublisherCollection is a container: no signals itself, but immediate child
        // SubGameEta has steam_appid.txt (classified as SteamEmu by FolderScanner).
        // SubGameEta should appear as SteamEmu.
        Assert.Contains(results, g =>
            g.FolderName.Equals("SubGameEta", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("SubGameEta", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);
    }

    /// <summary>
    /// Scan the Steam common folder mock root.
    /// Expected entries:
    ///   - MockGameAlpha (GameAlpha.exe + steam_appid.txt → SteamEmu via FolderScanner)
    ///   - MockGameBeta  (GameBeta.exe + steam_appid.txt → SteamEmu via FolderScanner)
    /// Note: In production, Steam library paths like this one are handled by
    /// SteamLibraryScanner (structural detection), not FolderScanner.
    /// </summary>
    [Fact]
    public void SteamCommonRoot_ScansAsSteamEmuViaFolderScanner()
    {
        string root = Path.Combine(MockRoot, "steam", "steamapps", "common");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Steam);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameAlpha", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameBeta", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);
    }

    /// <summary>
    /// All scanned entries should have a non-empty Id.
    /// </summary>
    [Fact]
    public void AllScannedGames_HaveValidIds()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        Assert.All(results, g => Assert.False(string.IsNullOrWhiteSpace(g.Id)));
    }

    /// <summary>
    /// Entries should have a LastScan timestamp set.
    /// </summary>
    [Fact]
    public void AllScannedGames_HaveScanTimestamp()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        Assert.All(results, g => Assert.NotEqual(default, g.LastScanned));
    }

    /// <summary>
    /// Scanning the same folder twice with the same scanner should
    /// return the same folder count (deterministic).
    /// </summary>
    [Fact]
    public void Scan_IsDeterministic()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var first = scanner.Scan(root, GameSourceKind.Standalone);
        var second = scanner.Scan(root, GameSourceKind.Standalone);

        var firstNames = first.Select(g => g.FolderName).OrderBy(n => n).ToList();
        var secondNames = second.Select(g => g.FolderName).OrderBy(n => n).ToList();

        Assert.Equal(firstNames, secondNames);
    }
}
