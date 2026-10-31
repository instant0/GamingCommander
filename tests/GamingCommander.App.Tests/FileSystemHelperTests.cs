using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class FileSystemHelperTests
{
    [Theory]
    [InlineData("Divinity Original Sin 2 - Definitive Edition", "Divinity Original Sin 2")]
    [InlineData("The Witcher 3 GOTY", "The Witcher 3")]
    public void NormalizeDisplayName_StripsEditionTokensAndSeparators(string folder, string expected)
    {
        // The full token list is pinned by EditionTokensTests; these rows cover
        // compound token+separator, single token, and the no-op case.
        Assert.Equal(expected, FileSystemHelper.NormalizeDisplayName(folder));
    }
}
