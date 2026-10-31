using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

public partial class App : Application
{
    private static readonly bool StartupLoggingEnabled =
        !string.Equals(Environment.GetEnvironmentVariable("GC_STARTUP_LOGGING"), "0", StringComparison.OrdinalIgnoreCase);

    private static string? _logFilePath;

    /// <summary>Read-only launcher discovery registered at composition; the setup preview consumes it.</summary>
    internal static LauncherDiscoveryService? LauncherDiscovery { get; private set; }

    /// <summary>Per-store policy provider; one instance for the app.</summary>
    internal static IStorePolicyProvider? StorePolicies { get; private set; }

    private static string GetLogPath()
    {
        if (_logFilePath == null)
        {
            string logPath = Path.Combine(AppPaths.AppDataRoot, "startup.log");
            AppPaths.EnsureAppDataPath(logPath);
            _logFilePath = logPath;
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

                string? firstLoadWarning = null;

                // Store warnings go to startup.log; the first one also wins the status line once.
                void WarnStore(string message)
                {
                    Log($"  Store warning: {message}");
                    firstLoadWarning ??= message;
                }

                Log("Creating GamesDatabaseService...");
                var dbService = new GamesDatabaseService(AppDataPaths.GetGamesDbPath(), WarnStore);
                Log($"  GamesDbPath: {AppDataPaths.GetGamesDbPath()}");

                Log("Creating JsonConfigService...");
                var configService = new JsonConfigService(AppDataPaths.GetConfigPath(), WarnStore);
                Log($"  ConfigPath: {AppDataPaths.GetConfigPath()}");

                Log("Creating StorePolicyProvider...");
                var storePolicies = new StorePolicyProvider(configService);
                StorePolicies = storePolicies;

                Log("Creating LibrariesDatabaseService...");
                var librariesService = new LibrariesDatabaseService(AppDataPaths.GetLibrariesPath(), WarnStore);
                Log($"  LibrariesPath: {AppDataPaths.GetLibrariesPath()}");

                Log("Loading config...");
                AppConfig config = configService.Load();
                Log($"  Config loaded: IsFirstRun={config.IsFirstRun}, Libraries={librariesService.Libraries.Count}");

                // One runtime holder for the launcher discovery setting; setup updates it in place
                // and scanners read a policy snapshot. Nothing is probed while composing.
                var launcherDiscoveryContext = LauncherDiscoveryContext.FromConfig(config);

                Log("Loading blacklist...");
                var blacklist = new BlacklistLoader(baseDir).Load();
                Log($"  Blacklist loaded: {blacklist.ExeNamePatterns.Count} exe patterns, {blacklist.DirectoryPatterns.Count} dir patterns");

                Log("Creating FolderScanner with blacklist...");
                IRegistryReader registryReader = OperatingSystem.IsWindows()
                    ? new WindowsRegistryReader()
                    : null!;
                var scanner = new FolderScanner(config.HiddenFolders, blacklist, registryReader, config.ScanDepth,
                    () => launcherDiscoveryContext.Policy.AllowsRegistryFallback);

                Log("Registering launcher locators...");
                var launcherDiscovery = new LauncherDiscoveryService(
                [
                    new SteamLauncherLocator(
                        () => librariesService.Libraries.SelectMany(l => l.Folders),
                        registryReader ?? new NullRegistryReader()),
                    new EpicLauncherLocator(),
                    new GogLauncherLocator(registryReader ?? new NullRegistryReader()),
                    new UbisoftLauncherLocator(registryReader ?? new NullRegistryReader()),
                    new RockstarLauncherLocator(registryReader ?? new NullRegistryReader()),
                    new XboxLauncherLocator(),
                ]);
                LauncherDiscovery = launcherDiscovery;
                Log($"  Launcher locators registered; discovery setting: {config.LauncherDiscovery}");

                Log("Creating SteamLibraryScanner...");
                var steamPaths = librariesService.Libraries
                    .Where(l => l.Type == GameSourceKind.Steam)
                    .SelectMany(l => l.Folders);
                var steamScanner = new SteamLibraryScanner(steamPaths);
                Log($"  Steam paths: {string.Join(", ", steamPaths)}");

                Log("Creating LibraryManager...");
                var libraryManager = new LibraryManager(librariesService, dbService, scanner, steamScanner, launcherDiscoveryContext);

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
                var metadataStore = new MetadataStore(AppDataPaths.GetMetadataDbPath(), WarnStore);
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
                if (firstLoadWarning is not null)
                    shellVm.StatusText = firstLoadWarning;
                Log("  ShellViewModel created");

                Log("Creating MainWindow...");
                var mainWindow = new MainWindow(
                    shellVm, dbService, librariesService, libraryManager, configService, metadataService, onlineGate,
                    storePolicies, launcherDiscovery, launcherDiscoveryContext);
                Log("  MainWindow created");

                desktop.MainWindow = mainWindow;
                Log("  desktop.MainWindow set");

                if (needsWizard)
                {
                    Log("Opening LibrarySetupWindow (auto-open)...");
                    var setupWindow = new LibrarySetupWindow(
                        configService, dbService, libraryManager,
                        storePolicies: storePolicies, launcherDiscovery: launcherDiscovery,
                        isFirstRun: true, discoveryContext: launcherDiscoveryContext);
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
                            shellVm.StatusText = firstLoadWarning
                                ?? "No libraries configured. Press F2 to add library folders.";
                        }
                        else
                        {
                            shellVm.JumpToLibraryRoots();
                            int totalGames = librariesService.Libraries
                                .Sum(l => dbService.GetGamesForLibrary(l.Name).Count);
                            shellVm.StatusText = firstLoadWarning
                                ?? $"Welcome — {librariesService.Libraries.Count} librar{(librariesService.Libraries.Count != 1 ? "ies" : "y")}, {totalGames} game(s) loaded.";
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
                    shellVm.StatusText = firstLoadWarning
                        ?? $"Loaded {librariesService.Libraries.Count} librar{(librariesService.Libraries.Count != 1 ? "ies" : "y")}, {totalGames} game(s). Press F2 to manage.";
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

    /// <summary>Returns -1 if leftVersion &lt; rightVersion, 0 if equal, 1 if leftVersion &gt; rightVersion.</summary>
    private static int CompareVersions(string leftVersion, string rightVersion)
    {
        if (!Version.TryParse(leftVersion, out var left) || !Version.TryParse(rightVersion, out var right))
            return string.Compare(leftVersion, rightVersion, StringComparison.Ordinal);
        return left.CompareTo(right);
    }
}
