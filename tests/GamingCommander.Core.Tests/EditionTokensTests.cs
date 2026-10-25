using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Pin test: the two caller sets are deliberately different. Adding or renaming an
/// edition token should be a one-place change in <see cref="EditionTokens"/>, and this
/// test makes the resulting behavior change visible.
/// </summary>
public sealed class EditionTokensTests
{
    [Fact]
    public void DisplayTokens_AreTheDeliberateDisplaySet() =>
        Assert.Equal(
            [
                "Remastered",
                "Definitive Edition",
                "Enhanced Edition",
                "Ultimate Edition",
                "Special Edition",
                "GOTY",
                "Edition",
            ],
            EditionTokens.DisplayTokens);

    [Fact]
    public void TailTokens_AreTheDeliberateTailSet() =>
        Assert.Equal(
            [
                "Reloaded",
                "Definitive",
                "Complete",
                "Enhanced",
                "Ultimate",
                "Deluxe",
                "Gold",
                "Anniversary",
                "Game of the Year",
                "GOTY",
            ],
            EditionTokens.TailTokens);
}
