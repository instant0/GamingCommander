namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for the application-data directory guard: creation is allowed only under
/// <see cref="AppPaths.AppDataRoot"/>; outside paths are refused and create nothing.
/// </summary>
public sealed class AppPathsTests
{
    [Fact]
    public void AppDataRoot_IsDataUnderBaseDirectory()
    {
        Assert.Equal(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"),
            AppPaths.AppDataRoot);
    }

    [Fact]
    public void EnsureAppDataPath_OutsideRoot_ThrowsAndCreatesNothing()
    {
        string outsideRoot = Path.Combine(
            Path.GetTempPath(),
            "AppPathsTests_" + Guid.NewGuid().ToString("N")[..8]);
        string target = Path.Combine(outsideRoot, "nested", "x.json");

        try
        {
            Assert.Throws<InvalidOperationException>(() => AppPaths.EnsureAppDataPath(target));
            Assert.False(Directory.Exists(outsideRoot));
        }
        finally
        {
            if (Directory.Exists(outsideRoot))
                Directory.Delete(outsideRoot, true);
        }
    }

    [Fact]
    public void EnsureAppDataPath_InsideRoot_CreatesNestedParent()
    {
        string nested = Path.Combine(
            AppPaths.AppDataRoot,
            "AppPathsTests_" + Guid.NewGuid().ToString("N")[..8]);
        string target = Path.Combine(nested, "x.json");

        try
        {
            string returned = AppPaths.EnsureAppDataPath(target);

            Assert.Equal(target, returned);
            Assert.True(Directory.Exists(nested));
        }
        finally
        {
            if (Directory.Exists(nested))
                Directory.Delete(nested, true);
        }
    }

    [Fact]
    public void AppDataPaths_ReturnPathsUnderAppDataRoot()
    {
        Assert.Equal(Path.Combine(AppPaths.AppDataRoot, "settings.json"), AppDataPaths.GetConfigPath());
        Assert.Equal(Path.Combine(AppPaths.AppDataRoot, "games.json"), AppDataPaths.GetGamesDbPath());
        Assert.Equal(Path.Combine(AppPaths.AppDataRoot, "libraries.json"), AppDataPaths.GetLibrariesPath());
        Assert.Equal(Path.Combine(AppPaths.AppDataRoot, "games_metadata.json"), AppDataPaths.GetMetadataDbPath());
    }
}
