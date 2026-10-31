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
        svc.Upsert(new Collection { Name = "RPGs", SmartQuery = "tag:RPG" });

        Collection? c = Create().Get("Favourites");
        Assert.NotNull(c);
        Assert.Equal(["a", "b"], c!.GameIds);
        Assert.False(c.IsSmart);

        Collection? smart = Create().Get("RPGs");
        Assert.NotNull(smart);
        Assert.True(smart!.IsSmart);
        Assert.Equal("tag:RPG", smart.SmartQuery);
    }

    [Fact]
    public void Remove_Works()
    {
        var svc = Create();
        svc.Upsert(new Collection { Name = "Temp" });
        Assert.True(svc.Remove("Temp"));
        Assert.Null(Create().Get("Temp"));
    }
}
