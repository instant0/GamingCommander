using Avalonia.Threading;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

/// <summary>
/// Library rescan member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// Owns the scan cancellation token and the in-progress flag.
/// </summary>
public partial class MainWindow
{
    private CancellationTokenSource? _scanCts;
    private bool _isRefreshing;

    private async Task RefreshCurrentRootAsync()
    {
        if (_viewModel is null) return;

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

            var libraries = _librariesService.Libraries;

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
            var viewModel = _viewModel;
            Dispatcher.UIThread.Post(() =>
            {
                viewModel?.ClearScanning();
                if (viewModel is not null)
                    viewModel.IsScanning = false;
            });
            _scanCts?.Dispose();
            _scanCts = null;
            _isRefreshing = false;
        }
    }
}