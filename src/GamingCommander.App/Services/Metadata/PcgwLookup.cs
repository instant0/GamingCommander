using GamingCommander.Core.Services;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Fetches PCGW pages. Path: appid.php or OpenSearch → action=parse.
/// Does not call Cargo (permission denied). Inject HttpClient in tests.
/// </summary>
public sealed class PcgwLookup : IDisposable
{
    public const string Api = "https://www.pcgamingwiki.com/w/api.php";
    public const string AppIdPhp = "https://www.pcgamingwiki.com/api/appid.php";
    public const string UserAgent = "GamingCommander/0.1 (metadata; pcgw)";

    private readonly HttpClient _http;
    private readonly TimeSpan _minInterval;
    private readonly bool _ownsClient;
    private DateTime _lastCallUtc = DateTime.MinValue;
    /// <summary>Optional. Network failures flip the session offline.</summary>
    public MetadataOnlineGate? Online { get; set; }
    private int _requestCount;

    public PcgwLookup(HttpClient http, TimeSpan? minInterval = null)
        : this(http, minInterval, ownsClient: false)
    {
    }

    public PcgwLookup(TimeSpan? minInterval = null)
        : this(new HttpClient(), minInterval, ownsClient: true)
    {
    }

    private PcgwLookup(HttpClient http, TimeSpan? minInterval, bool ownsClient)
    {
        _http = http;
        _ownsClient = ownsClient;
        _minInterval = minInterval ?? TimeSpan.FromMilliseconds(600);
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
    }

    public int RequestCount => _requestCount;

    /// <summary>Resolve a page then parse Infobox + operator sections from one Parse payload.</summary>
    public async Task<PcgwLookupResult?> LookupAsync(
        string? steamAppId,
        string? displayName,
        CancellationToken cancellationToken = default,
        int? yearHint = null,
        string? pageTitleOverride = null)
    {
        string? page = null;
        if (!string.IsNullOrWhiteSpace(pageTitleOverride))
        {
            page = pageTitleOverride.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(steamAppId))
        {
            string? html = await GetStringAsync($"{AppIdPhp}?appid={Uri.EscapeDataString(steamAppId.Trim())}", cancellationToken)
                .ConfigureAwait(false);
            page = html is null ? null : PcgwInfoboxParser.ParseHtmlTitle(html);
            if (PcgwTitleFilter.IsNoisy(page))
                page = null;
        }

        if (page is null && !string.IsNullOrWhiteSpace(displayName))
        {
            IReadOnlyList<string> titles = await SearchTitlesAsync(displayName, cancellationToken).ConfigureAwait(false);
            page = PcgwTitleFilter.PickBest(titles, yearHint);
        }

        if (page is null || PcgwTitleFilter.IsNoisy(page))
            return null;

        return await FetchPageAsync(page, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Clean OpenSearch titles (soundtrack/demo removed).</summary>
    public async Task<IReadOnlyList<string>> SearchTitlesAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        string url = Api + "?action=opensearch&limit=8&format=json&search=" + Uri.EscapeDataString(displayName.Trim());
        string? json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        if (json is null)
            return [];

        return PcgwInfoboxParser.ParseOpenSearchTitles(json)
            .Where(t => !PcgwTitleFilter.IsNoisy(t))
            .ToList();
    }

    /// <summary>Parse one wiki page by exact title.</summary>
    public async Task<PcgwLookupResult?> FetchPageAsync(string page, CancellationToken cancellationToken = default)
    {
        string parseUrl = Api + "?action=parse&prop=wikitext&format=json&page=" + Uri.EscapeDataString(page);
        string? parseJson = await GetStringAsync(parseUrl, cancellationToken).ConfigureAwait(false);
        if (parseJson is null)
            return null;

        PcgwFacts? facts = PcgwInfoboxParser.ParseApiResponse(parseJson);
        string? wikitext = PcgwInfoboxParser.ExtractWikitext(parseJson);
        PcgwSectionFacts sections = PcgwSectionParser.ParseAll(wikitext ?? "");
        if (facts is null && !HasSections(sections))
            return null;

        return new PcgwLookupResult(facts, sections);
    }

    /// <summary>Resolve a page then parse Infobox facts. Returns null when nothing resolves.</summary>
    public async Task<PcgwFacts?> LookupFactsAsync(
        string? steamAppId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        PcgwLookupResult? page = await LookupAsync(steamAppId, displayName, cancellationToken).ConfigureAwait(false);
        return page?.Facts;
    }

    private static bool HasSections(PcgwSectionFacts sections) =>
        sections.Paths.Count > 0
        || sections.CommandLine.Count > 0
        || sections.Fixes.Count > 0
        || sections.Video.Count > 0
        || sections.CloudSync.Count > 0
        || sections.StoreIds.Count > 0;

    private async Task<string?> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        if (Online is not null && !Online.IsLookupEnabled())
            return null;

        await EnforceRateLimitAsync(cancellationToken).ConfigureAwait(false);
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

    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }
}
