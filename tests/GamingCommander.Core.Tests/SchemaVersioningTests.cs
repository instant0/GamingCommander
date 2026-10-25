using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class SchemaVersioningTests
{
    [Fact]
    public void CurrentVersionIsReadable() =>
        Assert.True(SchemaVersioning.IsReadable(SchemaVersioning.Current));

    [Fact]
    public void OlderVersionIsReadable() =>
        Assert.True(SchemaVersioning.IsReadable(SchemaVersioning.Current - 1));

    [Fact]
    public void FutureVersionIsNotReadable() =>
        Assert.False(SchemaVersioning.IsReadable(SchemaVersioning.Current + 1));
}
