using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

public partial class MainWindow : Window
{
    private ShellViewModel? _viewModel;
    private IGamesDatabaseService? _dbService;
    private IConfigService? _configService;
    private ILibrariesService? _librariesService;
    private FolderScanner? _scanner;
    private SteamLibraryScanner? _steamScanner;

    private LibraryManager? _libraryManager;
    private IMetadataService? _metadataService;
    private MetadataLookupQueue? _metadataQueue;
    private MetadataOnlineGate? _onlineGate;
    private HttpClient? _probeHttp;
    private CancellationTokenSource? _statusClearCts;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _metadataCts;
    private bool _isRefreshing;

    /// <summary>Primary application window. Manages dual-pane navigation, keyboard shortcuts, and game launching.</summary>
    public MainWindow(
        ShellViewModel shellViewModel,
        IGamesDatabaseService dbService,
        ILibrariesService librariesService,
        IMetadataService? metadataService = null,
        MetadataOnlineGate? onlineGate = null)
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
        _librariesService = librariesService;
        _metadataService = metadataService;
        _onlineGate = onlineGate;

        // Ensure _scanner and _configService are initialized so OpenLibrarySetupAsync
        // always has a blacklist-enabled scanner (not the null-fallback path).
        _configService = new JsonConfigService(GetConfigPath());
        var blacklist = new BlacklistLoader(AppDomain.CurrentDomain.BaseDirectory).Load();
        IRegistryReader registryReader = OperatingSystem.IsWindows()
            ? new WindowsRegistryReader()
            : null!;
        _scanner = new FolderScanner(_configService.Load().HiddenFolders, blacklist, registryReader);

        AppConfig config = _configService.Load();
        var steamPaths = _librariesService.Libraries
            .Where(l => l.Type == GameSourceKind.Steam)
            .SelectMany(l => l.Folders);
        _steamScanner = new SteamLibraryScanner(steamPaths);

        _libraryManager = new LibraryManager(_librariesService, _dbService, _scanner, _steamScanner);

        if (_metadataService is not null)
        {
            _metadataQueue = new MetadataLookupQueue(_metadataService, _configService, _onlineGate);
            _metadataQueue.ItemCompleted += OnMetadataQueueItemCompleted;
            _metadataQueue.ProgressChanged += OnMetadataQueueProgress;
        }

        if (_onlineGate is not null)
        {
            _onlineGate.Changed += UpdateLookupChip;
            Opened += (_, _) => _ = ProbeOnlineAsync();
        }

        Closed += (_, _) =>
        {
            if (_onlineGate is not null)
                _onlineGate.Changed -= UpdateLookupChip;
            if (_metadataQueue is not null)
            {
                _metadataQueue.ItemCompleted -= OnMetadataQueueItemCompleted;
                _metadataQueue.ProgressChanged -= OnMetadataQueueProgress;
                _metadataQueue.Dispose();
            }
            _probeHttp?.Dispose();
        };

        UpdateLookupChip();

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

