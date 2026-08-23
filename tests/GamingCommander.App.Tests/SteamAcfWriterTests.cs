using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class SteamAcfWriterTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "acf_" + Guid.NewGuid().ToString("N")[..8]);

    public SteamAcfWriterTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void Build_RoundTripsThroughParser()
    {
        string text = SteamAcfWriter.Build("222750", "Wargame: AirLand Battle", "Wargame Airland Battle");
        string file = Path.Combine(_dir, "appmanifest_222750.acf");
        File.WriteAllText(file, text);

        AcfInfo? parsed = SteamAcfParser.ParseAcfFile(file, _dir);
        Assert.NotNull(parsed);
        Assert.Equal("222750", parsed.AppId);
        Assert.Equal("Wargame: AirLand Battle", parsed.Name);
        Assert.Equal("Wargame Airland Battle", parsed.Installdir);
        Assert.Equal("4", parsed.StateFlags);
    }

    [Fact]
    public void TryWrite_CreatesFileOnce()
    {
        Assert.True(SteamAcfWriter.TryWrite(_dir, "10", "Game", "GameFolder", out string path, out string error));
        Assert.True(File.Exists(path));
        Assert.Empty(error);
        Assert.False(SteamAcfWriter.TryWrite(_dir, "10", "Game", "GameFolder", out _, out string again));
        Assert.Equal("ACF already exists.", again);
    }

    [Fact]
    public void IsAppId_RejectsJunk()
    {
        Assert.False(SteamAcfWriter.IsAppId(""));
        Assert.False(SteamAcfWriter.IsAppId("abc"));
        Assert.True(SteamAcfWriter.IsAppId("222750"));
    }
}
