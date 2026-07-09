using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class MockDataIntegrationTests
{
    private static readonly string MockRoot = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "data", "mock");

    /// <summary>
    /// Scan the standalone mock root. Expected entries:
    ///   - StandaloneGameDelta  (exe + launcher exe, root default type)
    ///   - SteamEmuEpsilon      (exe + steam_api64.dll, root default type)
    ///   - AntiCheatZeta        (exe + anti-cheat, root default type)
    ///   - PublisherCollection  (sub-game with steam_appid.txt → detected as Steam override)
    ///   - _installer           (has setup.exe — included but primary exe is setup)
    ///   - redist               (has dxwebsetup.exe — included)
    /// Excluded: documentation (no exe, no markers)
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

        // Should include folders with non-game exes
        Assert.Contains(results, g =>
            g.FolderName.Equals("_installer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, g =>
            g.FolderName.Equals("redist", StringComparison.OrdinalIgnoreCase));

        // Should NOT include folders with no exe and no markers
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("documentation", StringComparison.OrdinalIgnoreCase));

        // PublisherCollection contains SubGameEta which has steam_appid.txt
        // The scanner checks the folder itself, not sub-folders. PublisherCollection
        // has no direct exe + no markers at top level. SubGameEta is a child folder.
        // PublisherCollection may be skipped or may appear depending on exe presence.
        // SubGameEta is a sub-directory and not scanned separately (scan is 1-level deep).
    }

    /// <summary>
    /// Scan the Steam common folder mock root.
    /// Expected entries:
    ///   - MockGameAlpha (GameAlpha.exe + steam_appid.txt → Steam)
    ///   - MockGameBeta  (GameBeta.exe + steam_appid.txt → Steam)
    /// </summary>
    [Fact]
    public void SteamCommonRoot_ScansSteamGames()
    {
        string root = Path.Combine(MockRoot, "steam", "steamapps", "common");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Steam);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameAlpha", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.Steam);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameBeta", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.Steam);
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
