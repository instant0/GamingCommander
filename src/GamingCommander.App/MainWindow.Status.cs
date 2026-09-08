using Avalonia.Threading;

namespace GamingCommander.App;

/// <summary>
/// Status bar member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// Owns the auto-clear cancellation token.
/// </summary>
public partial class MainWindow
{
    private CancellationTokenSource? _statusClearCts;

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
}