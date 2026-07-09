using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Detection;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

public partial class MainWindow : Window
{
    private ShellViewModel? _viewModel;
    private IGamesDatabaseService? _dbService;

    public MainWindow()
    {
        InitializeComponent();
        string dbPath = GetGamesDbPath();
        string configPath = GetConfigPath();
        _dbService = new GamesDatabaseService(dbPath);
        var libraryManager = new DesignTimeLibraryManager(
            new DesignTimeGameDiscoveryService(),
            _dbService);
        var configService = new JsonConfigService(configPath);
        _viewModel = new ShellViewModel(libraryManager, configService);
        DataContext = _viewModel;
    }

    public MainWindow(ShellViewModel shellViewModel, IGamesDatabaseService dbService)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "startup.log"),
                $"[MainWindow ctor] InitializeComponent FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n");
            throw;
        }
        
        _viewModel = shellViewModel;
        _dbService = dbService;
        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ShellViewModel.SelectedIndex))
                {
                    var listBox = this.FindControl<ListBox>("LeftListBox");
                    listBox?.ScrollIntoView(_viewModel.SelectedIndex);
                }
            };

            _viewModel.NavigationChanged += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var listBox = this.FindControl<ListBox>("LeftListBox");
                    listBox?.Focus();
                    if (_viewModel.SelectedIndex >= 0)
                        listBox?.ScrollIntoView(_viewModel.SelectedIndex);
                });
            };
        }
    }

    private static ShellViewModel CreateDefaultViewModel()
    {
        var dbService = new GamesDatabaseService(GetGamesDbPath());
        var libraryManager = new DesignTimeLibraryManager(
            new DesignTimeGameDiscoveryService(),
            dbService);
        var configService = new JsonConfigService(GetConfigPath());
        return new ShellViewModel(libraryManager, configService);
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

    private IGamesDatabaseService GetDbService()
    {
        return _dbService ?? new GamesDatabaseService(GetGamesDbPath());
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        if (_viewModel is null)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Up:
                if (_viewModel.SelectedIndex > 0)
                    _viewModel.SelectedIndex--;
                e.Handled = true;
                break;

            case Key.Down:
                if (_viewModel.SelectedIndex < _viewModel.Items.Count - 1)
                    _viewModel.SelectedIndex++;
                e.Handled = true;
                break;

            case Key.Enter:
                _viewModel.NavigateInto();
                e.Handled = true;
                break;

            case Key.Back:
                _viewModel.NavigateUp();
                e.Handled = true;
                break;

            case Key.F9:
                _viewModel.JumpToLibraryRoots();
                e.Handled = true;
                break;

            case Key.F3:
                _viewModel.StatusText = "Not yet implemented";
                e.Handled = true;
                break;

            case Key.F5:
                _viewModel.StatusText = "Launch not yet implemented";
                e.Handled = true;
                break;

            case Key.F8:
                _viewModel.StatusText = "Category view not yet implemented";
                e.Handled = true;
                break;

            case Key.F10:
                Close();
                e.Handled = true;
                break;

            case Key.S:
                if (e.KeyModifiers == KeyModifiers.None)
                {
                    _viewModel.StatusText = "Search not yet implemented";
                    e.Handled = true;
                }
                break;

            case Key.F2:
                await OpenLibrarySetupAsync();
                e.Handled = true;
                break;

            case Key.T:
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift) && !e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    await OpenGameSetupAsync();
                else
                    base.OnKeyDown(e);
                break;

            default:
                base.OnKeyDown(e);
                break;
        }
    }

    private async Task OpenLibrarySetupAsync()
    {
        var dbService = GetDbService();
        var configService = new JsonConfigService(GetConfigPath());
        var libraryManager = new DesignTimeLibraryManager(
            new DesignTimeGameDiscoveryService(),
            dbService);

        AppConfig config = configService.Load();
        foreach (var root in config.LibraryRoots)
            libraryManager.AddRoot(root.Path, root.DefaultType, []);

        var window = new LibrarySetupWindow(
            configService, dbService, libraryManager, new FolderScanner(config.HiddenFolders));
        await ShowDialog(window);

        _viewModel?.Reload();
    }

    private async Task OpenGameSetupAsync()
    {
        if (_viewModel is null) return;
        if (_viewModel.IsAtRootLevel) return;

        var item = _viewModel.SelectedItem;
        if (item?.GameId is null) return;

        var dbService = GetDbService();
        var configService = new JsonConfigService(GetConfigPath());
        var games = dbService.GetGamesForRoot(_viewModel.GetCurrentRootPath()!);
        var game = games.FirstOrDefault(g => g.Id == item.GameId);
        if (game is null) return;

        var window = new GameSetupWindow(game, _viewModel.GetCurrentRootPath()!, configService, dbService);
        await window.ShowDialog(this);

        _viewModel.Reload();
    }

    private void LeftListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_viewModel?.SelectedItem?.IsBrowsable == true)
            _viewModel.NavigateInto();
    }

    private void CommandButtonPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string hotkey || _viewModel is null)
            return;

        switch (hotkey)
        {
            case "F2":
                _ = OpenLibrarySetupAsync();
                break;
            case "F3":
                _viewModel.StatusText = "Not yet implemented";
                break;
            case "F5":
                _viewModel.StatusText = "Launch not yet implemented";
                break;
            case "F8":
                _viewModel.StatusText = "Category view not yet implemented";
                break;
            case "F9":
                _viewModel.JumpToLibraryRoots();
                break;
            case "F10":
                Close();
                break;
        }
    }
}
