namespace GamingCommander.App.Services;

/// <summary>
/// Single source of truth for the small detection noise sets (detector review 2026-10-14, O1/O2).
/// These are deliberately separate from the tiered <c>data/blacklist.json</c> (which drives exe
/// scoring): each set expresses a distinct, bounded rule. Centralizing them stops the same tokens
/// from drifting across <see cref="FolderScanner"/>, <see cref="ContainerScanner"/>,
/// <see cref="ExecutableDiscovery"/>, <see cref="LnkParser"/>, and <see cref="SteamLibraryScanner"/>.
/// </summary>
internal static class DetectionNoise
{
    /// <summary>
    /// "Not game evidence" exe-name substrings. Used by the container non-game file-type layer
    /// (layer 3) and the exact-folder-match terminating rule's noise gate. Installers/updaters/
    /// tools/editors that happen to share a folder name must not be promoted or terminate.
    /// </summary>
    internal static readonly string[] NonGameExe =
    [
        "install", "setup", "unins", "redist", "vcredist", "dxsetup", "oalinst",
        "launcher", "updater", "bootstrap", "crash", "error", "service", "tool",
        "editor", "config", "helper", "update",
    ];

    /// <summary>DLLs / targets skipped when resolving .lnk shortcuts (not real game exes).</summary>
    internal static readonly IReadOnlySet<string> LnkSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "steam_api.dll", "steam_api64.dll", "eos.dll", "upc.dll",
    };

    /// <summary>
    /// Steam <c>common/</c> folder names that are not games. This is the Steam scanner's OWN list —
    /// it must never filter the literal name "steam" (installdirs are arbitrary).
    /// </summary>
    internal static readonly IReadOnlySet<string> SteamCommonNoise = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "steam controller configs",
        "steamworks shared",
    };
}
