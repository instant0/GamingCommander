using GamingCommander.Core;

namespace GamingCommander.App.Services.Metadata;

public enum MetadataOnlineKind
{
    Disabled,
    Checking,
    Online,
    Offline,
}

/// <summary>
/// One connectivity probe per process. Offline is sticky — no more HTTP this session.
/// </summary>
public sealed class MetadataOnlineGate
{
    /// <summary>HEAD favicon on a host we already use. Any HTTP response = online.</summary>
    public const string ProbeUrl = "https://www.pcgamingwiki.com/favicon.ico";
    public const string UserAgent = PcgwLookup.UserAgent;

    private readonly object _gate = new();
    private readonly IConfigService? _config;
    private bool _probed;

    public MetadataOnlineGate(IConfigService? config = null)
    {
        _config = config;
    }

    public MetadataOnlineKind Kind { get; private set; } = MetadataOnlineKind.Disabled;

    /// <summary>True only after a successful probe and no later failure.</summary>
    public bool AllowsHttp
    {
        get { lock (_gate) return Kind == MetadataOnlineKind.Online; }
    }

    public event Action? Changed;

    public void SetDisabled() => Set(MetadataOnlineKind.Disabled);

    public void SetChecking() => Set(MetadataOnlineKind.Checking);

    /// <summary>One GET to PCGW. Further calls are no-ops.</summary>
    public async Task ProbeOnceAsync(HttpClient http, CancellationToken cancellationToken = default)
    {
        if (!IsLookupEnabled())
        {
            SetDisabled();
            return;
        }

        lock (_gate)
        {
            if (Kind == MetadataOnlineKind.Disabled)
                return;
            if (_probed)
                return;
            _probed = true;
        }

        Set(MetadataOnlineKind.Checking);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            EnsureUserAgent(http);
            using var request = new HttpRequestMessage(HttpMethod.Head, ProbeUrl);
            using HttpResponseMessage response = await http.SendAsync(request, cts.Token).ConfigureAwait(false);
            _ = response.StatusCode;
            Set(MetadataOnlineKind.Online);
        }
        catch
        {
            Set(MetadataOnlineKind.Offline);
        }
    }

    /// <summary>A later lookup lost the network. Stop all further HTTP.</summary>
    public void ReportFailure()
    {
        lock (_gate)
        {
            if (Kind == MetadataOnlineKind.Disabled)
                return;
            _probed = true;
        }

        Set(MetadataOnlineKind.Offline);
    }

    /// <summary>F2 checkbox. Missing config (tests) does not block an explicit probe.</summary>
    public bool IsLookupEnabled() =>
        _config is null || _config.Load().EnableOnlineMetadata;

    public string StatusLabel => Kind switch
    {
        MetadataOnlineKind.Online => "Online",
        MetadataOnlineKind.Offline => "Offline",
        MetadataOnlineKind.Checking => "Checking…",
        _ => "Lookup Disabled",
    };

    private static void EnsureUserAgent(HttpClient http)
    {
        if (http.DefaultRequestHeaders.UserAgent.Count == 0)
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
    }

    private void Set(MetadataOnlineKind kind)
    {
        lock (_gate)
        {
            if (Kind == kind)
                return;
            Kind = kind;
        }

        Changed?.Invoke();
    }
}
