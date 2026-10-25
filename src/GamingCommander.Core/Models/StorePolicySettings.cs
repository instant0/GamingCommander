namespace GamingCommander.Core.Models;

/// <summary>
/// Persisted per-store policies. A missing store is treated as disabled with default flags.
/// </summary>
public sealed record StorePolicySettings
{
    /// <summary>Configured store policies; empty means every launcher is disabled.</summary>
    public IReadOnlyList<StorePolicy> Stores { get; init; } = [];
}
