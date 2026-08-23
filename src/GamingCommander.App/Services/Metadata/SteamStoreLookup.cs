namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Fetches raw Steam Store <c>appdetails</c> JSON. Does not parse.
/// Inject <see cref="HttpMessageHandler"/> in tests — do not hit Valve from <c>dotnet test</c>.
/// </summary>
public sealed class SteamStoreLookup : IDisposable
{
    public const string Endpoint = "https://store.steampowered.com/api/appdetails";
    public const string UserAgent = "GamingCommander/0.1 (metadata; steam store)";

    private readonly HttpClient _http;
    private readonly TimeSpan _minInterval;
    private readonly bool _ownsClient;
    private DateTime _lastCallUtc = DateTime.MinValue;
    /// <summary>Optional. Network failures flip the session offline.</summary>
    public MetadataOnlineGate? Online { get; set; }
    private int _requestCount;

    /// <summary>Creates a lookup with an injected client (tests or shared handler).</summary>
    public SteamStoreLookup(HttpClient http, TimeSpan? minInterval = null)
        : this(http, minInterval, ownsClient: false)
    {
    }

    /// <summary>Creates a lookup with a default <see cref="HttpClient"/> (production).</summary>
    public SteamStoreLookup(TimeSpan? minInterval = null)
        : this(new HttpClient(), minInterval, ownsClient: true)
    {
    }

    private SteamStoreLookup(HttpClient http, TimeSpan? minInterval, bool ownsClient)
    {
        _http = http;
        _ownsClient = ownsClient;
        _minInterval = minInterval ?? TimeSpan.FromSeconds(10);
        EnsureUserAgent(_http);
    }

    /// <summary>Number of HTTP requests issued (for tests).</summary>
    public int RequestCount => _requestCount;

    /// <summary>
    /// GET appdetails. Returns raw JSON, or null when AppID is empty or the request fails.
    /// Does not interpret <c>success</c> — that is <see cref="SteamStoreParser"/>.
    /// </summary>
    public async Task<string?> FetchRawAsync(string appId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return null;

        if (Online is not null && !Online.IsLookupEnabled())
            return null;

        await EnforceRateLimitAsync(cancellationToken).ConfigureAwait(false);

        string url = $"{Endpoint}?appids={Uri.EscapeDataString(appId.Trim())}&l=english";
        try
        {
            _requestCount++;
            using HttpResponseMessage response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if ((int)response.StatusCode >= 500)
                Online?.ReportFailure();
            if (!response.IsSuccessStatusCode)
                return null;

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(body) ? null : body;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            Online?.ReportFailure();
            return null;
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        if (_minInterval <= TimeSpan.Zero || _lastCallUtc == DateTime.MinValue)
        {
            _lastCallUtc = DateTime.UtcNow;
            return;
        }

        TimeSpan wait = _minInterval - (DateTime.UtcNow - _lastCallUtc);
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
        _lastCallUtc = DateTime.UtcNow;
    }

    private static void EnsureUserAgent(HttpClient http)
    {
        if (http.DefaultRequestHeaders.UserAgent.Count == 0)
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }
}
