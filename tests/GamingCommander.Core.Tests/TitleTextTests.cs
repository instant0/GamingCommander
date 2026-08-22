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
}
