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
    /// Folder with only non-game executables (installer, setup) is correctly
    /// excluded from scan results — a folder with only noise executables
    /// (setup.exe, vcredist_x64.exe) is not a game.
    /// </summary>
    [Fact]
    public void NonGameFolder_WithOnlyNoiseExe_IsExcluded()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // "_installer" has only noise exes (setup.exe, vcredist_x64.exe)
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("_installer", StringComparison.OrdinalIgnoreCase));

        // "redist" has only noise exes (dxwebsetup.exe, oalinst.exe)
        Assert.DoesNotContain(results, g =>
            g.FolderName.Equals("redist", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Folder with exe and steam_appid.txt should be detected as SteamEmu
    /// by FolderScanner. (Real Steam library detection is structural via
    /// SteamLibraryScanner — steam_appid.txt alone is NOT a definitive Steam
    /// signal; many standalone games include it.)
    /// </summary>
    [Fact]
    public void SteamGame_WithAppidOnly_IsDetectedAsSteamEmu()
    {
        string root = Path.Combine(MockRoot, "steam", "steamapps", "common");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Steam);

        Assert.Contains(results, g =>
            g.FolderName.Equals("MockGameAlpha", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);
    }

    /// <summary>
    /// Folder with steam_api64.dll at root (outside Steam library path)
    /// should be detected as SteamEmu.
    /// </summary>
    [Fact]
    public void SteamEmuGame_WithApiDll_IsDetectedAsSteamEmu()
    {
        string root = Path.Combine(MockRoot, "standalone");
        var scanner = new FolderScanner();
        var results = scanner.Scan(root, GameSourceKind.Standalone);

        // SteamEmuEpsilon has steam_api64.dll + GameEpsilon.exe
        // steam_api64.dll at root outside Steam library = Steam Emulator
        Assert.Contains(results, g =>
            g.FolderName.Equals("SteamEmuEpsilon", StringComparison.OrdinalIgnoreCase)
            && g.GameSource == GameSourceKind.SteamEmu);
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
