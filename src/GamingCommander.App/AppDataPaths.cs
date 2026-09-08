namespace GamingCommander.App;

/// <summary>
/// Centralizes the four {app-base}/data/*.json paths shared by App and MainWindow.
/// NOTE: each getter may CREATE the data directory if it does not exist
/// (filesystem side effect) — same behavior as the private helpers they replace.
/// The startup log path (startup.log) intentionally stays window-local in App.
/// </summary>
public static class AppDataPaths
{
    private static string DataDir
    {
        get
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            return dataDir;
        }
    }

    public static string GetConfigPath() => Path.Combine(DataDir, "settings.json");

    public static string GetGamesDbPath() => Path.Combine(DataDir, "games.json");

    public static string GetLibrariesPath() => Path.Combine(DataDir, "libraries.json");

    public static string GetMetadataDbPath() => Path.Combine(DataDir, "games_metadata.json");
}