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
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dbService = new GamesDatabaseService(GetGamesDbPath());
            var libraryManager = new DesignTimeLibraryManager(
                new DesignTimeGameDiscoveryService(),
                dbService);
            var configService = new JsonConfigService(GetConfigPath());
            var migrationPlanner = new DesignTimeMigrationPlanner();

            AppConfig config = configService.Load();
            bool needsWizard = config.IsFirstRun || config.LibraryRoots.Count == 0;

            var shellVm = new ShellViewModel(libraryManager, configService);
            var mainWindow = new MainWindow(shellVm, dbService);
            desktop.MainWindow = mainWindow;

            if (needsWizard)
            {
                var wizardWindow = new WizardWindow(configService, dbService);
                wizardWindow.ShowDialog(mainWindow);

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
                int totalGames = config.LibraryRoots.Sum(
                    r => dbService.GetGamesForRoot(r.Path).Count);
                shellVm.StatusText = $"Loaded {config.LibraryRoots.Count} root(s), {totalGames} game(s). Press F2 to manage.";
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string GetConfigPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "..", "..", "..", "..", "..", "data");
        dataDir = Path.GetFullPath(dataDir);
        return Path.Combine(dataDir, "settings.json");
    }

    private static string GetGamesDbPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "..", "..", "..", "..", "..", "data");
        dataDir = Path.GetFullPath(dataDir);
        return Path.Combine(dataDir, "games.json");
    }
}
