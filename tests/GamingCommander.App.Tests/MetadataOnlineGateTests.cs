using System.Net;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class MetadataOnlineGateTests
{
    [Fact]
    public async Task ProbeOnce_Success_IsOnline()
    {
        var gate = new MetadataOnlineGate();
        gate.SetChecking();
        using var http = new HttpClient(new StubHandler(HttpStatusCode.OK));
        await gate.ProbeOnceAsync(http);
        Assert.True(gate.AllowsHttp);
        Assert.Equal("Online", gate.StatusLabel);
    }

    [Fact]
    public async Task ProbeOnce_AnyHttpStatus_IsOnline()
    {
        var gate = new MetadataOnlineGate();
        gate.SetChecking();
        using var http = new HttpClient(new StubHandler(HttpStatusCode.Forbidden));
        await gate.ProbeOnceAsync(http);
        Assert.True(gate.AllowsHttp);
    }

    [Fact]
    public async Task ProbeOnce_NoResponse_IsOfflineSticky()
    {
        var gate = new MetadataOnlineGate();
        gate.SetChecking();
        using var http = new HttpClient(new ThrowHandler());
        await gate.ProbeOnceAsync(http);
        Assert.False(gate.AllowsHttp);
        Assert.Equal("Offline", gate.StatusLabel);

        using var http2 = new HttpClient(new StubHandler(HttpStatusCode.OK));
        await gate.ProbeOnceAsync(http2);
        Assert.False(gate.AllowsHttp);
    }

    [Fact]
    public async Task ConfigFlagOff_DoesNotProbe()
    {
        var config = new StubConfig(false);
        var gate = new MetadataOnlineGate(config);
        gate.SetChecking();
        var handler = new StubHandler(HttpStatusCode.OK);
        using var http = new HttpClient(handler);
        await gate.ProbeOnceAsync(http);
        Assert.Equal(0, handler.Calls);
        Assert.Equal(MetadataOnlineKind.Disabled, gate.Kind);
    }

    [Fact]
    public async Task Disabled_DoesNotProbe()
    {
        var gate = new MetadataOnlineGate();
        gate.SetDisabled();
        var handler = new StubHandler(HttpStatusCode.OK);
        using var http = new HttpClient(handler);
        await gate.ProbeOnceAsync(http);
        Assert.Equal(0, handler.Calls);
        Assert.Equal("Lookup Disabled", gate.StatusLabel);
    }

    [Fact]
    public void ReportFailure_StopsHttp()
    {
        var gate = new MetadataOnlineGate();
        gate.SetChecking();
        gate.ReportFailure();
        Assert.False(gate.AllowsHttp);
        Assert.Equal(MetadataOnlineKind.Offline, gate.Kind);
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        public int Calls { get; private set; }

        public StubHandler(HttpStatusCode code) => _code = code;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(_code));
        }
    }

    private sealed class StubConfig : IConfigService
    {
        private readonly AppConfig _config;

        public StubConfig(bool enable) =>
            _config = new AppConfig([], [], [], IsFirstRun: false, EnableOnlineMetadata: enable);

        public AppConfig Load() => _config;
        public void Save(AppConfig config) { }
    }
}
