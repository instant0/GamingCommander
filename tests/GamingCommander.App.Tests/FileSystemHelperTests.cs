using GamingCommander.App.Services;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

public sealed class FileSystemHelperTests
{
    public static IEnumerable<object[]> DisplayTokens() =>
        EditionTokens.DisplayTokens.Select(token => new object[] { token });

    [Theory]
    [MemberData(nameof(DisplayTokens))]
    public void NormalizeDisplayName_RemovesEachDisplayToken(string token) =>
        Assert.Equal("Dying Light 2", FileSystemHelper.NormalizeDisplayName($"Dying Light 2 {token}"));

    [Theory]
    [InlineData("Hollow_Knight", "Hollow Knight")]
    [InlineData("Dark Souls Remastered", "Dark Souls")]
    [InlineData("The Witcher 3 GOTY", "The Witcher 3")]
    [InlineData("Civilization VI Special Edition", "Civilization VI")]
    [InlineData("Divinity Original Sin 2 - Definitive Edition", "Divinity Original Sin 2")]
    [InlineData("Deep Rock Galactic", "Deep Rock Galactic")]
    public void NormalizeDisplayName_ExistingBehaviorUnchanged(string folder, string expected) =>
        Assert.Equal(expected, FileSystemHelper.NormalizeDisplayName(folder));
}
