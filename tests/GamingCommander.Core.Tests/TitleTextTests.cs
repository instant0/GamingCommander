using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class TitleTextTests
{
    [Theory]
    [InlineData("Dark Souls® III", "Dark Souls III")]
    [InlineData("Dead Space™ 3", "Dead Space 3")]
    [InlineData("Foo© Bar", "Foo Bar")]
    public void ForSearch_StripsMarks(string raw, string expected) =>
        Assert.Equal(expected, TitleText.ForSearch(raw));

    [Fact]
    public void LettersAndDigits_MatchesConcatenatedExe() =>
        Assert.Equal("darksoulsiii", TitleText.LettersAndDigits("Dark Souls® III"));

    [Fact]
    public void ExpandPacked_SplitsCamelCase() =>
        Assert.Equal("Deep Rock", TitleText.ExpandPacked("DeepRock"));

    [Fact]
    public void FromFolderName_ElexIi() =>
        Assert.Equal("elex II", TitleText.FromFolderName("elexII"));

    [Theory]
    [InlineData("elex", "elex")]
    [InlineData("elexII", "ELEX2")]
    [InlineData("ELEX", "ELEX")]
    public void MatchesFolderAndExe_GothicStyle(string folder, string exe) =>
        Assert.True(TitleText.MatchesFolderAndExe(folder, exe));

    [Fact]
    public void MatchesFolderAndExe_NotSystem() =>
        Assert.False(TitleText.MatchesFolderAndExe("elexII", "System"));

    [Fact]
    public void IsGenericLabel_System() =>
        Assert.True(TitleText.IsGenericLabel("System"));

    [Fact]
    public void SharesNameToken_RejectsSystemForElex() =>
        Assert.False(TitleText.SharesNameToken("System", "elexII"));

    [Fact]
    public void SearchQueries_IncludesFolderAndPacked()
    {
        IReadOnlyList<string> q = TitleText.SearchQueries("FSD", "deeprock", "Deep Rock Galactic");
        Assert.Contains("Deep Rock Galactic", q);
        Assert.Contains("deeprock", q);
    }
}
