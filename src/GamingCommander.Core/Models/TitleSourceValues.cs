namespace GamingCommander.Core.Models;

/// <summary>
/// Constant values for the <see cref="PlatformMetadataKeys.TitleSource"/> persisted
/// metadata key. These record HOW a game's display title was derived and are
/// ON-DISK DATA (games.json) — the values must never be renamed, only the C#
/// references to them may change. Use these constants instead of raw string
/// literals when reading or writing a TitleSource value.
/// </summary>
public static class TitleSourceValues
{
    /// <summary>Title taken from the game's GOG <c>goggame-*.info</c> file.</summary>
    public const string GogInfo = "GogInfo";

    /// <summary>Title taken from the EA install log.</summary>
    public const string EaInstallLog = "EaInstallLog";

    /// <summary>Title taken from an Epic Games Store .item manifest.</summary>
    public const string EpicItemManifest = "EpicItemManifest";

    /// <summary>Title derived from the game folder + discovered executable name.</summary>
    public const string FolderExeMatch = "FolderExeMatch";

    /// <summary>Title derived from the primary executable's PE FileDescription.</summary>
    public const string PeFileDescription = "PeFileDescription";

    /// <summary>Title taken from the game's Uplay/Ubisoft readme.</summary>
    public const string UbisoftReadme = "UbisoftReadme";

    /// <summary>Title pinned by the user via the PCGW picker (F3).</summary>
    public const string PcgwPick = "PcgwPick";

    /// <summary>
    /// Title pinned by the user via manual edit. Legacy persisted marker:
    /// compared to preserve pins across rescans, never written by current code.
    /// </summary>
    public const string UserOverride = "UserOverride";
}