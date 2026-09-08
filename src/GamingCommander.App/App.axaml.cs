using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
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
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                Log("Creating GamesDatabaseService...");
                var dbService = new GamesDatabaseService(GetGamesDbPath());
                Log($"  GamesDbPath: {GetGamesDbPath()}");

                Log("Creating JsonConfigService...");
                var configService = new JsonConfigService(GetConfigPath());
                Log($"  ConfigPath: {GetConfigPath()}");

                Log("Creating LibrariesDatabaseService...");
                var librariesService = new LibrariesDatabaseService(GetLibrariesPath());
                Log($"  LibrariesPath: {GetLibrariesPath()}");

                Log("Loading config...");
                AppConfig config = configService.Load();
                Log($"  Config loaded: IsFirstRun={config.IsFirstRun}, Libraries={librariesService.Libraries.Count}");

                Log("Loading blacklist...");
                var blacklist = new BlacklistLoader(baseDir).Load();
                Log($"  Blacklist loaded: {blacklist.ExeNamePatterns.Count} exe patterns, {blacklist.DirectoryPatterns.Count} dir patterns");

                Log("Creating FolderScanner with blacklist...");
                IRegistryReader registryReader = OperatingSystem.IsWindows()
                    ? new WindowsRegistryReader()
                    : null!;
                var scanner = new FolderScanner(config.HiddenFolders, blacklist, registryReader);

                Log("Creating SteamLibraryScanner...");
                var steamPaths = librariesService.Libraries
                    .Where(l => l.Type == GameSourceKind.Steam)
                    .SelectMany(l => l.Folders);
                var steamScanner = new SteamLibraryScanner(steamPaths);
                Log($"  Steam paths: {string.Join(", ", steamPaths)}");

                Log("Creating LibraryManager...");
                var libraryManager = new LibraryManager(librariesService, dbService, scanner, steamScanner);

                Log("Creating DesignTimeMigrationPlanner...");
                var migrationPlanner = new DesignTimeMigrationPlanner();

                string currentVersion = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
                bool isNewerVersion = config.LastSeenVersion is not null
                    && CompareVersions(config.LastSeenVersion, currentVersion) < 0;
                bool needsWizard = config.IsFirstRun
                    || config.LastSeenVersion is null
                    || isNewerVersion
                    || librariesService.Libraries.Count == 0;
                Log($"  CurrentVersion: {currentVersion}, LastSeen: {config.LastSeenVersion ?? "(null)"}, needsWizard: {needsWizard}, isNewerVersion: {isNewerVersion}");

                Log("Creating ShellViewModel...");
                string tagColorsPath = Path.Combine(AppContext.BaseDirectory, "data", "tag_colors.json");
                var tagColorService = new TagColorService(tagColorsPath);
                var metadataStore = new MetadataStore(GetMetadataDbPath());
                var steamLookup = new SteamStoreLookup();
                var pcgwLookup = new PcgwLookup();
                var onlineGate = new MetadataOnlineGate(configService);
                if (config.EnableOnlineMetadata)
                    onlineGate.SetChecking();
                else
                    onlineGate.SetDisabled();
                steamLookup.Online = onlineGate;
                pcgwLookup.Online = onlineGate;
                var metadataService = new MetadataService(metadataStore, configService, steamLookup, pcgwLookup, onlineGate);
                var shellVm = new ShellViewModel(libraryManager, configService, tagColorService, metadataStore);
                shellVm.LookupStatusText = onlineGate.StatusLabel;
                Log("  ShellViewModel created");

                Log("Creating MainWindow...");
                var mainWindow = new MainWindow(shellVm, dbService, librariesService, metadataService, onlineGate);
                Log("  MainWindow created");

                desktop.MainWindow = mainWindow;
                Log("  desktop.MainWindow set");

                if (needsWizard)
                {
                    Log("Opening LibrarySetupWindow (auto-open)...");
                    var setupWindow = new LibrarySetupWindow(configService, dbService, libraryManager, isFirstRun: true);
                    mainWindow.Show();
                    setupWindow.ShowDialog(mainWindow);
                    Log("  LibrarySetupWindow opened");

                    setupWindow.Closed += (_, _) =>
                    {
                        config = configService.Load();
                        config = config with { LastSeenVersion = currentVersion };
                        configService.Save(config);

                        if (librariesService.Libraries.Count == 0)
                        {
                            shellVm.StatusText = "No libraries configured. Press F2 to add library folders.";
                        }
                        else
                        {
                            shellVm.JumpToLibraryRoots();
                            int totalGames = librariesService.Libraries
                                .Sum(l => dbService.GetGamesForLibrary(l.Name).Count);
                            shellVm.StatusText = $"Welcome — {librariesService.Libraries.Count} librar{(librariesService.Libraries.Count != 1 ? "ies" : "y")}, {totalGames} game(s) loaded.";
                        }

                        _ = mainWindow.SyncOnlineGateFromConfigAsync();
                    };
                }
                else
                {
                    // Stamp version so we don't re-wizard for this build
                    if (!string.Equals(config.LastSeenVersion, currentVersion, StringComparison.Ordinal))
                    {
                        config = config with { LastSeenVersion = currentVersion };
                        configService.Save(config);
                    }

                    mainWindow.Show();
                    int totalGames = librariesService.Libraries
                        .Sum(l => dbService.GetGamesForLibrary(l.Name).Count);
                    shellVm.StatusText = $"Loaded {librariesService.Libraries.Count} librar{(librariesService.Libraries.Count != 1 ? "ies" : "y")}, {totalGames} game(s). Press F2 to manage.";
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

    private static string GetLibrariesPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "libraries.json");
    }

    private static string GetMetadataDbPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "games_metadata.json");
    }

    /// <summary>Returns -1 if leftVersion &lt; rightVersion, 0 if equal, 1 if leftVersion &gt; rightVersion.</summary>
    private static int CompareVersions(string leftVersion, string rightVersion)
    {
        if (!Version.TryParse(leftVersion, out var left) || !Version.TryParse(rightVersion, out var right))
            return string.Compare(leftVersion, rightVersion, StringComparison.Ordinal);
        return left.CompareTo(right);
    }
}