    private static string GetLibrariesPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataDir = Path.Combine(baseDir, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "libraries.json");
    }

    private ILibrariesService GetLibrariesService()
    {
        return _librariesService ?? new LibrariesDatabaseService(GetLibrariesPath());
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
                if (!_viewModel.SearchBackspace())
                    _viewModel.NavigateUp();
                e.Handled = true;
                break;

            case Key.Escape:
                if (!_viewModel.CancelSearch())
                    _viewModel.NavigateUp();
                e.Handled = true;
                break;

            case Key.F1:
                _ = HelpDialogBuilder.ShowHelpAsync(this);
                e.Handled = true;
                break;

            case Key.F3:
                _ = LookupSelectedGameMetadataAsync();
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
                _ = OpenFilterAsync();
                e.Handled = true;
                break;

            case Key.F10:
                Close();
                e.Handled = true;
                break;

            case Key.F2:
                await OpenLibrarySetupAsync();
                e.Handled = true;
                break;

            default:
                // Plan 122: unbound printable keys silently build a live search query.
                if (e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift
                    && TryMapPrintable(e.Key, out char ch))
                {
                    _viewModel.AppendSearchChar(ch);
                    e.Handled = true;
                    break;
                }
                base.OnKeyDown(e);
                break;
        }
    }

    /// <summary>
    /// Maps unmodified printable keys to characters for type-to-search.
    /// Letters, digits, space, dash, period, and apostrophe (title punctuation).
    /// </summary>
    private static bool TryMapPrintable(Key key, out char ch)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            ch = (char)('a' + (key - Key.A));
            return true;
        }
        if (key is >= Key.D0 and <= Key.D9)
        {
            ch = (char)('0' + (key - Key.D0));
            return true;
        }
        ch = key switch
        {
            Key.Space => ' ',
            Key.OemMinus => '-',
            Key.OemPeriod => '.',
            Key.OemQuotes => '\'',
            _ => default,
        };
        return ch != default;
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
            string args;
            string? libraryName = _viewModel.GetCurrentLibraryName();
            GameEntry? game = item.GameId is not null ? FindGameById(item.GameId) : null;
            if (game is not null)
            {
                (target, args) = GameLaunchResolver.Resolve(game);
            }
            else
            {
                args = LaunchArgumentComposer.IsSteamUri(item.CommandLineArguments)
                    ? string.Empty
                    : item.CommandLineArguments;
            }

            if (string.IsNullOrEmpty(target))
            {
                _viewModel.StatusText = $"No launch target for {item.Title}";
                return Task.CompletedTask;
            }

            _viewModel.StatusText = string.IsNullOrEmpty(args)
                ? $"Launching: {target}"
                : $"Launching: {target} {args}";

            if (LaunchArgumentComposer.IsSteamUri(target))
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
            if (game is not null)
                QueueSilentMetadataIfStale(game);
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Launch failed: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private async Task OpenFilterAsync()
    {
        if (_viewModel is null)
            return;

        // Opening the F8 dialog ends any typed search session (Plan 122).
        if (_viewModel.IsSearching)
            _viewModel.CancelSearch();

        GameFilter? chosen = await FilterWindow.ShowAsync(
            this, _viewModel.CollectFilterOptions(), _viewModel.ActiveFilter).ConfigureAwait(true);
        if (chosen is null)
        {
            if (_viewModel.IsFilterActive)
                _viewModel.ClearFilter();
            SetStatusWithAutoClear("Filter cleared.");
            return;
        }

        _viewModel.ApplyFilter(chosen);
    }

    private void TagBadgePressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_viewModel is null || sender is not Control control)
            return;
        string? name = control.Tag as string ?? (control.DataContext as TagBadgeViewModel)?.Name;
        if (string.IsNullOrWhiteSpace(name))
            return;
        // A tag click replaces any typed search session (Plan 122).
        if (_viewModel.IsSearching)
            _viewModel.CancelSearch();
        _viewModel.ApplyFilter(new GameFilter(GameFilterKind.Tag, name));
        e.Handled = true;
    }

    private async Task OpenLibrarySetupAsync()
    {
        if (_libraryManager is null) return;
        var configService = GetConfigService();
        var dbService = GetDbService();

        // Steam locator uses the same Windows-only registry reader as the scanner.
        IRegistryReader registryReader = OperatingSystem.IsWindows()
            ? new WindowsRegistryReader()
            : new NullRegistryReader();

        var window = new LibrarySetupWindow(
            configService, dbService, _libraryManager, new SteamInstallPathLocator(registryReader));
        await window.ShowDialog(this);

        _viewModel?.Reload();
        await SyncOnlineGateFromConfigAsync().ConfigureAwait(true);
    }

    private async Task OpenGameSetupAsync()
    {
        if (_viewModel is null) return;
        if (_viewModel.IsAtRootLevel) return;

        var item = _viewModel.SelectedItem;
        if (item?.GameId is null) return;

        var dbService = GetDbService();
        var configService = GetConfigService();
        string? libraryName = _viewModel.GetCurrentLibraryName();
        var game = item.GameId is null ? null : FindGameById(item.GameId);
        if (game is null) return;

        IReadOnlyList<GameMetadataCommandLine> catalog =
            _viewModel.GetSidecar(game.Id)?.Details?.CommandLine ?? [];
        if (_onlineGate is { AllowsHttp: true })
            _ = RefreshMetadataForGameAsync(game);

        var window = new GameSetupWindow(game, libraryName, configService, dbService, catalog);
        await window.ShowDialog(this);

        _viewModel.Reload();
    }

    private void QueueSilentMetadataIfStale(GameEntry game)
    {
        if (_onlineGate is not { AllowsHttp: true } || _metadataQueue is null || _viewModel is null)
            return;

        GameMetadataRecord? cached = _viewModel.GetSidecar(game.Id);
        if (!MetadataService.IsStale(cached))
            return;

        _metadataQueue.Enqueue([game]);
    }

    private async Task ProbeOnlineAsync()
    {
        if (_onlineGate is null)
            return;

        AppConfig config = GetConfigService().Load();
        if (!config.EnableOnlineMetadata)
        {
            _onlineGate.SetDisabled();
            return;
        }

        if (_onlineGate.Kind == MetadataOnlineKind.Disabled)
            _onlineGate.SetChecking();

        _probeHttp ??= new HttpClient();
        await _onlineGate.ProbeOnceAsync(_probeHttp).ConfigureAwait(true);
    }

    /// <summary>Re-read F2 flag and probe once if lookup was just turned on.</summary>
    public async Task SyncOnlineGateFromConfigAsync()
    {
        if (_onlineGate is null)
            return;

        AppConfig config = GetConfigService().Load();
        if (!config.EnableOnlineMetadata)
        {
            _onlineGate.SetDisabled();
            return;
        }

        if (_onlineGate.Kind == MetadataOnlineKind.Disabled)
            _onlineGate.SetChecking();

        await ProbeOnlineAsync().ConfigureAwait(true);
    }

    private void UpdateLookupChip()
    {
        void Apply()
        {
            if (_viewModel is not null)
                _viewModel.LookupStatusText = _onlineGate?.StatusLabel ?? "Lookup Disabled";

            var chip = this.FindControl<TextBlock>("LookupStatusChip");
            if (chip is null)
                return;

            chip.Foreground = _onlineGate?.Kind switch
            {
                MetadataOnlineKind.Online => AppTheme.TextSuccess,
                MetadataOnlineKind.Offline => AppTheme.TextDanger,
                MetadataOnlineKind.Checking => AppTheme.TextHighlight,
                _ => AppTheme.TextHighlight,
            };
        }

        if (Dispatcher.UIThread.CheckAccess())
            Apply();
        else
            Dispatcher.UIThread.Post(Apply);
    }

    private void EnqueueMetadataLookups(IReadOnlyList<GameEntry> games)
    {
        if (_metadataQueue is null || games.Count == 0)
            return;

        _metadataQueue.Enqueue(games);
    }

    private void OnMetadataQueueProgress()
    {
        string line = _metadataQueue?.StatusLine ?? string.Empty;
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel is not null)
                _viewModel.LookupQueueText = line;
        });
    }

    private void OnMetadataQueueItemCompleted(string gameId, GameMetadataRecord? record)
    {
        Dispatcher.UIThread.Post(() => _viewModel?.ApplySidecarMetadata(gameId, record));
    }

    private async Task LookupSelectedGameMetadataAsync()
    {
        if (_viewModel is null || _viewModel.IsAtRootLevel)
        {
            SetStatusWithAutoClear("Select a game first.");
            return;
        }

        if (_onlineGate is null || !_configService!.Load().EnableOnlineMetadata)
        {
            SetStatusWithAutoClear("Online lookup is off (F2).");
            return;
        }

        if (!_onlineGate.AllowsHttp)
        {
            SetStatusWithAutoClear("Offline — no lookup.");
            return;
        }

        GameEntry? game = GetSelectedGame();
        if (game is null)
        {
            SetStatusWithAutoClear("Select a game first.");
            return;
        }

        string? chosen = null;
        if (TrySteamAppId(game) is null)
        {
            int? year = TitleText.IsGenericLabel(Path.GetFileNameWithoutExtension(game.ExecutablePath))
                ? game.LastModified.Year is >= 1995 and <= 2035 ? game.LastModified.Year : null
                : PeProductYear.Guess(game.ExecutablePath)
                    ?? (game.LastModified.Year is >= 1995 and <= 2035 ? game.LastModified.Year : null);
            IReadOnlyList<string> pages = [];

            // E7 identity query pipeline (2026-09-07): strictly ordered candidates,
            // FIRST clean result set wins. Folder name is LAST — acronym folders
            // (pigs, mmxl, jag2) must never be queried verbatim before better
            // identity sources. Order: PE title → exe stem → exe stem (normalized
            // separators) → display name → folder name.
            string? peTitle = game.PlatformMetadata.TryGetValue("PeFileDescription", out string? peDesc)
                && !string.IsNullOrWhiteSpace(peDesc)
                ? peDesc
                : null;
            string? exeStem = !string.IsNullOrWhiteSpace(game.ExecutablePath)
                ? Path.GetFileNameWithoutExtension(game.ExecutablePath)
                : null;

            foreach (string query in TitleText.SearchQueries(
                peTitle,
                exeStem,
                TitleText.LookupName(game.DisplayName, game.FolderName, game.GameSource),
                game.DisplayName,
                game.FolderName)) // LAST — folder name is the weakest identity signal
            {
                pages = PcgwTitleFilter.Dedupe(
                    await _metadataService!.SearchPagesAsync(query).ConfigureAwait(true));
                if (pages.Count > 0)
                    break;
            }
            if (pages.Count > 1)
            {
                string? preferred = PcgwTitleFilter.PickBest(pages, year);
                chosen = await PickPcgwPageWindow.ShowAsync(this, pages, preferred).ConfigureAwait(true);
                if (chosen is null)
                {
                    SetStatusWithAutoClear("Lookup cancelled.");
                    return;
                }
            }
            else if (pages.Count == 1)
            {
                chosen = pages[0];
            }
        }

        GameMetadataRecord? record = await RefreshMetadataForGameAsync(game, force: true, pcgwPage: chosen)
            .ConfigureAwait(true);

        // E8 title persistence (2026-09-07): user intent is authoritative and never
        // overwritten. When a title was picked (user chose it) OR confidently
        // auto-resolved (single clean page), persist it as DisplayName with a
        // TitleSource marker so rescans preserve it. Folder-derived titles are
        // replaced; a user-picked title is never overwritten by detection.
        if (chosen is not null && !IsUserPinnedTitle(game))
        {
            var pinnedMeta = new Dictionary<string, string>(game.PlatformMetadata)
            {
                ["TitleSource"] = "PcgwPick",
                ["AutoDetectedTitle"] = game.DisplayName,
            };
            var pinned = game with
            {
                DisplayName = chosen,
                PlatformMetadata = pinnedMeta,
            };
            if (_viewModel?.GetCurrentLibraryName() is not null)
            {
                GetDbService().UpdateGameEntry(pinned);
                _viewModel.Reload();
            }
        }

        if (record?.HasDisplayableExtras == true)
            SetStatusWithAutoClear($"Metadata updated: {chosen ?? game.DisplayName}");
        else
            SetStatusWithAutoClear($"No extras found for {chosen ?? game.DisplayName}.");
    }

    /// <summary>
    /// True when the game's title is user-pinned (TitleSource = PcgwPick / UserOverride)
    /// — user intent is authoritative and must never be overwritten by a later pick.
    /// </summary>
    private static bool IsUserPinnedTitle(GameEntry game)
    {
        if (game.PlatformMetadata.TryGetValue("TitleSource", out string? source))
        {
            return source is "PcgwPick" or "UserOverride";
        }
        return false;
    }

    /// <summary>ACF / sidecar AppID. When set, F3 must not OpenSearch by name.</summary>
    private static string? TrySteamAppId(GameEntry game)
    {
        if (game.PlatformMetadata.TryGetValue("SteamAppId", out string? id)
            && !string.IsNullOrWhiteSpace(id))
        {
            return id.Trim();
        }

        return null;
    }

    private GameEntry? GetSelectedGame()
    {
        if (_viewModel?.SelectedItem?.GameId is null)
            return null;
        return FindGameById(_viewModel.SelectedItem.GameId);
    }

    /// <summary>Finds a game by ID across all libraries (a game belongs to exactly one anchor).</summary>
    private GameEntry? FindGameById(string gameId)
    {
        GamesDatabase db = GetDbService().Load();
        return db.Games.FirstOrDefault(g => g.Id == gameId);
    }

    /// <summary>Online extras for one game. Does not block F4. F3 passes force.</summary>
    private async Task<GameMetadataRecord?> RefreshMetadataForGameAsync(
        GameEntry game, bool force = false, string? pcgwPage = null)
    {
        if (_metadataService is null || _viewModel is null)
            return null;

        _metadataCts?.Cancel();
        _metadataCts?.Dispose();
        var cts = new CancellationTokenSource();
        _metadataCts = cts;

        string? steamAppId = TrySteamAppId(game);

        try
        {
            _viewModel.StatusText = $"Looking up metadata: {game.DisplayName}";
            GameMetadataRecord? record = await _metadataService
                .RefreshAsync(game.Id, steamAppId, game.DisplayName, cts.Token, force, PeProductYear.Guess(game.ExecutablePath), pcgwPage)
                .ConfigureAwait(true);
            _viewModel.ApplySidecarMetadata(game.Id, record);
            return record;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
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

            var libraries = GetLibrariesService().Libraries;

            // At root level or filter: rescan all configured libraries sequentially
            if (_viewModel.IsAtRootLevel || _viewModel.IsFilterActive)
            {
                if (libraries.Count == 0)
                {
                    SetStatusWithAutoClear("No libraries configured. Press F2 to add library folders.");
                    return;
                }

                SetStatusWithAutoClear("Scanning all libraries...", 0);

                var allGames = new List<GameEntry>();
                foreach (Library lib in libraries)
                {
                    ct.ThrowIfCancellationRequested();

                    string libName = lib.Name;
                    Dispatcher.UIThread.Post(() =>
                    {
                        _viewModel.SetScanning(libName);
                        SetStatusWithAutoClear($"Scanning {libName}...", 0);
                    });

                    await Task.Run(() =>
                    {
                        ct.ThrowIfCancellationRequested();
                        _libraryManager.RescanLibrary(libName, ct);
                    }, ct);

                    allGames.AddRange(_dbService.GetGamesForLibrary(libName));
                }

                EnqueueMetadataLookups(allGames);

                Dispatcher.UIThread.Post(() =>
                {
                    _viewModel.Reload();
                    SetStatusWithAutoClear(
                        $"Rescanned {libraries.Count} librar{(libraries.Count != 1 ? "ies" : "y")}, found {allGames.Count} game(s).");
                });
                return;
            }

            // Drilled into a library: rescan that anchor only
            string currentLibrary = _viewModel.CurrentLibraryName;
            Library? matched = libraries.FirstOrDefault(l =>
                l.Name.Equals(currentLibrary, StringComparison.OrdinalIgnoreCase));
            if (matched is null) return;

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel.SetScanning(currentLibrary);
                SetStatusWithAutoClear($"Scanning {currentLibrary}...", 0);
            });

            IReadOnlyList<GameEntry> scannedGames = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                _libraryManager.RescanLibrary(currentLibrary, ct);
                return _dbService.GetGamesForLibrary(currentLibrary);
            }, ct);

            EnqueueMetadataLookups(scannedGames);

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel.Reload();
                if (scannedGames.Count == 0)
                    SetStatusWithAutoClear("Rescan complete — no games found in this library.");
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

    private void WriteEpicItem_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        _ = WriteEpicItemAsync();

    private async Task WriteEpicItemAsync()
    {
        GameEntry? game = GetSelectedGame();
        if (game is null)
        {
            SetStatusWithAutoClear("Select a game first.");
            return;
        }

        if (!await EpicRepairDialog.ConfirmAsync(this, game.DisplayName).ConfigureAwait(true))
        {
            SetStatusWithAutoClear("Epic .item write cancelled.");
            return;
        }

        string gameFolder = game.PlatformMetadata.GetValueOrDefault("GameFolder", "");
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            // GameFolder metadata may be absent; the entry's FolderPath IS the physical
            // game folder (never the anchor name — anchors can be display-only catalogs).
            gameFolder = game.FolderPath;
        }
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            SetStatusWithAutoClear("No game folder known for this entry. Rescan the library first.");
            return;
        }

        string manifests = EpicManifestPaths.DefaultManifestsDir;
        if (!EpicItemWriter.TryWrite(
                gameFolder, manifests, out string path, out string error, game.DisplayName))
        {
            SetStatusWithAutoClear(error);
            return;
        }

        foreach (GameEntry row in GetDbService().Load().Games)
        {
            if (row.GameSource != GameSourceKind.Epic)
                continue;
            string folder = row.PlatformMetadata.GetValueOrDefault("GameFolder",
                Path.Combine(Path.GetDirectoryName(row.FolderPath) ?? "", row.FolderName));
            if (!EpicInstallPath.Same(folder, gameFolder))
                continue;
            var extra = new Dictionary<string, string>(row.PlatformMetadata)
            {
                ["EpicStatus"] = "Installed",
                ["EpicItemPath"] = path,
                ["GameFolder"] = gameFolder,
            };
            GetDbService().UpdateGameEntry(row with { PlatformMetadata = extra, ManifestPath = path });
        }

        _viewModel?.Reload();
        SetStatusWithAutoClear(
            $"Wrote {Path.GetFileName(path)}. Epic Update can officialize ONE of these per launcher run. Quit Epic before writing another.");
    }

    private void WriteSteamAcf_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null)
            return;

        GameEntry? game = GetSelectedGame();
        if (game is null)
        {
            SetStatusWithAutoClear("Select a game first.");
            return;
        }

        string appId = _viewModel.SteamAppIdForAcf;
        if (!SteamAcfWriter.IsAppId(appId))
        {
            SetStatusWithAutoClear("No Steam AppID yet. F3 lookup first, then write the ACF.");
            return;
        }

        string lib = game.PlatformMetadata.GetValueOrDefault("LibraryRoot", "")
            is { Length: > 0 } stored
            ? stored
            : LibraryManager.NormalizeLibraryRoot(game.FolderPath);
        if (!SteamAcfWriter.TryWrite(
                lib, appId, game.DisplayName, game.FolderName, out string path, out string error))
        {
            SetStatusWithAutoClear(error);
            return;
        }

        var extra = new Dictionary<string, string>(game.PlatformMetadata)
        {
            ["SteamStatus"] = "Installed",
            ["SteamAppId"] = appId,
            ["AcfFilePath"] = path,
            ["AcfLibraryPath"] = lib,
        };
        var updated = game with
        {
            CommandLineArguments = $"steam://rungameid/{appId}",
            ManifestPath = path,
            PlatformMetadata = extra,
        };
        GetDbService().UpdateGameEntry(updated);
        _viewModel.Reload();
        SetStatusWithAutoClear($"Wrote {Path.GetFileName(path)}");
    }

    private void ChangeType_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        _ = OpenGameSetupAsync();

    private void MultipleExeWarning_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        _ = OpenGameSetupAsync();

    private void OpenConfigPath_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        OpenFolderFromDisplay(_viewModel?.DetailsConfigPath);

    private void OpenSavePath_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        OpenFolderFromDisplay(_viewModel?.DetailsSavePath);

    private void OpenGameFolder_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        OpenFolderFromDisplay(_viewModel?.DetailsGameFolder);

    private void OpenFolderFromDisplay(string? displayPath)
    {
        string? gameDir = _viewModel?.SelectedItem?.InstallDirectory;
        if (!WindowsExplorer.TryBuildOpenFolder(displayPath, gameDir, out string fileName, out string arguments))
        {
            SetStatusWithAutoClear("Path is not a local Windows folder.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            SetStatusWithAutoClear($"Could not open folder: {ex.Message}");
        }
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
                _ = LookupSelectedGameMetadataAsync();
                break;
            case "F4":
                _ = OpenGameSetupAsync();
                break;
            case "F5":
                _ = RefreshCurrentRootAsync();
                break;
            case "F8":
                _ = OpenFilterAsync();
                break;
            case "F10":
                Close();
                break;
        }
    }
}
