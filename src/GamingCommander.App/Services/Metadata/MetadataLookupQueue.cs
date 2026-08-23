using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Sequential online extras. Rescan enqueues; one HTTP chain at a time.
/// Parsers already reject bad payloads. Writes only the sidecar via <see cref="IMetadataService"/>.
/// </summary>
public sealed class MetadataLookupQueue : IDisposable
{
    private readonly IMetadataService _service;
    private readonly IConfigService _config;
    private readonly MetadataOnlineGate? _online;
    private readonly object _gate = new();
    private readonly Queue<WorkItem> _queue = new();
    private readonly HashSet<string> _queuedIds = new(StringComparer.Ordinal);
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private bool _disposed;

    public MetadataLookupQueue(
        IMetadataService service,
        IConfigService config,
        MetadataOnlineGate? online = null)
    {
        _service = service;
        _config = config;
        _online = online;
    }

    /// <summary>Raised after each item is stored (or skipped). May run off the UI thread.</summary>
    public event Action<string, GameMetadataRecord?>? ItemCompleted;

    /// <summary>Raised when the queue line should refresh (enqueue / start / idle).</summary>
    public event Action? ProgressChanged;

    private string? _currentName;

    /// <summary>Games waiting (not including the one in flight).</summary>
    public int PendingCount
    {
        get { lock (_gate) return _queue.Count; }
    }

    /// <summary>Compact status-bar line. Empty when idle.</summary>
    public string StatusLine
    {
        get
        {
            lock (_gate)
            {
                int left = _queue.Count;
                if (_currentName is null && left == 0)
                    return string.Empty;
                if (_currentName is null)
                    return left == 1 ? "1 queued" : $"{left} queued";
                if (left == 0)
                    return $"Looking up {_currentName}";
                return $"Looking up {_currentName} · {left} left";
            }
        }
    }

    /// <summary>Enqueue games from a finished rescan. No-op when online metadata is off.</summary>
    public void Enqueue(IEnumerable<GameEntry> games)
    {
        if (_disposed || !_config.Load().EnableOnlineMetadata || _online is { AllowsHttp: false })
            return;

        bool start = false;
        lock (_gate)
        {
            foreach (GameEntry game in games)
            {
                if (string.IsNullOrWhiteSpace(game.Id) || !_queuedIds.Add(game.Id))
                    continue;

                _queue.Enqueue(ToWork(game));
            }

            if (_worker is null || _worker.IsCompleted)
            {
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                _worker = Task.Run(() => RunAsync(_cts.Token));
                start = true;
            }
        }

        _ = start;
        ProgressChanged?.Invoke();
    }

    /// <summary>Wait until the queue is empty and the worker has stopped.</summary>
    public async Task WaitUntilIdleAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task? worker;
            lock (_gate)
            {
                if (_queue.Count == 0 && (_worker is null || _worker.IsCompleted))
                    return;
                worker = _worker;
            }

            if (worker is not null)
                await worker.WaitAsync(cancellationToken).ConfigureAwait(false);
            else
                await Task.Delay(15, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            WorkItem item;
            lock (_gate)
            {
                if (_online is { AllowsHttp: false })
                {
                    _queue.Clear();
                    _queuedIds.Clear();
                    _currentName = null;
                    _worker = null;
                    ProgressChanged?.Invoke();
                    return;
                }

                if (_queue.Count == 0)
                {
                    _currentName = null;
                    _worker = null;
                    ProgressChanged?.Invoke();
                    return;
                }

                item = _queue.Dequeue();
                _queuedIds.Remove(item.GameEntryId);
                _currentName = item.DisplayName;
            }

            ProgressChanged?.Invoke();

            GameMetadataRecord? record = null;
            try
            {
                record = await _service
                    .RefreshAsync(item.GameEntryId, item.SteamAppId, item.DisplayName, cancellationToken, force: false, item.YearHint)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                // Bad network / parse already handled inside the service. Keep the queue moving.
            }

            lock (_gate)
                _currentName = null;

            try
            {
                ItemCompleted?.Invoke(item.GameEntryId, record);
            }
            catch
            {
            }

            ProgressChanged?.Invoke();
        }
    }

    private static WorkItem ToWork(GameEntry game)
    {
        string? appId = null;
        if (game.PlatformMetadata.TryGetValue("SteamAppId", out string? id) && !string.IsNullOrWhiteSpace(id))
            appId = id.Trim();

        int? year = PeProductYear.Guess(game.ExecutablePath)
            ?? (game.LastModified.Year is >= 1995 and <= 2035 ? game.LastModified.Year : null);
        string searchName = PeProductYear.TitleHint(game.ExecutablePath)
            ?? TitleText.ExpandPacked(game.DisplayName);
        if (string.IsNullOrWhiteSpace(searchName))
            searchName = game.DisplayName;
        return new WorkItem(game.Id, appId, searchName, year);
    }

    private readonly record struct WorkItem(string GameEntryId, string? SteamAppId, string DisplayName, int? YearHint);
}
