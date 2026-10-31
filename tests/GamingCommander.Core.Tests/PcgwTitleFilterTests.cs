using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class PcgwTitleFilterTests
{
    [Theory]
    [InlineData("Cyberpunk 2077 - Official Soundtrack")]
    [InlineData("Some Game Demo")]
    public void RejectsNoiseTitles(string title) =>
        Assert.True(PcgwTitleFilter.IsNoisy(title));

    [Theory]
    [InlineData("Democracy 3")]
    [InlineData("Bethesda Softworks")]
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
    public void PickBest_QueryElex_NotElexIi()
    {
        string? best = PcgwTitleFilter.PickBest(["ELEX II", "ELEX"], yearHint: null, query: "ELEX");
        Assert.Equal("ELEX", best);
    }

    [Theory]
    [InlineData(2026, "Dead Space (2023)")]
    [InlineData(2008, "Dead Space")]
    public void PickBest_YearHint_PrefersClosestYear(int yearHint, string expected)
    {
        string? title = PcgwTitleFilter.PickBest(
        [
            "Dead Space",
            "Dead Space (2023)",
            "Dead Space (remake)",
            "Dead Space 2",
        ], yearHint: yearHint);
        Assert.Equal(expected, title);
    }

    [Fact]
    public void Dedupe_DropsCaseOnlyDuplicates()
    {
        IReadOnlyList<string> titles = PcgwTitleFilter.Dedupe(
        [
            "Beneath a Steel Sky",
            "Beneath A Steel Sky",
            "Beneath a Steel Sky",
        ]);
        Assert.Single(titles);
        Assert.Equal("Beneath a Steel Sky", titles[0]);
    }

}
