namespace GamingCommander.Core.Models;

/// <summary>
/// Field names of the Steam ACF (appmanifest) key=value format plus the
/// <c>"path"</c> key of libraryfolders.vdf. These spellings ARE the format
/// (see docs/research/steam_acf_schema.md): <see cref="GamingCommander.App.Services.SteamAcfWriter"/>
/// must emit exactly what Steam writes and <see cref="GamingCommander.App.Services.SteamAcfParser"/>
/// reads, so the names are pinned here as constants shared by both sides.
/// </summary>
public static class SteamAcfFields
{
    /// <summary>Top-level ACF section header.</summary>
    public const string AppState = "AppState";

    /// <summary>Steam numeric AppID.</summary>
    public const string AppId = "appid";

    /// <summary>Steam client universe (write-only here; no reader in this codebase).</summary>
    public const string Universe = "Universe";

    /// <summary>Installed game name as written by Steam.</summary>
    public const string Name = "name";

    /// <summary>Install folder name relative to the library root.</summary>
    public const string InstallDir = "installdir";

    /// <summary>ACF state flags bitmask.</summary>
    public const string StateFlags = "StateFlags";

    /// <summary>Unix-timestamp of last update.</summary>
    public const string LastUpdated = "LastUpdated";

    /// <summary>Installed size on disk in bytes.</summary>
    public const string SizeOnDisk = "SizeOnDisk";

    /// <summary>Steam build id.</summary>
    public const string BuildId = "buildid";

    /// <summary>libraryfolders.vdf key whose value is a Steam library root path.</summary>
    public const string Path = "path";
}