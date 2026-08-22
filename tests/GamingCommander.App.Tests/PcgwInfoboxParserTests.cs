using GamingCommander.App.Services.Metadata;

namespace GamingCommander.App.Tests;

public sealed class PcgwInfoboxParserTests
{
    [Fact]
    public void ParseHtmlTitle_StripsSiteSuffix()
    {
        string html = File.ReadAllText(Fixture("pcgw_appid.html"));
        Assert.Equal("Cyberpunk 2077", PcgwInfoboxParser.ParseHtmlTitle(html));
    }

    [Fact]
    public void ParseOpenSearch_FirstTitle()
    {
        string json = File.ReadAllText(Fixture("pcgw_opensearch.json"));
        Assert.Equal("Cyberpunk 2077", PcgwInfoboxParser.ParseOpenSearchFirstTitle(json));
    }

    [Fact]
    public void ParseSearchTitles_ReadsQueryList()
    {
        string json = """{"query":{"search":[{"title":"Prime World: Defenders"},{"title":"Prime World: Defenders 2"}]}}""";
        IReadOnlyList<string> titles = PcgwInfoboxParser.ParseSearchTitles(json);
        Assert.Equal("Prime World: Defenders", titles[0]);
        Assert.Equal("Prime World: Defenders 2", titles[1]);
    }

    [Fact]
    public void ParseOpenSearch_SkipsSoundtrackHit()
    {
        string json = """["q",["Cyberpunk 2077 Soundtrack","Cyberpunk 2077"],[],["u1","u2"]]""";
        Assert.Equal("Cyberpunk 2077", PcgwInfoboxParser.ParseOpenSearchFirstTitle(json));
    }

    [Fact]
    public void ParseWikitext_ExtractsInfoboxRows()
    {
        string wt = File.ReadAllText(Fixture("pcgw_infobox.wikitext"));
        PcgwFacts? facts = PcgwInfoboxParser.ParseWikitext(wt, "Cyberpunk 2077");

        Assert.NotNull(facts);
        Assert.Contains("CD Projekt Red", facts.Developers);
        Assert.Contains("CD Projekt", facts.Publishers);
        Assert.Contains("REDengine 4", facts.Engines);
        Assert.Contains("December 10, 2020", facts.ReleaseDates);
        Assert.Contains("RPG", facts.Genres);
        Assert.Contains("Open world", facts.Genres);
        Assert.Equal("https://www.pcgamingwiki.com/wiki/Cyberpunk_2077", facts.PageUrl);
    }

    [Fact]
    public void ParseApiResponse_ReadsWikitextStar()
    {
        string wt = File.ReadAllText(Fixture("pcgw_infobox.wikitext"));
        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            parse = new { title = "Cyberpunk 2077", wikitext = new Dictionary<string, string> { ["*"] = wt } },
        });

        PcgwFacts? facts = PcgwInfoboxParser.ParseApiResponse(json);
        Assert.NotNull(facts);
        Assert.Contains("CD Projekt Red", facts.Developers);
    }

    [Fact]
    public void CommonParser_MapsPcgwFacts()
    {
        string wt = File.ReadAllText(Fixture("pcgw_infobox.wikitext"));
        var record = CommonMetadataParser.ToRecord(PcgwInfoboxParser.ParseWikitext(wt, "Cyberpunk 2077")!, "id-1");

        Assert.NotNull(record);
        Assert.Equal("CD Projekt Red, Virtuos", record.Developer);
        Assert.Equal("REDengine 4", record.Engine);
        Assert.Equal("Pcgw", record.LastMetadataSource);
        Assert.Contains("wiki/Cyberpunk_2077", record.PcGamingWikiUrl);
    }

    [Fact]
    public void CommonParser_MapsSectionDetails()
    {
        string wt = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "pcgw", "cyberpunk_sections.wikitext"));
        var page = new PcgwLookupResult(
            PcgwInfoboxParser.ParseWikitext(File.ReadAllText(Fixture("pcgw_infobox.wikitext")), "Cyberpunk 2077"),
            PcgwSectionParser.ParseAll(wt));

        var record = CommonMetadataParser.ToRecord(page, "id-1");
        Assert.NotNull(record?.Details);
        Assert.Contains(record.Details.CommandLine, c => c.Argument == "--launcher-skip");
        Assert.Contains(record.Details.ConfigPaths, p => p.Os == "Windows");
        Assert.Equal("true", record.Details.CloudSync["steam cloud"]);
        Assert.Equal("1091500", record.SteamAppId);
    }

    [Fact]
    public void ParseWikitext_NoInfobox_ReturnsNull()
    {
        Assert.Null(PcgwInfoboxParser.ParseWikitext("just some text"));
    }

    private static string Fixture(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
