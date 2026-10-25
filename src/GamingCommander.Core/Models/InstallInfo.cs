namespace GamingCommander.Core.Models;

/// <summary>
/// Unified install state + version for a game entry (Plan 127 B8). Store-specific detail
/// (size, build id, state flags) is owned by <see cref="SteamAcfInfo"/>/<see cref="EpicInfo"/>.
/// </summary>
public sealed record InstallInfo
{
    /// <summary>Unified cross-store install state.</summary>
    public InstallState State { get; init; } = InstallState.Unknown;

    /// <summary>Installed version/build string when known.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>True when an update is known to be available.</summary>
    public bool UpdateAvailable { get; init; }

    /// <summary>Raw state reported by the store inventory; independent of the disk-derived <see cref="State"/>.</summary>
    public StoreReportedState StoreReportedState { get; init; } = StoreReportedState.Unknown;

    /// <summary>When a store inventory last reported this game.</summary>
    public DateTimeOffset? LastStoreSeenUtc { get; init; }

    /// <summary>When the install state was last verified.</summary>
    public DateTimeOffset? LastVerified { get; init; }
}
