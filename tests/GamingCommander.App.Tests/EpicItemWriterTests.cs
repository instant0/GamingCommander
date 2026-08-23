using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class EpicItemWriterTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ew_" + Guid.NewGuid().ToString("N")[..8]);

    public EpicItemWriterTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void TryWrite_FromMancpn()
    {
        string game = Path.Combine(_dir, "Orphan");
        string eg = Path.Combine(game, ".egstore");
        Directory.CreateDirectory(Path.Combine(eg));
        Directory.CreateDirectory(Path.Combine(game, "Binaries", "Win64"));
        File.WriteAllText(Path.Combine(eg, "AABBCCDD.mancpn"),
            """{"CatalogNamespace":"ns","CatalogItemId":"id","AppName":"app"}""");
        File.WriteAllBytes(Path.Combine(game, "Binaries", "Win64", "Orphan.exe"), new byte[8]);
        string manifests = Path.Combine(_dir, "Manifests");

        Assert.True(EpicItemWriter.TryWrite(game, manifests, out string path, out string err), err);
        Assert.True(File.Exists(path));
        string json = File.ReadAllText(path);
        Assert.Contains("\"CatalogItemId\": \"id\"", json);
        Assert.Contains("Orphan.exe", json);
    }
}
