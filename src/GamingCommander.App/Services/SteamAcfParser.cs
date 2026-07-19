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
    /// Parses libraryfolders.vdf to discover all Steam library paths.
    /// Format: numbered keys like "1" "D:\\SteamLibrary", "2" "E:\\SteamLibrary"
    /// </summary>
    /// <param name="libraryRoot">Steam library root containing steamapps/libraryfolders.vdf.</param>
    internal static List<string> DiscoverLibraryPaths(string libraryRoot)
    {
        var paths = new List<string>();
        string vdfPath = Path.Combine(libraryRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath)) return paths;

        try
        {
            string text = File.ReadAllText(vdfPath);
            var parsed = VdfParser.Parse(text);

            // Navigate into the root block (usually "LibraryFolders")
            var block = parsed;
            if (block.Count == 1 && block.Values.First() is Dictionary<string, object> inner)
                block = inner;

            foreach (var kvp in block)
            {
                // Numeric keys like "1", "2", "3" hold path values
                if (int.TryParse(kvp.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    if (kvp.Value is string pathStr && !string.IsNullOrWhiteSpace(pathStr))
                    {
                        paths.Add(NormalizePath(pathStr));
                    }
                }
            }
        }
        catch
        {
            // Silently return empty on parse failure
        }

        return paths;
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
