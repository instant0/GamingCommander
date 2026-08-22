namespace GamingCommander.Core.Models;

/// <summary>Origin of a sidecar metadata fact (Plan 119).</summary>
public enum MetadataSource
{
    /// <summary>Unknown or mixed merge.</summary>
    Unknown = 0,

    /// <summary>Steam Store appdetails.</summary>
    Steam = 1,

    /// <summary>PCGamingWiki OpenSearch / Parse.</summary>
    Pcgw = 2,

    /// <summary>Local Epic .item / .mancpn (no GraphQL).</summary>
    EpicLocal = 3,
}
