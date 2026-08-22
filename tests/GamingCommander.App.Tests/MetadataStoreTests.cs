using GamingCommander.App.Services.Metadata;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class MetadataStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sidecar;
    private readonly string _gamesJson;

    public MetadataStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MetaStore_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _sidecar = Path.Combine(_tempDir, "games_metadata.json");
        _gamesJson = Path.Combine(_tempDir, "games.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Get_MissingFile_ReturnsNull()
    {
        var store = new MetadataStore(_sidecar);
        Assert.Null(store.Get("any-id"));
        Assert.False(File.Exists(_sidecar));
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        var store = new MetadataStore(_sidecar);
        store.Upsert("known", new GameMetadataRecord { Developer = "A" });

        Assert.Null(store.Get("other"));
    }

    [Fact]
    public void Upsert_ThenGet_RoundTripsMergedFields()
    {
        var store = new MetadataStore(_sidecar);
        store.Upsert("g1", new GameMetadataRecord
        {
            Developer = "CD PROJEKT RED",
            Publisher = "CD PROJEKT RED",
            Genre = "RPG",
            MetacriticScore = 86,
            PcGamingWikiUrl = "https://www.pcgamingwiki.com/wiki/Cyberpunk_2077",
        });

        var loaded = new MetadataStore(_sidecar).Get("g1");
        Assert.NotNull(loaded);
        Assert.Equal("g1", loaded.GameEntryId);
        Assert.Equal("CD PROJEKT RED", loaded.Developer);
        Assert.Equal("RPG", loaded.Genre);
        Assert.Equal(86, loaded.MetacriticScore);
        Assert.True(loaded.HasDisplayableExtras);
    }

    [Fact]
    public void Upsert_DoesNotCreateOrTouchGamesJson()
    {
        File.WriteAllText(_gamesJson, "{\"roots\":[]}");
        string before = File.ReadAllText(_gamesJson);

        var store = new MetadataStore(_sidecar);
        store.Upsert("g1", new GameMetadataRecord { Developer = "X" });

        Assert.True(File.Exists(_sidecar));
        Assert.Equal(before, File.ReadAllText(_gamesJson));
    }

    [Fact]
    public void Get_CorruptSidecar_ReturnsNull()
    {
        File.WriteAllText(_sidecar, "not-json{{{");
        var store = new MetadataStore(_sidecar);
        Assert.Null(store.Get("g1"));
    }

    [Fact]
    public void HasDisplayableExtras_EmptyRecord_IsFalse()
    {
        Assert.False(new GameMetadataRecord().HasDisplayableExtras);
    }

    [Fact]
    public void Upsert_RoundTripsDetailsCatalog()
    {
        var store = new MetadataStore(_sidecar);
        store.Upsert("g1", new GameMetadataRecord
        {
            Developer = "CD PROJEKT RED",
            Details = new GameMetadataDetails
            {
                ConfigPaths =
                [
                    new GameMetadataPath
                    {
                        Kind = "config",
                        Os = "Windows",
                        Template = @"{{P|localappdata}}\CD Projekt Red\Cyberpunk 2077",
                    },
                ],
                CommandLine =
                [
                    new GameMetadataCommandLine
                    {
                        Argument = "--launcher-skip",
                        Notes = "skips the separate launcher",
                        Source = "fixbox",
                    },
                ],
            },
        });

        var loaded = new MetadataStore(_sidecar).Get("g1");
        Assert.NotNull(loaded?.Details);
        Assert.True(loaded.Details.HasAny);
        Assert.Equal("--launcher-skip", loaded.Details.CommandLine[0].Argument);
        Assert.Contains("{{P|localappdata}}", loaded.Details.ConfigPaths[0].Template);
        Assert.True(loaded.HasDisplayableExtras);
    }
}
