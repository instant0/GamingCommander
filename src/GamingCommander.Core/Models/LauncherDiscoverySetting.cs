namespace GamingCommander.Core.Models;

/// <summary>
/// The Setup choice for how launchers are discovered. This is not the display mode
/// (Library / Flat / Stores / Engines); it decides whether launcher locations may be read
/// at all and which ones may be probed on an explicit action.
/// </summary>
public enum LauncherDiscoverySetting
{
    /// <summary>First run: ask before any launcher probe.</summary>
    Unset,

    /// <summary>Detect every registered launcher on an explicit action.</summary>
    Automatic,

    /// <summary>Detect only the launchers the user selected.</summary>
    Selected,

    /// <summary>No launcher discovery, registry, or ProgramData launcher reads; file signals only.</summary>
    FileOnly,
}
