using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Maps a store-specific status string (<see cref="SteamAcfInfo.Status"/> /
/// <see cref="EpicInfo.Status"/>) to the unified <see cref="InstallState"/> (Plan 127 B8).
/// </summary>
public static class InstallStateMap
{
    public static InstallState FromStoreStatus(string? status) => status switch
    {
        "Installed" => InstallState.Installed,
        "Moved" => InstallState.Moved,
        "Missing" => InstallState.Missing,
        "Orphaned" => InstallState.Orphaned,
        "Updating" => InstallState.Updating,
        _ => InstallState.Unknown,
    };
}
