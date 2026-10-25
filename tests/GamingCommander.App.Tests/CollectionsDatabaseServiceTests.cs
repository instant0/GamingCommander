using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>Tests for CollectionsDatabaseService (Plan 127 B12).</summary>
public sealed class CollectionsDatabaseServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "col_" + Guid.NewGuid().ToString("N")[..8]);

    public CollectionsDatabaseServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    private CollectionsDatabaseService Create() => new(Path.Combine(_dir, "collections.json"));

    [Fact]
    public void Upsert_Get_RoundTrip()
    {
        var svc = Create();
        svc.Upsert(new Collection { Name = "Favourites", GameIds = ["a", "b"] });

        Collection? c = Create().Get("Favourites");
        Assert.NotNull(c);
        Assert.Equal(["a", "b"], c!.GameIds);
        Assert.False(c.IsSmart);
    }

    [Fact]
    public void SmartCollection_IsSmart()
    {
        var svc = Create();
        svc.Upsert(new Collection { Name = "RPGs", SmartQuery = "tag:RPG" });

        Collection? c = Create().Get("RPGs");
        Assert.True(c!.IsSmart);
        Assert.Equal("tag:RPG", c.SmartQuery);
    }

    [Fact]
    public void Remove_Works()
    {
        var svc = Create();
        svc.Upsert(new Collection { Name = "Temp" });
        Assert.True(svc.Remove("Temp"));
        Assert.Null(Create().Get("Temp"));
    }

    [Fact]
    public void FutureSchemaVersion_ReturnsEmpty()
    {
        string path = Path.Combine(_dir, "collections.json");
        File.WriteAllText(path, $$"""{ "Version": {{SchemaVersioning.Current + 1}}, "Collections": {} }""");
        var svc = new CollectionsDatabaseService(path);

        Assert.Empty(svc.Collections);
    }
}
