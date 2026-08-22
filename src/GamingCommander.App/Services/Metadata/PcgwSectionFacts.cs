namespace GamingCommander.App.Services.Metadata;

/// <summary>Config or save path from <c>{{Game data/config}}</c> / <c>{{Game data/saves}}</c>.</summary>
public sealed record PcgwGameDataPath(string Kind, string Os, string Template);

/// <summary>One command-line catalog row for F4 toggles.</summary>
public sealed record PcgwCommandLineEntry(string Argument, string Notes, bool NeedsValue, string Source);

/// <summary>Essential-improvements Fixbox (exe hint and/or suggested args).</summary>
public sealed record PcgwFix(string Title, string? SuggestedArgs, string? SuggestedExecutable);

/// <summary>Structured operator facts from a PCGW Parse wikitext (Plan 120 Part 2).</summary>
public sealed record PcgwSectionFacts(
    IReadOnlyList<PcgwGameDataPath> Paths,
    IReadOnlyList<PcgwCommandLineEntry> CommandLine,
    IReadOnlyList<PcgwFix> Fixes,
    IReadOnlyDictionary<string, string> Video,
    IReadOnlyDictionary<string, string> CloudSync,
    IReadOnlyDictionary<string, string> StoreIds);
