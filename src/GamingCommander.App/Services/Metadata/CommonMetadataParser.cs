using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Maps source facts onto <see cref="GameMetadataRecord"/>. No HTTP.
/// Steam locale dates are kept as-is (not parsed as DateTime).
/// </summary>
public static class CommonMetadataParser
{
    /// <summary>Normalize Steam Store facts. Returns null when <see cref="SteamStoreFacts.Success"/> is false.</summary>
    public static GameMetadataRecord? ToRecord(SteamStoreFacts facts, string gameEntryId = "")
    {
        if (!facts.Success)
            return null;

        return new GameMetadataRecord
        {
            GameEntryId = gameEntryId,
            Developer = Join(facts.Developers),
            Publisher = Join(facts.Publishers),
            ReleaseDate = BlankToNull(facts.ReleaseDate),
            Genre = Join(facts.Genres),
            Description = BlankToNull(facts.ShortDescription),
            Engine = null,
            MetacriticScore = facts.MetacriticScore,
            SteamAppId = BlankToNull(facts.SteamAppId),
            CoverArtUrl = BlankToNull(facts.HeaderImage),
            OfficialWebsite = BlankToNull(facts.Website),
            LastMetadataSource = nameof(MetadataSource.Steam),
        };
    }

    /// <summary>Normalize PCGW Infobox facts. Returns null when the page has no usable fields.</summary>
    public static GameMetadataRecord? ToRecord(PcgwFacts facts, string gameEntryId = "")
    {
        string? developer = Join(facts.Developers.Select(StripCompanyPrefix).OfType<string>().ToList());
        string? publisher = Join(facts.Publishers.Select(StripCompanyPrefix).OfType<string>().ToList());
        string? genre = Join(facts.Genres);
        string? engine = Join(facts.Engines);
        string? release = facts.ReleaseDates.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        if (developer is null && publisher is null && genre is null && engine is null && release is null
            && string.IsNullOrWhiteSpace(facts.PageUrl))
        {
            return null;
        }

        return new GameMetadataRecord
        {
            GameEntryId = gameEntryId,
            Developer = developer,
            Publisher = publisher,
            ReleaseDate = BlankToNull(release),
            Genre = genre,
            Engine = engine,
            PcGamingWikiUrl = BlankToNull(facts.PageUrl),
            LastMetadataSource = nameof(MetadataSource.Pcgw),
        };
    }

    /// <summary>Infobox + operator sections from one PCGW page.</summary>
    public static GameMetadataRecord? ToRecord(PcgwLookupResult page, string gameEntryId = "")
    {
        GameMetadataRecord? record = page.Facts is null ? null : ToRecord(page.Facts, gameEntryId);
        GameMetadataDetails details = ToDetails(page.Sections);
        if (record is null && !details.HasAny)
            return null;

        record ??= new GameMetadataRecord
        {
            GameEntryId = gameEntryId,
            LastMetadataSource = nameof(MetadataSource.Pcgw),
        };

        string? steamAppId = MetadataText.SafeSteamAppId(StoreId(page.Sections, "Steam"));
        string? gogId = MetadataText.SafeStoreSlug(StoreId(page.Sections, "GOG"));
        return record with
        {
            Details = details.HasAny ? details : record.Details,
            SteamAppId = record.SteamAppId ?? steamAppId,
            GogGameId = record.GogGameId ?? gogId,
        };
    }

    private static List<GameMetadataPath> SafePaths(IReadOnlyList<PcgwGameDataPath> paths, string kind) =>
        paths
            .Where(p => p.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))
            .Select(p => (Os: MetadataText.SafeNote(p.Os, 16), Template: MetadataText.SafePathTemplate(p.Template)))
            .Where(p => p.Template is not null && p.Os.Length > 0)
            .Select(p => new GameMetadataPath { Kind = kind, Os = p.Os, Template = p.Template! })
            .ToList();

    private static string? SanitizeArgList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var kept = new List<string>();
        foreach (string part in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string? arg = MetadataText.SafeArgument(part);
            if (arg is not null)
                kept.Add(arg);
        }

        return kept.Count == 0 ? null : string.Join(' ', kept);
    }

    private static string? StoreId(PcgwSectionFacts sections, string store) =>
        sections.StoreIds.TryGetValue(store, out string? id) && !string.IsNullOrWhiteSpace(id)
            ? id.Trim()
            : null;

    /// <summary>Map parsed PCGW sections onto the sidecar details block.</summary>
    public static GameMetadataDetails ToDetails(PcgwSectionFacts sections)
    {
        return new GameMetadataDetails
        {
            ConfigPaths = SafePaths(sections.Paths, "config"),
            SavePaths = SafePaths(sections.Paths, "saves"),
            CommandLine = sections.CommandLine
                .Select(c => (Arg: MetadataText.SafeArgument(c.Argument), Row: c))
                .Where(x => x.Arg is not null)
                .Select(x => new GameMetadataCommandLine
                {
                    Argument = x.Arg!,
                    Notes = MetadataText.SafeNote(x.Row.Notes),
                    NeedsValue = x.Row.NeedsValue,
                    Source = MetadataText.SafeNote(x.Row.Source, 24),
                })
                .ToList(),
            Fixes = sections.Fixes
                .Select(f => new GameMetadataFix
                {
                    Title = MetadataText.SafeNote(f.Title, 200),
                    SuggestedArgs = MetadataText.SafeArgument(f.SuggestedArgs) is string one
                        ? one
                        : SanitizeArgList(f.SuggestedArgs),
                    SuggestedExecutable = MetadataText.SafePathTemplate(f.SuggestedExecutable),
                })
                .Where(f => f.Title.Length > 0)
                .ToList(),
            Video = sections.Video
                .Where(kv => MetadataDetailsFormatter.IsShortCap(kv.Value))
                .ToDictionary(kv => MetadataText.SafeNote(kv.Key, 32), kv => MetadataText.SafeNote(kv.Value, 24), StringComparer.OrdinalIgnoreCase),
            CloudSync = sections.CloudSync
                .Where(kv => MetadataDetailsFormatter.IsShortCap(kv.Value))
                .ToDictionary(kv => MetadataText.SafeNote(kv.Key, 32), kv => MetadataText.SafeNote(kv.Value, 16), StringComparer.OrdinalIgnoreCase),
        };
    }

    /// <summary>Strip PCGW <c>Company:</c> prefixes (used by step 3).</summary>
    public static string? StripCompanyPrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Replace("Company:", "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static string? Join(IReadOnlyList<string> items)
    {
        var cleaned = items.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        return cleaned.Count == 0 ? null : string.Join(", ", cleaned);
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
