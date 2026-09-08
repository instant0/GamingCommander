using GamingCommander.App.Services;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for the Steam registry + libraryfolders.vdf bootstrap (Plan 123 §8.3,
/// confirmed design 2026-09-07).
///   - Chain: HKLM\SOFTWARE\Valve\Steam\InstallPath → libraryfolders.vdf → all libraries
///   - vdf is a PATH LOCATOR ONLY (apps blocks ignored; content = ACF+folder scan)
///   - CanAddSteamLibraries gating (Q4: hidden when Steam not installed)
/// </summary>
public sealed class SteamBootstrapTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MockRegistryReader _registry;

    public SteamBootstrapTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SteamBootstrap_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _registry = new MockRegistryReader(
            "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam]\r\n" +
            "\"InstallPath\"=str(2):\"P:\\Program Files (x86)\\Steam\"\r\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    /// <summary>Real libraryfolders.vdf from the user's machine (testdata/samples).</summary>
    private static string RealVdfPath =>
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "testdata", "samples", "libraryfolders.vdf"));

    /// <summary>
    /// Copies the REAL libraryfolders.vdf (P:/E:/D: over 3 libraries) into
    /// &lt;temp&gt;\steamapps and points the registry InstallPath at that temp dir.
    /// The real vdf CONTENT is what's under test.
    /// </summary>
    private SteamInstallPathLocator LocatorFromRealVdf()
    {
        string steamapps = Path.Combine(_tempDir, "steamapps");
        Directory.CreateDirectory(steamapps);
        File.Copy(RealVdfPath, Path.Combine(steamapps, "libraryfolders.vdf"), true);
        var registry = new MockRegistryReader(
            "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam]\r\n" +
            $"\"InstallPath\"=str(2):\"{_tempDir}\"\r\n");
        return new SteamInstallPathLocator(registry);
    }

    // ════════════════════════════════════════════════════════════════
    //  Locator chain
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindInstallPath_ReadsRegistryInstallPath()
    {
        var locator = new SteamInstallPathLocator(_registry);
        Assert.Equal(@"P:\Program Files (x86)\Steam", locator.FindInstallPath());
    }

    [Fact]
    public void FindInstallPath_MissingRegistryKey_ReturnsNull()
    {
        var empty = new MockRegistryReader("");
        var locator = new SteamInstallPathLocator(empty);
        Assert.Null(locator.FindInstallPath());
    }

    [Fact]
    public void IsSteamAvailable_FalseWhenNoRegistryKey()
    {
        var empty = new MockRegistryReader("");
        var locator = new SteamInstallPathLocator(empty);
        Assert.False(locator.IsSteamAvailable);
    }

    [Fact]
    public void FindAllLibraries_InstallPathPlusVdfPaths()
    {
        // Uses the REAL libraryfolders.vdf (3 libraries over P:, E:, D:).
        var locator = LocatorFromRealVdf();

        var libs = locator.FindAllLibraries();

        Assert.Contains(_tempDir, libs);                          // install path
        Assert.Contains(@"P:\Program Files (x86)\Steam", libs);   // library "0"
        Assert.Contains(@"E:\SteamLibrary", libs);                // library "1"
        Assert.Contains(@"D:\SteamLibrary", libs);                // library "2"
        Assert.Equal(4, libs.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  Authority rule: vdf is a path locator ONLY
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindAllLibraries_AppsBlocksDoNotAddLibraries()
    {
        // The real file has multiple "apps" blocks (appid → size). They must NOT be
        // read as library paths — only the three "path" entries count.
        var locator = LocatorFromRealVdf();

        var libs = locator.FindAllLibraries();

        // install path + exactly the 3 path entries = 4 (no appid or app-size tokens)
        Assert.Equal(4, libs.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  Gating (Q4)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void IsSteamAvailable_TrueWhenInstallAndVdfPresent()
    {
        var locator = LocatorFromRealVdf();
        Assert.True(locator.IsSteamAvailable);
    }
}