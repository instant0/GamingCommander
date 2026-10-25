namespace GamingCommander.Core.Models;

/// <summary>
/// Unified, cross-store install state for a game entry (Plan 127 B8). Derived from the
/// store-specific status (<see cref="SteamAcfInfo.Status"/> / <see cref="EpicInfo.Status"/>)
/// plus folder presence. Store-specific detail (size/build/stateflags) stays on the store record.
/// </summary>
public enum InstallState
{
    /// <summary>Not yet determined.</summary>
    Unknown = 0,

    /// <summary>Files present and registered.</summary>
    Installed = 1,

    /// <summary>Owned/entitled but not installed locally.</summary>
    Owned = 2,

    /// <summary>Files present at a different path than the manifest expects.</summary>
    Moved = 3,

    /// <summary>Manifest present but files missing.</summary>
    Missing = 4,

    /// <summary>Files present but no manifest registration.</summary>
    Orphaned = 5,

    /// <summary>An update is in progress / pending.</summary>
    Updating = 6,
}
