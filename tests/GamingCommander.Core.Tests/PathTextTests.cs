using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class PathTextTests
{
    [Theory]
    [InlineData(@"C:\Games\Test\", @"C:\Games\Test")]
    [InlineData("C:/Games/Test/", @"C:\Games\Test")]
    [InlineData("  C:/Games/Test  ", @"C:\Games\Test")]
    [InlineData(@"C:\", @"C:\")]
    [InlineData("C:", @"C:\")]
    public void NormalizeForCompare_UnifiesSeparators(string input, string expected)
    {
        Assert.Equal(expected, PathText.NormalizeForCompare(input));
    }

    [Fact]
    public void NormalizeForCompare_PreservesCase()
    {
        Assert.Equal(@"C:\Games\Test", PathText.NormalizeForCompare(@"C:\Games\Test"));
    }

    [Fact]
    public void Equals_IgnoresSeparatorsAndCase()
    {
        Assert.True(PathText.Equals(@"D:\Games\Fortnite", "d:/games/fortnite/"));
        Assert.False(PathText.Equals(@"D:\Games\Fortnite", @"E:\Games\Fortnite"));
        Assert.False(PathText.Equals(null, @"D:\Games"));
    }

    [Theory]
    [InlineData(@"C:\Games\X", @"C:\Games", true)]
    [InlineData("C:/Games/X", @"C:\Games", true)]
    [InlineData(@"C:\Games\X\", "c:/games", true)]
    [InlineData(@"C:\Games2", @"C:\Games", false)]
    [InlineData(@"C:\Games", @"C:\Games", false)]
    [InlineData(@"C:\Games", @"C:\Games\X", false)]
    [InlineData(@"D:\Games\X", @"C:\Games", false)]
    [InlineData(@"C:\Games\X", "", false)]
    public void IsUnder_IsSegmentAware(string child, string parent, bool expected)
    {
        Assert.Equal(expected, PathText.IsUnder(child, parent));
    }

    [Theory]
    [InlineData(@"D:\Games\Fortnite\", "Fortnite")]
    [InlineData("D:/Games/Fortnite", "Fortnite")]
    [InlineData(@"C:\", "")]
    [InlineData(null, "")]
    public void Leaf_ReturnsLastFolderName(string? path, string expected)
    {
        Assert.Equal(expected, PathText.Leaf(path));
    }
}
