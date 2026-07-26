using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
    private CancellationTokenSource? _statusClearCts;
    private CancellationTokenSource? _scanCts;
    private bool _isRefreshing;

    /// <summary>Primary application window. Manages dual-pane navigation, keyboard shortcuts, and game launching.</summary>
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
            .Select(r => r.RootPath);
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

    /// <summary>
    /// Sets status bar text with optional auto-clear after specified milliseconds.
    /// Cancels any pending clear operation before setting new status.
    /// </summary>
    private void SetStatusWithAutoClear(string message, int autoClearMs = 5000)
    {
        if (_viewModel is null) return;

        _viewModel.StatusText = message;

        // Cancel any pending clear
        _statusClearCts?.Cancel();
        _statusClearCts?.Dispose();
        _statusClearCts = null;

        // Schedule auto-clear if requested
        if (autoClearMs > 0)
        {
            _statusClearCts = new CancellationTokenSource();
            CancellationToken token = _statusClearCts.Token;
            Task.Delay(autoClearMs, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_viewModel is not null)
                            _viewModel.StatusText = string.Empty;
                    });
                }
            }, token);
        }
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
                _ = HelpDialogBuilder.ShowHelpAsync(this);
                e.Handled = true;
                break;

            case Key.F3:
                SetStatusWithAutoClear("View metadata — coming in a future update");
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
                _ = RefreshCurrentRootAsync();
                e.Handled = true;
                break;

            case Key.F8:
                SetStatusWithAutoClear("Filter/category view — coming in a future update");
                e.Handled = true;
                break;

            case Key.F10:
                Close();
                e.Handled = true;
                break;

            case Key.S:
                if (e.KeyModifiers == KeyModifiers.None)
                {
                    SetStatusWithAutoClear("Search not yet implemented");
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
        if (item is null || string.IsNullOrEmpty(item.LaunchTarget))
        {
            _viewModel.StatusText = item is not null
                ? $"No launch target for {item.Title}"
                : "No game selected.";
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
            // Resolve arguments: URI launches use the URI itself as the entire target, no extra args
            string args = item.CommandLineArguments.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : item.CommandLineArguments;

            _viewModel.StatusText = string.IsNullOrEmpty(args)
                ? $"Launching: {target}"
                : $"Launching: {target} {args}";

            if (target.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                });
            }
            else
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(target) ?? "",
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
        string? rootPath = _viewModel.GetCurrentRootPath();
        if (rootPath is null) return;
        var games = dbService.GetGamesForRoot(rootPath);
        var game = games.FirstOrDefault(g => g.Id == item.GameId);
        if (game is null) return;

        var window = new GameSetupWindow(game, rootPath, configService, dbService);
        await window.ShowDialog(this);

        _viewModel.Reload();
    }

    private async Task RefreshCurrentRootAsync()
    {
        if (_viewModel is null || _libraryManager is null) return;

        // If already scanning, cancel it (F5 toggle behavior)
        if (_scanCts is not null)
        {
            _scanCts.Cancel();
            _scanCts.Dispose();
            _scanCts = null;
            _viewModel.ClearScanning();
            _isRefreshing = false;
            SetStatusWithAutoClear("Scan cancelled.");
            return;
        }

        if (_isRefreshing) return;

        _isRefreshing = true;
        _scanCts = new CancellationTokenSource();
        CancellationToken ct = _scanCts.Token;

        try
        {
            _viewModel.IsScanning = true;

            // At root level: rescan all configured roots sequentially
            if (_viewModel.IsAtRootLevel)
            {
                var config = GetConfigService().Load();
                if (config.LibraryRoots.Count == 0)
                {
                    SetStatusWithAutoClear("No roots configured. Press F2 to add folders.");
                    return;
                }

                SetStatusWithAutoClear("Scanning all roots...", 0);

                foreach (LibraryRoot root in config.LibraryRoots)
                {
                    ct.ThrowIfCancellationRequested();

                    Dispatcher.UIThread.Post(() =>
                    {
                        _viewModel.SetScanning(root.RootPath);
                        SetStatusWithAutoClear($"Scanning {Path.GetFileName(root.RootPath)}...", 0);
                    });

                    await Task.Run(() =>
                    {
                        ct.ThrowIfCancellationRequested();
                        IReadOnlyList<Core.Models.GameEntry> games =
                            _libraryManager.SelectScannerAndScan(root.RootPath, root.DefaultType, ct);
                        _libraryManager.RescanRoot(root.RootPath, games);
                    }, ct);
                }

                int totalGames = config.LibraryRoots.Sum(
                    r => _libraryManager.GetGamesForRoot(r.RootPath).Count);

                Dispatcher.UIThread.Post(() =>
                {
                    _viewModel.Reload();
                    SetStatusWithAutoClear(
                        $"Rescanned {config.LibraryRoots.Count} root(s), found {totalGames} game(s).");
                });
                return;
            }

            // Drilled into a root: rescan that root only
            string rootPath = _viewModel.CurrentRootPath;
            var cfg = GetConfigService().Load();
            var matchedRoot = cfg.LibraryRoots.FirstOrDefault(r =>
                r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
            if (matchedRoot is null) return;

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel.SetScanning(rootPath);
                SetStatusWithAutoClear($"Scanning {Path.GetFileName(rootPath)}...", 0);
            });

            IReadOnlyList<Core.Models.GameEntry> scannedGames = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return _libraryManager.SelectScannerAndScan(
                    rootPath, matchedRoot.DefaultType, ct);
            }, ct);

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel.ApplyRescannedGames(scannedGames);
                if (scannedGames.Count == 0)
                    SetStatusWithAutoClear("Rescan complete — no games found in this root.");
                else
                    SetStatusWithAutoClear($"Rescan complete — found {scannedGames.Count} game(s).");
            });
        }
        catch (OperationCanceledException)
        {
            Dispatcher.UIThread.Post(() => SetStatusWithAutoClear("Scan cancelled."));
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => SetStatusWithAutoClear($"Rescan failed: {ex.Message}"));
        }
        finally
        {
            Dispatcher.UIThread.Post(() =>
            {
                _viewModel?.ClearScanning();
                _viewModel.IsScanning = false;
            });
            _scanCts?.Dispose();
            _scanCts = null;
            _isRefreshing = false;
        }
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
                _ = HelpDialogBuilder.ShowHelpAsync(this);
                break;
            case "F2":
                _ = OpenLibrarySetupAsync();
                break;
            case "F3":
                SetStatusWithAutoClear("View metadata — coming in a future update");
                break;
            case "F4":
                _ = OpenGameSetupAsync();
                break;
            case "F5":
                _ = RefreshCurrentRootAsync();
                break;
            case "F8":
                SetStatusWithAutoClear("Filter/category view — coming in a future update");
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
