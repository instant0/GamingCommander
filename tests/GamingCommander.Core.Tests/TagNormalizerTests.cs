using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public class TagNormalizerTests
{
    // ════════════════════════════════════════════════════════════════
    //  Normalize
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("RPG", "RPG")]
    [InlineData("  RPG  ", "RPG")]
    [InlineData("RPG  Co-op", "RPG Co-op")]
    [InlineData("  RPG    Co-op  ", "RPG Co-op")]
    [InlineData("RPG\tCo-op", "RPG Co-op")]
    [InlineData("RPG\nCo-op", "RPG Co-op")]
    [InlineData("RPG\r\nCo-op", "RPG Co-op")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void Normalize_TrimsAndCollapsesWhitespace(string? input, string expected)
    {
        string result = TagNormalizer.Normalize(input ?? string.Empty);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_PreservesCasing()
    {
        Assert.Equal("rpg", TagNormalizer.Normalize("rpg"));
        Assert.Equal("Co-op", TagNormalizer.Normalize("Co-op"));
    }

    // ════════════════════════════════════════════════════════════════
    //  AreEquivalent
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("RPG", "rpg", true)]
    [InlineData("Co-op", "co-op", true)]
    [InlineData("RPG", "RPG", true)]
    [InlineData("RPG", "Action", false)]
    [InlineData("RPG", "", false)]
    public void AreEquivalent_CaseInsensitiveComparison(string tag1, string tag2, bool expected)
    {
        Assert.Equal(expected, TagNormalizer.AreEquivalent(tag1, tag2));
    }

    [Fact]
    public void AreEquivalent_NormalizesWhitespace()
    {
        Assert.True(TagNormalizer.AreEquivalent("  RPG  ", "RPG"));
        Assert.True(TagNormalizer.AreEquivalent("RPG  Co-op", "RPG Co-op"));
    }

    // ════════════════════════════════════════════════════════════════
    //  AddDistinct
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void AddDistinct_AddsNewTag()
    {
        var tags = new List<string> { "RPG" };
        var result = TagNormalizer.AddDistinct(tags, "Co-op");

        Assert.Equal(2, result.Count);
        Assert.Contains("RPG", result);
        Assert.Contains("Co-op", result);
    }

    [Fact]
    public void AddDistinct_SkipsDuplicate_CaseInsensitive()
    {
        var tags = new List<string> { "RPG" };
        var result = TagNormalizer.AddDistinct(tags, "rpg");

        Assert.Single(result);
        Assert.Equal("RPG", result[0]);
    }

    [Fact]
    public void AddDistinct_SkipsEmptyTag()
    {
        var tags = new List<string> { "RPG" };
        var result = TagNormalizer.AddDistinct(tags, "");

        Assert.Single(result);
    }

    [Fact]
    public void AddDistinct_SkipsWhitespaceOnlyTag()
    {
        var tags = new List<string> { "RPG" };
        var result = TagNormalizer.AddDistinct(tags, "   ");

        Assert.Single(result);
    }

    [Fact]
    public void AddDistinct_NormalizesAddedTag()
    {
        var tags = new List<string>();
        var result = TagNormalizer.AddDistinct(tags, "  Co-op  ");

        Assert.Single(result);
        Assert.Equal("Co-op", result[0]);
    }

    // ════════════════════════════════════════════════════════════════
    //  ParseFromCommaSeparated
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseFromCommaSeparated_SingleTag()
    {
        var result = TagNormalizer.ParseFromCommaSeparated("RPG");
        Assert.Single(result);
        Assert.Equal("RPG", result[0]);
    }

    [Fact]
    public void ParseFromCommaSeparated_MultipleTags()
    {
        var result = TagNormalizer.ParseFromCommaSeparated("RPG, Co-op, Story Rich");
        Assert.Equal(3, result.Count);
        Assert.Equal("RPG", result[0]);
        Assert.Equal("Co-op", result[1]);
        Assert.Equal("Story Rich", result[2]);
    }

    [Fact]
    public void ParseFromCommaSeparated_Deduplicates()
    {
        var result = TagNormalizer.ParseFromCommaSeparated("RPG, rpg, RPG");
        Assert.Single(result);
        Assert.Equal("RPG", result[0]);
    }

    [Fact]
    public void ParseFromCommaSeparated_EmptyString()
    {
        var result = TagNormalizer.ParseFromCommaSeparated("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFromCommaSeparated_WhitespaceOnly()
    {
        var result = TagNormalizer.ParseFromCommaSeparated("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFromCommaSeparated_SkipsEmptyEntries()
    {
        var result = TagNormalizer.ParseFromCommaSeparated("RPG,,Co-op,");
        Assert.Equal(2, result.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  ToCommaSeparated
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ToCommaSeparated_EmptyList()
    {
        Assert.Equal("", TagNormalizer.ToCommaSeparated([]));
    }

    [Fact]
    public void ToCommaSeparated_SingleTag()
    {
        Assert.Equal("RPG", TagNormalizer.ToCommaSeparated(["RPG"]));
    }

    [Fact]
    public void ToCommaSeparated_MultipleTags()
    {
        Assert.Equal("RPG, Co-op, Story Rich", TagNormalizer.ToCommaSeparated(["RPG", "Co-op", "Story Rich"]));
    }

    [Fact]
    public void FromMetadata_SplitsGenreAndKeepsEngine()
    {
        List<string> tags = TagNormalizer.FromMetadata("Action, Adventure, Open world", "Anvil");
        Assert.Equal(["Action", "Adventure", "Open world", "Anvil"], tags);
    }

    [Fact]
    public void Merge_UserFirst_SkipsDuplicateGenre()
    {
        List<string> merged = TagNormalizer.Merge(["RPG", "Co-op"], ["Action", "rpg"]);
        Assert.Equal(["RPG", "Co-op", "Action"], merged);
    }
}
