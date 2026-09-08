using System.Globalization;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Parses Steam ACF (App Manifest) files and libraryfolders.vdf.
/// Provides structured access to game metadata stored in VDF format.
/// </summary>
internal static class SteamAcfParser
{
    private static readonly IReadOnlyList<string> RequiredAcfFields =
        ["appid", "name", "installdir", "StateFlags", "LastUpdated", "SizeOnDisk", "buildid"];

    /// <summary>
    /// Parses a Steam ACF (appmanifest) file and returns structured metadata.
    /// Returns null if the file is missing, corrupt, or missing required fields.
    /// </summary>
    /// <param name="acfPath">Full path to the .acf file.</param>
    /// <param name="libraryPath">Steam library root path (parent of steamapps/).</param>
    internal static AcfInfo? ParseAcfFile(string acfPath, string libraryPath)
    {
        try
        {
            string text = File.ReadAllText(acfPath);
            var fields = VdfParser.ExtractFields(text, RequiredAcfFields.ToArray());
            if (fields == null) return null;

            string installDir = fields.GetValueOrDefault("installdir", string.Empty);
            if (string.IsNullOrWhiteSpace(installDir)) return null;

            return new AcfInfo(
                LibraryPath: libraryPath,
                AcfFilePath: acfPath,
                AppId: fields.GetValueOrDefault("appid", string.Empty),
                Name: fields.GetValueOrDefault("name", string.Empty),
                Installdir: installDir,
                StateFlags: fields.GetValueOrDefault("StateFlags", string.Empty),
                LastUpdated: fields.GetValueOrDefault("LastUpdated", string.Empty),
                SizeOnDisk: fields.GetValueOrDefault("SizeOnDisk", string.Empty),
                BuildId: fields.GetValueOrDefault("buildid", string.Empty));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Discovers all Steam library paths from libraryfolders.vdf.
    ///
    /// SIMPLE approach (2026-09-07): the file is scanned line by line for lines
    /// containing the "path" key, then the NEXT quoted string on that line is the
    /// library path. Only values matching drive-letter syntax (X:\...) are kept.
    /// The "apps" blocks (appid → size) are NEVER read — they are informational
    /// and must not be treated as library paths.
    /// </summary>
    /// <param name="libraryRoot">Steam library root containing steamapps/libraryfolders.vdf.</param>
    internal static List<string> DiscoverLibraryPaths(string libraryRoot)
    {
        string vdfPath = Path.Combine(libraryRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
            return [];

        return GetLibraryPaths(vdfPath).ToList();
    }

    /// <summary>
    /// Reads every <c>"path"</c> entry from a Steam libraryfolders.vdf.
    /// Line shape: <c>"path"  "D:\SteamLibrary"</c> → split on '"', parts[1]=="path",
    /// the value is parts[3]. Unescapes the doubled backslashes Steam writes.
    /// </summary>
    private static IEnumerable<string> GetLibraryPaths(string filename)
    {
        foreach (var line in File.ReadLines(filename))
        {
            var parts = line.Split('"');
            if (parts.Length >= 4 && parts[1] == "path")
                yield return parts[3].Replace(@"\\", @"\");
        }
    }

    /// <summary>Normalizes a path by trimming trailing directory separators.</summary>
    internal static string NormalizePath(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

/// <summary>
/// Parsed metadata from a Steam ACF (appmanifest) file.
/// </summary>
internal sealed record AcfInfo(
    string LibraryPath,
    string AcfFilePath,
    string AppId,
    string Name,
    string Installdir,
    string StateFlags,
    string LastUpdated,
    string SizeOnDisk,
    string BuildId);
