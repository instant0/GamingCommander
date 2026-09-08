using System.IO;
using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Steam/Epic status-detail formatting for <see cref="ShellViewModel"/> (Plan 125
/// final stage): the actionable right-pane text for Orphaned / Missing / Moved games.
/// </summary>
public sealed partial class ShellViewModel
{
    /// <summary>Formats actionable detail text for Orphaned Steam games (folder exists, no ACF).</summary>
    private static string FormatOrphanedDetail(GameEntry game)
    {
        if (game.GameSource == GameSourceKind.Epic)
        {
            return "Orphaned — Epic folder (.egstore) has no ProgramData .item. " +
                    "Write Epic .item from .egstore scraps. Then Update in Epic — only ONE official repair per launcher run.";
        }

        string folder = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.FolderName, game.FolderName);
        string libRoot = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.LibraryRoot, "unknown library");
        return $"Orphaned — no Steam manifest for '{folder}'. " +
               $"This folder exists in {libRoot} but no ACF in any configured library names that folder. " +
               "F3 to get the Steam AppID from PCGW, then click Write Steam ACF. " +
               "Or add the library that already has the ACF (F2).";
    }

    /// <summary>Formats actionable detail text for Missing Steam games (ACF exists, no game folder).</summary>
    private static string FormatMissingDetail(GameEntry game)
    {
        string acfPath = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.AcfFilePath, "");
        string expected = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.AcfExpectedPath, "");
        return $"Missing — Steam manifest at {acfPath} expects files at {expected}. " +
               "Game folder not found in any configured library. " +
               "Possible: game is in an unconfigured library, or was uninstalled. " +
               "To fix: Add the correct Steam library via F2, or delete the orphaned ACF.";
    }

    /// <summary>Formats actionable detail text for Moved Steam games (game in different library than ACF).</summary>
    private static string FormatMovedDetail(GameEntry game)
    {
        string acfPath = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.AcfFilePath, "unknown");
        string acfLib = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.AcfLibraryPath, "unknown");
        string actualLib = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.ActualLibraryRoot, "unknown");
        string folder = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.FolderName, game.FolderName);
        string acfFileName = Path.GetFileName(acfPath);
        string targetPath = Path.Combine(actualLib, "steamapps", acfFileName);
        return $"Moved — game '{folder}' found in {actualLib} but ACF is in {acfLib}. " +
               $"To fix: Move ACF to {targetPath} and restart Steam.";
    }
}