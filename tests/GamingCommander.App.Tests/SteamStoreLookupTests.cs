using System.Net;
using System.Net.Http.Headers;
using GamingCommander.App.Services.Metadata;

namespace GamingCommander.App.Tests;

public sealed class SteamStoreLookupTests
{
    [Fact]
    public async Task FetchRaw_EmptyAppId_DoesNotSend()
    {
        var handler = new StubHandler("unused");
        using var http = new HttpClient(handler);
        using var lookup = new SteamStoreLookup(http, TimeSpan.Zero);

        Assert.Null(await lookup.FetchRawAsync(""));
        Assert.Equal(0, handler.Calls);
        Assert.Equal(0, lookup.RequestCount);
    }

    [Fact]
    public async Task FetchRaw_ReturnsFixtureBody_NoLiveHost()
    {
        string fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "steam_appdetails_1091500.json"));
        var handler = new StubHandler(fixture);
        using var http = new HttpClient(handler);
        using var lookup = new SteamStoreLookup(http, TimeSpan.Zero);

        string? raw = await lookup.FetchRawAsync("1091500");

        Assert.Equal(fixture, raw);
        Assert.Equal(1, handler.Calls);
        Assert.Contains("appids=1091500", handler.LastUri);
        Assert.Contains("store.steampowered.com", handler.LastUri);
        Assert.NotNull(http.DefaultRequestHeaders.UserAgent);
        Assert.NotEmpty(http.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public async Task FetchRaw_ThenParse_ProducesRecord()
    {
        string fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "steam_appdetails_1091500.json"));
        var handler = new StubHandler(fixture);
        using var http = new HttpClient(handler);
        using var lookup = new SteamStoreLookup(http, TimeSpan.Zero);

        string? raw = await lookup.FetchRawAsync("1091500");
        var record = CommonMetadataParser.ToRecord(SteamStoreParser.Parse(raw!, "1091500")!, "id-1");

        Assert.Equal("CD PROJEKT RED", record!.Developer);
        Assert.Equal(86, record.MetacriticScore);
    }

    [Fact]
    public async Task FetchRaw_HttpFailure_ReturnsNull()
    {
        var handler = new StubHandler("nope", HttpStatusCode.InternalServerError);
        using var http = new HttpClient(handler);
        using var lookup = new SteamStoreLookup(http, TimeSpan.Zero);

        Assert.Null(await lookup.FetchRawAsync("1091500"));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            _status = status;
        }

        public int Calls { get; private set; }
        public string LastUri { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri?.ToString() ?? string.Empty;
            var response = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    }
}
