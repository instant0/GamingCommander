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

    [Fact]
    public void TryWrite_FromOvtWhenMancpnMissing()
    {
        string game = Path.Combine(_dir, "DeathLike");
        string eg = Path.Combine(game, ".egstore", "d460fdcbec4e42f295473e94e96fda11");
        Directory.CreateDirectory(eg);
        Directory.CreateDirectory(Path.Combine(game, "Binaries", "Win64"));
        File.WriteAllBytes(Path.Combine(game, ".egstore", "0C1A9FF244DDA40D209294A46A2F36B6.manifest"), [1]);
        File.WriteAllBytes(Path.Combine(game, "Binaries", "Win64", "DeathStranding.exe"), new byte[8]);
        string payload = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    """{"sub":"d460fdcbec4e42f295473e94e96fda11","ent":[{"catalogItemId":"item1","namespace":"ns1"}]}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        File.WriteAllText(Path.Combine(eg, "token.ovt"),
            "{\"token\":\"egoc1~aaa." + payload + ".sig\"}");
        string manifests = Path.Combine(_dir, "Manifests2");
        Assert.True(EpicItemWriter.TryWrite(game, manifests, out string path, out string err), err);
        string json = File.ReadAllText(path);
        Assert.Contains("item1", json);
        Assert.Contains("ns1", json);
        Assert.Contains("0C1A9FF244DDA40D209294A46A2F36B6", json);
    }
}
