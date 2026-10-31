using System.Buffers.Binary;
using System.Text;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class EpicInventoryReaderTests
{
    [Fact]
    public void CatalogItem_ReturnsEntryWithMetadata()
    {
        using var temp = new TempDirectory();
        string manifests = temp.CreateDirectory("manifests");
        string install = temp.CreateDirectory("Games", "Epic Game");
        string exe = temp.CreateFile(
            Path.Combine("Games", "Epic Game", "Binaries", "Win64", "Game.exe"), "exe");
        string completeManifest = @"C:\ProgramData\Epic\Game\GUID.manifest";
        temp.CreateFile(
            Path.Combine("manifests", "game.item"),
            ItemJson(install, "Binaries/Win64/Game.exe",
                installSize: 12345, appVersion: "1.2.3", guid: "GUID",
                completeManifestPath: completeManifest));

        StoreInventoryEntry entry = new EpicInventoryReader(manifests).Read().Single();

        Assert.Equal(GameSourceKind.Epic, entry.Kind);
        Assert.Equal("catalog-id", entry.StoreId);
        Assert.Equal("Epic Game", entry.Name);
        Assert.Equal(install, entry.InstallPath);
        Assert.Equal(exe, entry.LaunchExe);
        Assert.True(entry.FolderExists);
        Assert.True(entry.ExeExists);
        Assert.Equal(StoreReportedState.Installed, entry.StoreState);

        var metadata = Assert.IsType<EpicInventoryMetadata>(entry.Metadata);
        Assert.Equal(12345, metadata.InstallSize);
        Assert.Equal("1.2.3", metadata.AppVersionString);
        Assert.Equal("GUID", metadata.InstallationGuid);
        Assert.Equal(completeManifest, metadata.CompleteManifestPath);
        Assert.True(metadata.IsCatalogItem);
    }

    [Fact]
    public void CatalogItemMissingFolder_StillReturnsEntry()
    {
        using var temp = new TempDirectory();
        string manifests = temp.CreateDirectory("manifests");
        string missing = temp.Combine("missing");
        temp.CreateFile(
            Path.Combine("manifests", "game.item"),
            ItemJson(missing, "Game.exe"));

        StoreInventoryEntry entry = new EpicInventoryReader(manifests).Read().Single();

        Assert.Equal("catalog-id", entry.StoreId);
        Assert.False(entry.FolderExists);
        Assert.False(entry.ExeExists);
        Assert.Null(entry.LaunchExe);
        Assert.Equal(StoreReportedState.Unknown, entry.StoreState);
    }

    [Fact]
    public void CorruptItem_IsSkipped()
    {
        using var temp = new TempDirectory();
        string manifests = temp.CreateDirectory("manifests");
        temp.CreateFile(Path.Combine("manifests", "bad.item"), "{ not json");
        temp.CreateFile(Path.Combine("manifests", "good.item"), ItemJson(temp.Root, "Game.exe"));

        IReadOnlyList<StoreInventoryEntry> entries = new EpicInventoryReader(manifests).Read();

        Assert.Single(entries);
        Assert.Equal("catalog-id", entries[0].StoreId);
    }

    [Fact]
    public void MancpnFallback_WhenNoGlobalItem()
    {
        using var temp = new TempDirectory();
        string games = temp.CreateDirectory("Games");
        string game = temp.CreateDirectory("Games", "Orphan Game");
        string mancpn = temp.CreateFile(
            Path.Combine("Games", "Orphan Game", ".egstore", "game.mancpn"),
            MancpnJson());

        StoreInventoryEntry entry = new EpicInventoryReader(
            temp.Combine("missing-manifests"), [games]).Read().Single();

        Assert.Equal("local-id", entry.StoreId);
        Assert.Equal("Orphan Game", entry.Name);
        Assert.Equal(game, entry.InstallPath);
        Assert.True(entry.FolderExists);
        Assert.Null(entry.LaunchExe);
        var metadata = Assert.IsType<EpicInventoryMetadata>(entry.Metadata);
        Assert.False(metadata.IsCatalogItem);
        Assert.Equal(mancpn, metadata.MancpnPath);
        Assert.Null(metadata.ItemPath);
    }

    [Fact]
    public void BinaryManifestFallback_SuppliesExeAndVersion()
    {
        using var temp = new TempDirectory();
        string games = temp.CreateDirectory("Games");
        string game = temp.CreateDirectory("Games", "Binary Game");
        string exe = temp.CreateFile(Path.Combine("Games", "Binary Game", "bin", "Game.exe"), "exe");
        string manifest = temp.CreateFile(
            Path.Combine("Games", "Binary Game", ".egstore", "GUID.manifest"),
            BinaryManifest("BinaryApp", "9.9.9", "bin/Game.exe"));
        temp.CreateFile(
            Path.Combine("Games", "Binary Game", ".egstore", "game.mancpn"),
            MancpnJson(app: "LocalApp"));

        StoreInventoryEntry entry = new EpicInventoryReader(
            temp.Combine("missing-manifests"), [games]).Read().Single();

        Assert.Equal(exe, entry.LaunchExe);
        Assert.True(entry.ExeExists);
        var metadata = Assert.IsType<EpicInventoryMetadata>(entry.Metadata);
        Assert.Equal("GUID", metadata.InstallationGuid);
        Assert.Equal("9.9.9", metadata.AppVersionString);
        Assert.Equal(manifest, metadata.CompleteManifestPath);
        Assert.Equal("LocalApp", metadata.AppName);
    }

    [Fact]
    public void Orphan_DoesNotDuplicateCatalogClaim()
    {
        using var temp = new TempDirectory();
        string manifests = temp.CreateDirectory("manifests");
        string games = temp.CreateDirectory("Games");
        string game = temp.CreateDirectory("Games", "Claimed");
        temp.CreateDirectory("Games", "Claimed", ".egstore");
        temp.CreateFile(Path.Combine("manifests", "game.item"), ItemJson(game, "Game.exe"));

        IReadOnlyList<StoreInventoryEntry> entries =
            new EpicInventoryReader(manifests, [games]).Read();

        Assert.Single(entries);
        Assert.True(Assert.IsType<EpicInventoryMetadata>(entries[0].Metadata).IsCatalogItem);
    }

    [Fact]
    public void DuplicateCatalogInstall_YieldsOneEntry()
    {
        using var temp = new TempDirectory();
        string manifests = temp.CreateDirectory("manifests");
        string install = temp.CreateDirectory("Game");
        temp.CreateFile(Path.Combine("manifests", "one.item"),
            ItemJson(install, "Game.exe", catalogItemId: "one"));
        temp.CreateFile(Path.Combine("manifests", "two.item"),
            ItemJson(install, "Game.exe", catalogItemId: "two"));

        IReadOnlyList<StoreInventoryEntry> entries = new EpicInventoryReader(manifests).Read();

        Assert.Single(entries);
        Assert.Contains(entries[0].StoreId, new[] { "one", "two" });
    }

    [Fact]
    public void NoManifestsNoOrphans_ReturnsEmpty()
    {
        using var temp = new TempDirectory();

        Assert.Empty(new EpicInventoryReader(temp.Combine("none"), []).Read());
    }

    // ── helpers ────────────────────────────────────────────────────

    private static string ItemJson(
        string installLocation,
        string launchExecutable,
        string displayName = "Epic Game",
        string catalogItemId = "catalog-id",
        long? installSize = null,
        string? appVersion = null,
        string? guid = null,
        string? completeManifestPath = null)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append($"\"DisplayName\":\"{Esc(displayName)}\",");
        sb.Append($"\"InstallLocation\":\"{Esc(installLocation)}\",");
        sb.Append($"\"LaunchExecutable\":\"{Esc(launchExecutable)}\",");
        sb.Append("\"CatalogNamespace\":\"ns\",");
        sb.Append($"\"CatalogItemId\":\"{catalogItemId}\",");
        sb.Append("\"AppName\":\"App\"");
        if (installSize is not null)
            sb.Append($",\"InstallSize\":{installSize}");
        if (appVersion is not null)
            sb.Append($",\"AppVersionString\":\"{Esc(appVersion)}\"");
        if (guid is not null)
            sb.Append($",\"InstallationGuid\":\"{Esc(guid)}\"");
        if (completeManifestPath is not null)
            sb.Append($",\"CompleteManifestPath\":\"{Esc(completeManifestPath)}\"");
        sb.Append('}');
        return sb.ToString();
    }

    private static string MancpnJson(
        string id = "local-id",
        string ns = "local-ns",
        string app = "LocalApp") =>
        $"{{\"CatalogNamespace\":\"{ns}\",\"CatalogItemId\":\"{id}\",\"AppName\":\"{app}\"}}";

    private static string Esc(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Builds the uncompressed Epic binary manifest header the reader understands.</summary>
    private static byte[] BinaryManifest(string appName, string build, string launchExe)
    {
        var body = new List<byte>(new byte[14]);
        AddFString(body, appName);
        AddFString(body, build);
        AddFString(body, launchExe);

        byte[] raw = new byte[41 + body.Count];
        raw[36] = 0; // stored uncompressed
        body.CopyTo(raw, 41);
        return raw;
    }

    private static void AddFString(List<byte> body, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] size = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(size, bytes.Length + 1);
        body.AddRange(size);
        body.AddRange(bytes);
        body.Add(0);
    }
}
