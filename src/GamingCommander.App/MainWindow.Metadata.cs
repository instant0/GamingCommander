using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App;

/// <summary>
/// Metadata lookup member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// Owns the in-flight metadata cancellation token.
/// </summary>
public partial class MainWindow
{
    private CancellationTokenSource? _metadataCts;

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

        AppConfig config = _configService.Load();
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

        AppConfig config = _configService.Load();
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
            string? peTitle = game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.PeFileDescription, out string? peDesc)
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
                [PlatformMetadataKeys.TitleSource] = TitleSourceValues.PcgwPick,
                [PlatformMetadataKeys.AutoDetectedTitle] = game.DisplayName,
            };
            var pinned = game with
            {
                DisplayName = chosen,
                PlatformMetadata = pinnedMeta,
            };
            if (_viewModel?.GetCurrentLibraryName() is not null)
            {
                _dbService.UpdateGameEntry(pinned);
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
        if (game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.TitleSource, out string? source))
        {
            return source is TitleSourceValues.PcgwPick or TitleSourceValues.UserOverride;
        }
        return false;
    }

    /// <summary>ACF / sidecar AppID. When set, F3 must not OpenSearch by name.</summary>
    private static string? TrySteamAppId(GameEntry game)
    {
        if (game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.SteamAppId, out string? id)
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
        GamesDatabase db = _dbService.Load();
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
}