using GamingCommander.App.Services.Metadata;

namespace GamingCommander.App.Tests;

public sealed class SteamStoreParserTests
{
    [Fact]
    public void Parse_SuccessFixture_MapsSourceFields()
    {
        string json = File.ReadAllText(Fixture("steam_appdetails_1091500.json"));
        SteamStoreFacts? facts = SteamStoreParser.Parse(json, "1091500");

        Assert.NotNull(facts);
        Assert.True(facts.Success);
        Assert.Equal("Cyberpunk 2077", facts.Name);
        Assert.Equal(["CD PROJEKT RED"], facts.Developers);
        Assert.Equal(["CD PROJEKT RED"], facts.Publishers);
        Assert.Equal("9 Dec, 2020", facts.ReleaseDate);
        Assert.Equal(["RPG"], facts.Genres);
        Assert.Equal(86, facts.MetacriticScore);
        Assert.Equal("1091500", facts.SteamAppId);
        Assert.Contains("cyberpunk.net", facts.Website);
        Assert.False(string.IsNullOrWhiteSpace(facts.ShortDescription));
    }

    [Fact]
    public void Parse_SuccessFalse_ReturnsUnsuccessfulFacts()
    {
        string json = File.ReadAllText(Fixture("steam_appdetails_fail.json"));
        SteamStoreFacts? facts = SteamStoreParser.Parse(json, "480");

        Assert.NotNull(facts);
        Assert.False(facts.Success);
        Assert.Null(facts.Name);
    }

    [Fact]
    public void Parse_WrongAppIdKey_ReturnsNull()
    {
        string json = File.ReadAllText(Fixture("steam_appdetails_1091500.json"));
        Assert.Null(SteamStoreParser.Parse(json, "271590"));
    }

    [Fact]
    public void Parse_CorruptJson_ReturnsNull()
    {
        Assert.Null(SteamStoreParser.Parse("not-json{{{", "1091500"));
    }

    [Fact]
    public void CommonParser_JoinsListsAndKeepsLocaleDate()
    {
        string json = File.ReadAllText(Fixture("steam_appdetails_1091500.json"));
        SteamStoreFacts facts = SteamStoreParser.Parse(json, "1091500")!;
        var record = CommonMetadataParser.ToRecord(facts, "entry-1");

        Assert.NotNull(record);
        Assert.Equal("entry-1", record.GameEntryId);
        Assert.Equal("CD PROJEKT RED", record.Developer);
        Assert.Equal("RPG", record.Genre);
        Assert.Equal("9 Dec, 2020", record.ReleaseDate);
        Assert.Equal(86, record.MetacriticScore);
        Assert.Equal("Steam", record.LastMetadataSource);
        Assert.Null(record.Engine);
        Assert.True(record.HasDisplayableExtras);
    }

    [Fact]
    public void CommonParser_UnsuccessfulFacts_ReturnsNull()
    {
        var facts = new SteamStoreFacts(false, "X", [], [], null, [], null, null, "1", null, null);
        Assert.Null(CommonMetadataParser.ToRecord(facts));
    }

    [Fact]
    public void CommonParser_StripCompanyPrefix()
    {
        Assert.Equal("ROCKFISH Games", CommonMetadataParser.StripCompanyPrefix("Company:ROCKFISH Games"));
        Assert.Null(CommonMetadataParser.StripCompanyPrefix("  "));
    }

    private static string Fixture(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
