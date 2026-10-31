namespace GamingCommander.App;

/// <summary>
/// Centralizes the four {app-base}/data/*.json paths shared by App and MainWindow.
/// The getters are pure: they no longer create the data directory as a side effect.
/// Creation goes through <see cref="AppPaths.EnsureAppDataPath"/> (startup log or first write).
/// The startup log path (startup.log) intentionally stays window-local in App.
/// </summary>
public static class AppDataPaths
{
    public static string GetConfigPath() => Path.Combine(AppPaths.AppDataRoot, "settings.json");

    public static string GetGamesDbPath() => Path.Combine(AppPaths.AppDataRoot, "games.json");

    public static string GetLibrariesPath() => Path.Combine(AppPaths.AppDataRoot, "libraries.json");

    public static string GetMetadataDbPath() => Path.Combine(AppPaths.AppDataRoot, "games_metadata.json");
}
