using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class PcgwTitleFilterTests
{
    [Theory]
    [InlineData("Cyberpunk 2077 Soundtrack")]
    [InlineData("Cyberpunk 2077 - Official Soundtrack")]
    [InlineData("Some Game Demo")]
    [InlineData("Foo (disambiguation)")]
    [InlineData("Tomb Raider - The Final Hours Digital Book")]
    public void RejectsNoiseTitles(string title) =>
        Assert.True(PcgwTitleFilter.IsNoisy(title));

    [Theory]
    [InlineData("Cyberpunk 2077")]
    [InlineData("Democracy 3")]
    [InlineData("Bethesda Softworks")]
    [InlineData("Tomb Raider (2013)")]
    public void KeepsRealTitles(string title) =>
        Assert.False(PcgwTitleFilter.IsNoisy(title));

    [Fact]
    public void FirstClean_SkipsSoundtrack()
    {
        string? title = PcgwTitleFilter.FirstClean(
        [
            "Cyberpunk 2077 Soundtrack",
            "Cyberpunk 2077",
        ]);
        Assert.Equal("Cyberpunk 2077", title);
    }
}
