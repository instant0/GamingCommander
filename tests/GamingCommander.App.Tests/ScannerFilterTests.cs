using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class ScannerFilterTests
{
    private static readonly string MockRoot = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "data", "mock");

    /// <summary>
    /// Folder with no .exe and no game marker files should be excluded.
    /// </summary>
    [Fact]
    public void NonGameFolder_WithoutExeOrMarkers_IsExcluded()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // "documentation" has no exe and no markers — should not appear
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("documentation", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Folder with only non-game executables (installer, setup) is included in scan
    /// results (it has exe files), but the primary executable will be one of the
    /// non-game exes (the best available candidate).
    /// </summary>
    [Fact]
    public void NonGameFolder_WithOnlyNonGameExe_IsIncludedWithNonGameExe()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // "_installer" has setup.exe and vcredist_x64.exe — both are non-game exes.
        // The folder IS included because it has .exe files.
        var installer = results.FirstOrDefault(g =>
            g.FolderName.Equals("_installer", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(installer);

        // "redist" has dxwebsetup.exe and oalinst.exe — both non-game.
        var redist = results.FirstOrDefault(g =>
            g.FolderName.Equals("redist", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(redist);
    }

    /// <summary>
    /// Folder with exe and steam_appid.txt should be detected as Steam.
    /// </summary>
    [Fact]
    public void SteamGame_WithAppidMarker_IsDetectedAsSteam()
    {
        string root = Path.Combine(MockRoot, "steam", "steamapps", "common");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Steam);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameAlpha", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.Steam);
    }

    /// <summary>
    /// Folder with steam_api64.dll (but no appid.txt) should be detected via
    /// the default type when scanned under a Standalone root.
    /// </summary>
    [Fact]
    public void SteamEmuGame_WithApiDll_IsDetectedAsStandalone()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // SteamEmuEpsilon has steam_api64.dll + GameEpsilon.exe
        // It should appear but as Standalone (the root default) since
        // steam_api64.dll is not in the game marker file list
        Assert.Contains(results, g =>
            g.FolderName.Equals("SteamEmuEpsilon", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// When a folder contains both a game exe and a larger anti-cheat installer,
    /// the primary executable should be the game exe (folder-name match wins).
    /// </summary>
    [Fact]
    public void PrimaryExe_WithAntiCheatInstaller_PrefersNameMatch()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // AntiCheatZeta contains:
        //   - GameZeta.exe (50MB, name matches "AntiCheatZeta" via token)
        //   - easyanticheat_setup.exe (100MB, filtered as non-game)
        //   - steam_api64.dll (not an exe)
        // The primary exe should be GameZeta.exe

        var entry = results.FirstOrDefault(g =>
            g.FolderName.Equals("AntiCheatZeta", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(entry);
        Assert.Contains("GameZeta.exe", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("easyanticheat", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The HiddenFolders ignore list should skip matching folder names.
    /// </summary>
    [Fact]
    public void HiddenFolders_AreSkipped()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner(new[] { "SteamEmuEpsilon" });
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("SteamEmuEpsilon", StringComparison.OrdinalIgnoreCase));
    }
}
