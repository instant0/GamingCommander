using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Detection;
using GamingCommander.Migration;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

public partial class App : Application
{
    private static readonly bool StartupLoggingEnabled =
        !string.Equals(Environment.GetEnvironmentVariable("GC_STARTUP_LOGGING"), "0", StringComparison.OrdinalIgnoreCase);

    private static string? _logFilePath;

    private static string GetLogPath()
    {
        if (_logFilePath == null)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dataDir = Path.Combine(baseDir, "data");
            _logFilePath = Path.Combine(dataDir, "startup.log");
        }
        return _logFilePath;
    }

    private static void Log(string msg)
    {
        if (!StartupLoggingEnabled) return;

        try
        {
            string logPath = GetLogPath();
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            File.AppendAllText(logPath, $"[{timestamp}] {msg}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    public override void Initialize()
    {
        Log("Initialize() - Loading XAML");
        try
        {
            AvaloniaXamlLoader.Load(this);
            Log("Initialize() - XAML loaded successfully");
        }
        catch (Exception ex)
        {
            Log($"Initialize() FAILED: {ex.GetType().Name}: {ex.Message}");
            Log($"  StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Log("OnFrameworkInitializationCompleted() - START");
        Log($"  BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
        Log($"  ApplicationLifetime type: {ApplicationLifetime?.GetType().Name ?? "null"}");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                Log("Creating GamesDatabaseService...");
                var dbService = new GamesDatabaseService(GetGamesDbPath());
                Log($"  GamesDbPath: {GetGamesDbPath()}");

                Log("Creating DesignTimeGameDiscoveryService...");
                var discoveryService = new DesignTimeGameDiscoveryService();

                Log("Creating DesignTimeLibraryManager...");
                var libraryManager = new DesignTimeLibraryManager(discoveryService, dbService);

                Log("Creating JsonConfigService...");
                var configService = new JsonConfigService(GetConfigPath());
                Log($"  ConfigPath: {GetConfigPath()}");

                Log("Creating DesignTimeMigrationPlanner...");
                var migrationPlanner = new DesignTimeMigrationPlanner();

                Log("Loading config...");
                AppConfig config = configService.Load();
                Log($"  Config loaded: IsFirstRun={config.IsFirstRun}, Roots={config.LibraryRoots.Count}");

                bool needsWizard = config.IsFirstRun || config.LibraryRoots.Count == 0;
                Log($"  needsWizard: {needsWizard}");

                Log("Creating ShellViewModel...");
                var shellVm = new ShellViewModel(libraryManager, configService);
                Log("  ShellViewModel created");

                Log("Creating MainWindow...");
                var mainWindow = new MainWindow(shellVm, dbService);
                Log("  MainWindow created");

                desktop.MainWindow = mainWindow;
                Log("  desktop.MainWindow set");

                if (needsWizard)
                {
                    Log("Opening WizardWindow...");
                    var wizardWindow = new WizardWindow(configService, dbService);
                    mainWindow.Show();
                    wizardWindow.ShowDialog(mainWindow);
                    Log("  WizardWindow opened");

                    wizardWindow.Closed += (_, _) =>
                    {
                        config = configService.Load();
                        if (config.LibraryRoots.Count == 0)
                        {
                            shellVm.StatusText = "No library roots configured. Press F2 to add folders.";
                        }
                        else
                        {
                            shellVm.JumpToLibraryRoots();
                            int totalGames = config.LibraryRoots.Sum(
                                r => dbService.GetGamesForRoot(r.Path).Count);
                            shellVm.StatusText = $"Welcome — {config.LibraryRoots.Count} root(s), {totalGames} game(s) loaded.";
                        }
                    };
                }
                else
                {
                    mainWindow.Show();
                    int totalGames = config.LibraryRoots.Sum(
                        r => dbService.GetGamesForRoot(r.Path).Count);
                    shellVm.StatusText = $"Loaded {config.LibraryRoots.Count} root(s), {totalGames} game(s). Press F2 to manage.";
                }
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                Log($"  StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Log($"  InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    Log($"  Inner StackTrace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }
        else
        {
            Log("  ApplicationLifetime is NOT IClassicDesktopStyleApplicationLifetime - skipping desktop init");
        }

        Log("OnFrameworkInitializationCompleted() - END");
        base.OnFrameworkInitializationCompleted();
    }

    private static string GetConfigPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "settings.json");
    }

    private static string GetGamesDbPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "games.json");
    }
}
