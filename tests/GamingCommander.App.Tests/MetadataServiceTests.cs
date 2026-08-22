using System.Net;
using System.Net.Http.Headers;
using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class MetadataServiceTests : IDisposable
{
    private readonly string _tempDir;

    public MetadataServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MetaSvc_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task Refresh_FlagOff_DoesNotHttp_ReturnsCache()
    {
        var store = new MetadataStore(Path.Combine(_tempDir, "games_metadata.json"));
        store.Upsert("g1", new GameMetadataRecord { Developer = "Cached" });
        var handler = new RoutingHandler();
        using var http = new HttpClient(handler);
        using var steam = new SteamStoreLookup(http, TimeSpan.Zero);
        using var pcgw = new PcgwLookup(http, TimeSpan.Zero);
        var svc = new MetadataService(store, new StubConfig(enableOnline: false), steam, pcgw);

        GameMetadataRecord? result = await svc.RefreshAsync("g1", "1091500", "Cyberpunk 2077");

        Assert.Equal("Cached", result?.Developer);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Refresh_SteamThenPcgw_MergesWithoutTouchingGamesJson()
    {
        string gamesJson = Path.Combine(_tempDir, "games.json");
        File.WriteAllText(gamesJson, "{\"roots\":[]}");
        string before = File.ReadAllText(gamesJson);

        var store = new MetadataStore(Path.Combine(_tempDir, "games_metadata.json"));
        var handler = new RoutingHandler();
        using var http = new HttpClient(handler);
        using var steam = new SteamStoreLookup(http, TimeSpan.Zero);
        using var pcgw = new PcgwLookup(http, TimeSpan.Zero);
        var svc = new MetadataService(store, new StubConfig(enableOnline: true), steam, pcgw);

        GameMetadataRecord? result = await svc.RefreshAsync("g1", "1091500", "Cyberpunk 2077");

        Assert.NotNull(result);
        Assert.Contains("CD Projekt", result.Developer, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(86, result.MetacriticScore);
        Assert.Equal("1091500", result.SteamAppId);
        Assert.Equal("REDengine 4", result.Engine);
        Assert.Contains("wiki/Cyberpunk_2077", result.PcGamingWikiUrl);
        Assert.NotNull(result.Details);
        Assert.Contains(result.Details.CommandLine, c => c.Argument == "--launcher-skip");
        Assert.Contains(result.Details.ConfigPaths, p => p.Os == "Windows");
        Assert.Equal(before, File.ReadAllText(gamesJson));
        Assert.True(handler.Calls > 0);
    }

    [Fact]
    public async Task Refresh_NoLocalAppId_PcgwNameThenSteamStore()
    {
        var store = new MetadataStore(Path.Combine(_tempDir, "games_metadata.json"));
        var handler = new RoutingHandler();
        using var http = new HttpClient(handler);
        using var steam = new SteamStoreLookup(http, TimeSpan.Zero);
        using var pcgw = new PcgwLookup(http, TimeSpan.Zero);
        var svc = new MetadataService(store, new StubConfig(enableOnline: true), steam, pcgw);

        GameMetadataRecord? result = await svc.RefreshAsync("g1", steamAppId: null, "Cyberpunk 2077");

        Assert.Equal("1091500", result?.SteamAppId);
        Assert.Equal(86, result?.MetacriticScore);
        Assert.True(handler.Uris.Exists(u => u.Contains("opensearch", StringComparison.OrdinalIgnoreCase)));
        Assert.True(handler.Uris.Exists(u => u.Contains("store.steampowered.com", StringComparison.OrdinalIgnoreCase)));
        Assert.False(handler.Uris.Exists(u => u.Contains("appid.php", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Refresh_FreshCache_SkipsHttp()
    {
        var store = new MetadataStore(Path.Combine(_tempDir, "games_metadata.json"));
        store.Upsert("g1", new GameMetadataRecord
        {
            Developer = "Fresh",
            LastUpdated = DateTimeOffset.UtcNow,
        });
        var handler = new RoutingHandler();
        using var http = new HttpClient(handler);
        using var steam = new SteamStoreLookup(http, TimeSpan.Zero);
        var svc = new MetadataService(store, new StubConfig(enableOnline: true), steam, pcgw: null);

        GameMetadataRecord? result = await svc.RefreshAsync("g1", "1091500", "X");

        Assert.Equal("Fresh", result?.Developer);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void IsStale_MissingOrOld()
    {
        Assert.True(MetadataService.IsStale(null));
        Assert.True(MetadataService.IsStale(new GameMetadataRecord()));
        Assert.False(MetadataService.IsStale(new GameMetadataRecord { LastUpdated = DateTimeOffset.UtcNow }));
        Assert.True(MetadataService.IsStale(new GameMetadataRecord { LastUpdated = DateTimeOffset.UtcNow.AddDays(-31) }));
    }

    [Fact]
    public void Merge_PcgwFillsEngineAndPrefersReleaseDate()
    {
        var steam = new GameMetadataRecord { Developer = "SteamDev", ReleaseDate = "9 Dec, 2020" };
        var pcgw = new GameMetadataRecord { Developer = "WikiDev", ReleaseDate = "December 10, 2020", Engine = "REDengine 4" };

        var merged = MetadataService.Merge(steam, pcgw, MetadataSource.Pcgw);

        Assert.Equal("SteamDev", merged!.Developer);
        Assert.Equal("December 10, 2020", merged.ReleaseDate);
        Assert.Equal("REDengine 4", merged.Engine);
    }

    [Fact]
    public void Merge_KeepsDetailsWhenSteamHasNone()
    {
        var withDetails = new GameMetadataRecord
        {
            Developer = "Wiki",
            Details = new GameMetadataDetails
            {
                CommandLine = [new GameMetadataCommandLine { Argument = "--launcher-skip", Notes = "skip" }],
            },
        };
        var steam = new GameMetadataRecord { Developer = "SteamDev", MetacriticScore = 86 };

        var afterSteam = MetadataService.Merge(withDetails, steam, MetadataSource.Steam);
        Assert.Contains(afterSteam!.Details!.CommandLine, c => c.Argument == "--launcher-skip");

        var emptyPcgw = new GameMetadataRecord { Engine = "REDengine 4" };
        var afterEmpty = MetadataService.Merge(afterSteam, emptyPcgw, MetadataSource.Pcgw);
        Assert.Contains(afterEmpty!.Details!.CommandLine, c => c.Argument == "--launcher-skip");
    }

    private sealed class StubConfig : IConfigService
    {
        private readonly AppConfig _config;

        public StubConfig(bool enableOnline)
        {
            _config = new AppConfig([], [], [], IsFirstRun: false, EnableOnlineMetadata: enableOnline);
        }

        public AppConfig Load() => _config;
        public void Save(AppConfig config) { }
    }

    private sealed class RoutingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public List<string> Uris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            string uri = request.RequestUri?.ToString() ?? "";
            Uris.Add(uri);
            string body;
            if (uri.Contains("store.steampowered.com", StringComparison.OrdinalIgnoreCase))
                body = File.ReadAllText(Fixture("steam_appdetails_1091500.json"));
            else if (uri.Contains("appid.php", StringComparison.OrdinalIgnoreCase))
                body = File.ReadAllText(Fixture("pcgw_appid.html"));
            else if (uri.Contains("opensearch", StringComparison.OrdinalIgnoreCase))
                body = File.ReadAllText(Fixture("pcgw_opensearch.json"));
            else if (uri.Contains("action=parse", StringComparison.OrdinalIgnoreCase))
            {
                string wt = File.ReadAllText(Fixture("pcgw_infobox.wikitext"))
                    + "\n" + File.ReadAllText(Fixture(Path.Combine("pcgw", "cyberpunk_sections.wikitext")));
                body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    parse = new { title = "Cyberpunk 2077", wikitext = new Dictionary<string, string> { ["*"] = wt } },
                });
            }
            else
                body = "{}";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }

        private static string Fixture(string name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    }
}
