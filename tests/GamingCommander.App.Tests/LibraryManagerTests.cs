using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public class LibraryManagerTests
{
    [Theory]
    [InlineData(@"d:\games\blizzard", @"d:\games", true)]   // child is inside parent
    [InlineData(@"d:\games\blizzard\", @"d:\games\", true)]  // trailing separators
    [InlineData(@"d:\games\blizzard", @"d:\games\", true)]   // mixed separators
    [InlineData(@"d:\games\blizzard", @"d:\other", false)]   // different trees
    [InlineData(@"d:\games", @"d:\games", false)]            // exact match is not child
    [InlineData(@"d:\games2", @"d:\games", false)]           // partial name match
    [InlineData(@"d:\games\blizzard", @"d:\games\blizzard\diablo", false)] // parent is longer
    [InlineData(@"D:\GAMES\BLIZZARD", @"d:\games", true)]    // case-insensitive
    public void IsChildOf_ReturnsExpected(string child, string parent, bool expected)
    {
        Assert.Equal(expected, LibraryManager.IsChildOf(child, parent));
    }
}
