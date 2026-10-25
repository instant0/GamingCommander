namespace GamingCommander.Core.Models;

/// <summary>
/// The state a store's inventory reports for a game (Plan 129 §3.4). This is evidence from the
/// store, kept separate from the disk-derived <see cref="InstallState"/>.
/// </summary>
public enum StoreReportedState
{
    /// <summary>The store did not report a state.</summary>
    Unknown = 0,

    /// <summary>The store reports the game as installed.</summary>
    Installed = 1,

    /// <summary>The store reports that an update is required.</summary>
    UpdateRequired = 2,

    /// <summary>The store reports an incomplete installation.</summary>
    Incomplete = 3,
}
