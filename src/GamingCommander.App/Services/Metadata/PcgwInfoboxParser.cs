using System.Text.Json;
using System.Text.RegularExpressions;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Parses PCGW HTML title, OpenSearch JSON, and Infobox wikitext.
/// No HTTP. Cargo is not used (denied on the live API).
/// </summary>
public static class PcgwInfoboxParser
{
    private static readonly Regex TitleTag = new(
        @"<title>([^<]+)</title>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DeveloperRow = new(
        @"\{\{Infobox game/row/developer\|(.+?)(?:\||\}\})", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex PublisherRow = new(
        @"\{\{Infobox game/row/publisher\|(.+?)(?:\||\}\})", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex EngineRow = new(
        @"\{\{Infobox game/row/engine\|(.+?)(?:\||\}\})", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex DateRow = new(
        @"\{\{Infobox game/row/date\|Windows\|(.+?)(?:\||\}\})", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex GenreRow = new(
        @"\{\{Infobox game/row/taxonomy/genres\|(.+?)(?:\||\}\})", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>Extracts the wiki page title from an <c>appid.php</c> HTML document.</summary>
    public static string? ParseHtmlTitle(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        Match m = TitleTag.Match(html);
        if (!m.Success)
            return null;

        string title = m.Groups[1].Value;
        int cut = title.IndexOf(" - PCGamingWiki", StringComparison.OrdinalIgnoreCase);
        if (cut > 0)
            title = title[..cut];
        title = title.Trim();
        return title.Length == 0 ? null : title;
    }

    /// <summary>OpenSearch titles in rank order (MediaWiki list: [query, titles, , urls]).</summary>
    public static IReadOnlyList<string> ParseOpenSearchTitles(string json)
    {
        var list = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 2)
                return list;

            JsonElement titles = doc.RootElement[1];
            if (titles.ValueKind != JsonValueKind.Array)
                return list;

            foreach (JsonElement item in titles.EnumerateArray())
            {
                string? title = item.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                    list.Add(title);
            }
        }
        catch (JsonException)
        {
            return list;
        }

        return list;
    }

    /// <summary>Best OpenSearch hit: skip noise, prefer <c>(YYYY)</c> near <paramref name="yearHint"/>.</summary>
    public static string? ParseOpenSearchFirstTitle(string json, int? yearHint = null) =>
        Core.Services.PcgwTitleFilter.PickBest(ParseOpenSearchTitles(json), yearHint);

    /// <summary>Raw wikitext from a Parse API envelope (same payload as Infobox parse).</summary>
    public static string? ExtractWikitext(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("parse", out JsonElement parse))
                return null;

            if (!parse.TryGetProperty("wikitext", out JsonElement wt))
                return null;

            string? wikitext = wt.ValueKind == JsonValueKind.Object && wt.TryGetProperty("*", out JsonElement star)
                ? star.GetString()
                : wt.GetString();
            return string.IsNullOrWhiteSpace(wikitext) ? null : wikitext;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Page title from a Parse API envelope.</summary>
    public static string? ExtractPageTitle(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("parse", out JsonElement parse))
                return null;

            string? title = parse.TryGetProperty("title", out JsonElement t) ? t.GetString() : null;
            return string.IsNullOrWhiteSpace(title) ? null : title;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Pulls wikitext from a Parse API envelope, then extracts Infobox facts.</summary>
    public static PcgwFacts? ParseApiResponse(string json)
    {
        string? wikitext = ExtractWikitext(json);
        if (wikitext is null)
            return null;

        return ParseWikitext(wikitext, ExtractPageTitle(json));
    }

    /// <summary>Extracts Infobox game rows from raw wikitext.</summary>
    public static PcgwFacts? ParseWikitext(string wikitext, string? pageTitle = null)
    {
        if (string.IsNullOrWhiteSpace(wikitext) || !wikitext.Contains("Infobox game", StringComparison.OrdinalIgnoreCase))
            return null;

        var developers = MatchList(DeveloperRow, wikitext);
        var publishers = MatchList(PublisherRow, wikitext);
        var engines = MatchList(EngineRow, wikitext);
        var dates = MatchList(DateRow, wikitext);
        var genres = new List<string>();
        foreach (Match m in GenreRow.Matches(wikitext))
        {
            foreach (string part in m.Groups[1].Value.Split(','))
            {
                string cleaned = CleanWikitext(part);
                if (cleaned.Length > 0)
                    genres.Add(cleaned);
            }
        }

        string? url = pageTitle is null
            ? null
            : "https://www.pcgamingwiki.com/wiki/" + pageTitle.Replace(' ', '_');

        return new PcgwFacts(pageTitle, url, developers, publishers, genres, engines, dates);
    }

    private static List<string> MatchList(Regex regex, string text)
    {
        var list = new List<string>();
        foreach (Match m in regex.Matches(text))
        {
            string cleaned = CleanWikitext(m.Groups[1].Value);
            if (cleaned.Length > 0)
                list.Add(cleaned);
        }

        return list;
    }

    private static string CleanWikitext(string text)
    {
        text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<ref[^>]*/>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<ref\b[^>]*>.*?</ref>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = Regex.Replace(text, @"\{\{Refurl\|.*?\}\}", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"\[\[[^|\]]*\|([^]]*)\]\]", "$1");
        text = Regex.Replace(text, @"\[\[([^]]*)\]\]", "$1");
        text = Regex.Replace(text, @"\{\{[^}]*\}\}", "");
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = Regex.Replace(text, @"\s+", " ").Trim().Trim(',', '|', ' ');
        return text;
    }
}
