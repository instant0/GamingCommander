using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

public partial class MainWindow : Window
{
    private ShellViewModel? _viewModel;
    private IGamesDatabaseService? _dbService;
    private IConfigService? _configService;
    private FolderScanner? _scanner;
    private SteamLibraryScanner? _steamScanner;

    private LibraryManager? _libraryManager;

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

        // Ensure _scanner and _configService are initialized so OpenLibrarySetupAsync
        // always has a blacklist-enabled scanner (not the null-fallback path).
        _configService = new JsonConfigService(GetConfigPath());
        var blacklist = new BlacklistLoader(AppDomain.CurrentDomain.BaseDirectory).Load();
        _scanner = new FolderScanner(_configService.Load().HiddenFolders, blacklist);

        AppConfig config = _configService.Load();
        var steamPaths = config.LibraryRoots
            .Where(r => r.DefaultType == GameSourceKind.Steam)
            .Select(r => r.Path);
        _steamScanner = new SteamLibraryScanner(steamPaths);

        _libraryManager = new LibraryManager(_configService, _dbService, _scanner, _steamScanner);

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

            _viewModel.RequestLaunch += item => _ = LaunchSelectedGameAsync();
        }
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

    private IConfigService GetConfigService()
    {
        return _configService ?? new JsonConfigService(GetConfigPath());
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

            case Key.Escape:
                _viewModel.NavigateUp();
                e.Handled = true;
                break;

            case Key.F1:
                await ShowHelpAsync();
                e.Handled = true;
                break;

            case Key.F3:
                _viewModel.StatusText = "View metadata — coming in a future update";
                e.Handled = true;
                break;

            case Key.F9:
                _viewModel.JumpToLibraryRoots();
                e.Handled = true;
                break;

            case Key.F4:
                await OpenGameSetupAsync();
                e.Handled = true;
                break;

            case Key.F5:
                await LaunchSelectedGameAsync();
                e.Handled = true;
                break;

            case Key.F6:
                await RefreshCurrentRootAsync();
                e.Handled = true;
                break;

            case Key.F7:
                await AddRootAsync();
                e.Handled = true;
                break;

            case Key.F8:
                _viewModel.StatusText = "Filter/category view — coming in a future update";
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

            // Legacy shortcut — F4 is now the primary retag key
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

    private Task LaunchSelectedGameAsync()
    {
        if (_viewModel is null) return Task.CompletedTask;

        // Must have a game selected (not at root level)
        if (_viewModel.IsAtRootLevel)
        {
            _viewModel.StatusText = "Navigate into a library root first, then select a game to launch.";
            return Task.CompletedTask;
        }

        var item = _viewModel.SelectedItem;
        if (item?.LaunchTarget is null || item.LaunchTarget.Length == 0)
        {
            _viewModel.StatusText = "No executable path for this entry.";
            return Task.CompletedTask;
        }

        string target = item.LaunchTarget;

        // If it's a directory entry (not a file), don't launch
        if (item.Kind == FileSystemEntryKind.Directory)
        {
            _viewModel.NavigateInto();
            return Task.CompletedTask;
        }

        try
        {
            _viewModel.StatusText = $"Launching: {target}";

            if (target.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(target),
                });
            }

            _viewModel.StatusText = $"Launched: {item.Title}";
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Launch failed: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private async Task OpenLibrarySetupAsync()
    {
        if (_libraryManager is null) return;
        var configService = GetConfigService();
        var dbService = GetDbService();

        var window = new LibrarySetupWindow(
            configService, dbService, _libraryManager);
        await window.ShowDialog(this);

        _viewModel?.Reload();
    }

    private async Task OpenGameSetupAsync()
    {
        if (_viewModel is null) return;
        if (_viewModel.IsAtRootLevel) return;

        var item = _viewModel.SelectedItem;
        if (item?.GameId is null) return;

        var dbService = GetDbService();
        var configService = GetConfigService();
        var games = dbService.GetGamesForRoot(_viewModel.GetCurrentRootPath()!);
        var game = games.FirstOrDefault(g => g.Id == item.GameId);
        if (game is null) return;

        var window = new GameSetupWindow(game, _viewModel.GetCurrentRootPath()!, configService, dbService);
        await window.ShowDialog(this);

        _viewModel.Reload();
    }

    private Task RefreshCurrentRootAsync()
    {
        if (_viewModel is null || _libraryManager is null) return Task.CompletedTask;

        // At root level: rescan all configured roots
        if (_viewModel.IsAtRootLevel)
        {
            var config = GetConfigService().Load();
            if (config.LibraryRoots.Count == 0)
            {
                _viewModel.StatusText = "No roots configured. Press F2 or F7 to add one.";
                return Task.CompletedTask;
            }

            _libraryManager.Refresh();
            int totalGames = config.LibraryRoots.Sum(
                r => _libraryManager.GetGamesForRoot(r.Path).Count);
            _viewModel.Reload();
            _viewModel.StatusText = $"Rescanned {config.LibraryRoots.Count} root(s), found {totalGames} game(s).";
            return Task.CompletedTask;
        }

        // Drilled into a root: rescan that root only
        string rootPath = _viewModel.CurrentRootPath;
        var cfg = GetConfigService().Load();
        var matchedRoot = cfg.LibraryRoots.FirstOrDefault(r =>
            r.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
        if (matchedRoot is null) return Task.CompletedTask;

        var scannedGames = _libraryManager.SelectScannerAndScan(rootPath, matchedRoot.DefaultType);
        _viewModel.ApplyRescannedGames(scannedGames);
        if (scannedGames.Count == 0)
            _viewModel.StatusText = "Rescan complete — no games found in this root.";
        return Task.CompletedTask;
    }

    private async Task AddRootAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select game library folder",
                AllowMultiple = false,
            });

        if (folders.Count == 0) return;
        string rawPath = folders[0].Path.LocalPath;
        string result = LibraryManager.NormalizeLibraryRoot(rawPath);

        if (_libraryManager is null) return;

        bool isSteamLibrary = LibraryManager.LooksLikeSteamLibrary(result);
        GameSourceKind detectedType = isSteamLibrary ? GameSourceKind.Steam : GameSourceKind.Standalone;

        // Pass empty games list — LibraryManager.AddRoot will scan internally
        _libraryManager.AddRoot(result, detectedType, []);

        _viewModel?.Reload();
        _viewModel!.StatusText = $"Added root: {result}";
    }

    private async Task ShowHelpAsync()
    {
        string version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(3) ?? "0.0.0";
        var textColor = AppTheme.TextSecondary;
        var headerColor = AppTheme.TextAccent;
        var keyColor = AppTheme.TextHighlight;
        var bgColor = AppTheme.PaneBg;

        var keys = new (string key, string desc)[]
        {
            ("F1", "Help — this window"),
            ("F2", "Library Setup — add/remove/rescan folders"),
            ("F3", "View game metadata (coming soon)"),
            ("F4", "Edit game type / tags"),
            ("F5", "Launch selected game"),
            ("F6", "Rescan current folder or all roots"),
            ("F7", "Add a library root folder"),
            ("F8", "Filter/category view (coming soon)"),
            ("F9", "Jump to library roots"),
            ("F10", "Quit GamingCommander"),
            ("Enter", "Launch game / drill into folder"),
            ("Esc / Backspace", "Go up one level"),
            ("Up / Down", "Navigate list"),
        };

        var panel = new StackPanel { Spacing = 8, Background = bgColor };

        panel.Children.Add(new TextBlock
        {
            Text = "GamingCommander",
            FontSize = AppTheme.FontSizeAppTitle,
            FontWeight = FontWeight.Bold,
            Foreground = headerColor,
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Version {version}",
            FontSize = AppTheme.FontSizeBody,
            Foreground = textColor,
            Margin = new Thickness(0, 0, 0, 8),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "A Norton Commander-style game launcher and library manager.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = AppTheme.FontSizeBody,
            Foreground = textColor,
            Margin = new Thickness(0, 0, 0, 12),
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Keyboard Reference",
            FontSize = AppTheme.FontSizeSubHeader,
            FontWeight = FontWeight.Bold,
            Foreground = headerColor,
            Margin = new Thickness(0, 0, 0, 4),
        });

        foreach (var (key, desc) in keys)
        {
            var row = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition(140, GridUnitType.Pixel),
                    new ColumnDefinition(1, GridUnitType.Star),
                ],
                Margin = new Thickness(0, 2),
            };
            row.Children.Add(new TextBlock { Text = key, Foreground = keyColor, FontWeight = FontWeight.Bold, FontSize = AppTheme.FontSizeBody });
            row.Children.Add(new TextBlock { Text = desc, Foreground = textColor, FontSize = AppTheme.FontSizeBody, Margin = new Thickness(8, 0, 0, 0) });
            Grid.SetColumn(row.Children[1], 1);
            panel.Children.Add(row);
        }

        panel.Children.Add(new TextBlock
        {
            Text = "\nData is stored in the app's data/ directory. No game files are modified.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = textColor,
            FontSize = AppTheme.FontSizeLabel,
            FontStyle = FontStyle.Italic,
            Margin = new Thickness(0, 12, 0, 0),
        });

        var helpWindow = new Window
        {
            Title = "Help — GamingCommander",
            Width = 480,
            Height = 520,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.Full,
            Content = new ScrollViewer
            {
                Background = bgColor,
                Content = new Border
                {
                    Padding = new Thickness(20),
                    Background = bgColor,
                    Child = panel,
                },
            },
        };

        await helpWindow.ShowDialog(this);
    }

    private void LeftListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var item = _viewModel?.SelectedItem;
        if (item is null) return;

        if (item.Kind == FileSystemEntryKind.ParentDirectory)
            _viewModel!.NavigateUp();
        else if (item.Kind == FileSystemEntryKind.File)
            _ = LaunchSelectedGameAsync();
        else if (item.Kind == FileSystemEntryKind.Directory)
            _viewModel!.NavigateInto();
    }

    private void CommandButtonPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string hotkey || _viewModel is null)
            return;

        switch (hotkey)
        {
            case "F1":
                _ = ShowHelpAsync();
                break;
            case "F2":
                _ = OpenLibrarySetupAsync();
                break;
            case "F3":
                _viewModel.StatusText = "View metadata — coming in a future update";
                break;
            case "F4":
                _ = OpenGameSetupAsync();
                break;
            case "F5":
                _ = LaunchSelectedGameAsync();
                break;
            case "F6":
                _ = RefreshCurrentRootAsync();
                break;
            case "F7":
                _ = AddRootAsync();
                break;
            case "F8":
                _viewModel.StatusText = "Filter/category view — coming in a future update";
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
