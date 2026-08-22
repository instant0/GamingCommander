using System.Text.Json.Serialization;

namespace GamingCommander.Core.Models;

/// <summary>Config or save path template from PCGW Game data rows.</summary>
public sealed record GameMetadataPath
{
    public string Kind { get; init; } = string.Empty;
    public string Os { get; init; } = string.Empty;
    public string Template { get; init; } = string.Empty;
}

/// <summary>One PCGW command-line catalog row (F4 toggle source). Not user state.</summary>
public sealed record GameMetadataCommandLine
{
    public string Argument { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool NeedsValue { get; init; }
    public string Source { get; init; } = string.Empty;
}

/// <summary>Essential-improvements hint (exe path and/or suggested args).</summary>
public sealed record GameMetadataFix
{
    public string Title { get; init; } = string.Empty;
    public string? SuggestedArgs { get; init; }
    public string? SuggestedExecutable { get; init; }
}

/// <summary>
/// Operator extras in the sidecar. Catalog only — enabled flags live on the game entry.
/// </summary>
public sealed record GameMetadataDetails
{
    public List<GameMetadataPath> ConfigPaths { get; init; } = [];
    public List<GameMetadataPath> SavePaths { get; init; } = [];
    public List<GameMetadataCommandLine> CommandLine { get; init; } = [];
    public List<GameMetadataFix> Fixes { get; init; } = [];
    public Dictionary<string, string> Video { get; init; } = [];
    public Dictionary<string, string> CloudSync { get; init; } = [];

    [JsonIgnore]
    public bool HasAny =>
        ConfigPaths.Count > 0
        || SavePaths.Count > 0
        || CommandLine.Count > 0
        || Fixes.Count > 0
        || Video.Count > 0
        || CloudSync.Count > 0;
}
