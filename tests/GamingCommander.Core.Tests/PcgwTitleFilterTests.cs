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

    [Fact]
    public void PickBest_YearHint_PrefersRemakeOverOriginal()
    {
        string? title = PcgwTitleFilter.PickBest(
        [
            "Dead Space",
            "Dead Space (2023)",
            "Dead Space (remake)",
            "Dead Space 2",
        ], yearHint: 2026);
        Assert.Equal("Dead Space (2023)", title);
    }

    [Fact]
    public void PickBest_YearHint2008_KeepsOriginal()
    {
        string? title = PcgwTitleFilter.PickBest(
        [
            "Dead Space",
            "Dead Space (2023)",
        ], yearHint: 2008);
        Assert.Equal("Dead Space", title);
    }
}
