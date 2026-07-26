using System.Text.Json;

namespace GamingCommander.App.Services;

/// <summary>
/// Parses Epic Games Store manifest files (.item and .mancpn) for game metadata.
/// Extracts display name, catalog identifiers, and launch executable paths.
/// Supports both local (.egstore/manifests/) and global (ProgramData) manifest discovery.
/// </summary>
internal static class EpicManifestParser
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Result of parsing an Epic .item file — contains full metadata.
    /// </summary>
    internal sealed record EpicItemData(
        string DisplayName,
        string InstallLocation,
        string LaunchExecutable,
        string CatalogNamespace,
        string CatalogItemId,
        string AppName,
        bool IsIncompleteInstall);

    /// <summary>
    /// Result of parsing an Epic .mancpn file — contains catalog identifiers only.
    /// </summary>
    internal sealed record EpicIdentifiers(
        string CatalogNamespace,
        string CatalogItemId,
        string AppName,
        string DisplayName = "",
        string LaunchExecutable = "");

    /// <summary>
    /// Parses a single .item file and extracts metadata.
    /// Returns null if the file is missing, malformed, or marked as incomplete install.
    /// </summary>
    /// <param name="filePath">Full path to the .item file.</param>
    internal static EpicItemData? ParseItemFile(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            JsonElement root = doc.RootElement;

            // Skip incomplete installs (games being downloaded/updated)
            bool isIncomplete = root.TryGetProperty("bIsIncompleteInstall", out var incomplete)
                && incomplete.GetBoolean();
            if (isIncomplete)
                return null;

            string displayName = root.TryGetProperty("DisplayName", out var dn) ? dn.GetString() ?? "" : "";
            string installLocation = root.TryGetProperty("InstallLocation", out var il) ? il.GetString() ?? "" : "";
            string launchExecutable = root.TryGetProperty("LaunchExecutable", out var le) ? le.GetString() ?? "" : "";
            string catalogNamespace = root.TryGetProperty("CatalogNamespace", out var cn) ? cn.GetString() ?? "" : "";
            string catalogItemId = root.TryGetProperty("CatalogItemId", out var ci) ? ci.GetString() ?? "" : "";
            string appName = root.TryGetProperty("AppName", out var an) ? an.GetString() ?? "" : "";

            return new EpicItemData(
                DisplayName: displayName,
                InstallLocation: installLocation,
                LaunchExecutable: launchExecutable,
                CatalogNamespace: catalogNamespace,
                CatalogItemId: catalogItemId,
                AppName: appName,
                IsIncompleteInstall: isIncomplete);
        }
        catch (JsonException)
        {
            // Malformed JSON — return null
            return null;
        }
        catch (IOException)
        {
            // File read error — return null
            return null;
        }
    }

    /// <summary>
    /// Parses a single .mancpn file and extracts catalog identifiers.
    /// Returns null if the file is missing or malformed.
    /// </summary>
    /// <param name="filePath">Full path to the .mancpn file.</param>
    internal static EpicIdentifiers? ParseMancpnFile(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            JsonElement root = doc.RootElement;

            string catalogNamespace = root.TryGetProperty("CatalogNamespace", out var cn) ? cn.GetString() ?? "" : "";
            string catalogItemId = root.TryGetProperty("CatalogItemId", out var ci) ? ci.GetString() ?? "" : "";
            string appName = root.TryGetProperty("AppName", out var an) ? an.GetString() ?? "" : "";

            // .mancpn files don't have DisplayName or LaunchExecutable
            return new EpicIdentifiers(
                CatalogNamespace: catalogNamespace,
                CatalogItemId: catalogItemId,
                AppName: appName);
        }
        catch (JsonException)
        {
            // Malformed JSON — return null
            return null;
        }
        catch (IOException)
        {
            // File read error — return null
            return null;
        }
    }

    /// <summary>
    /// Extracts local identifiers from .egstore/ or .egsstore/ directories.
    /// Searches for .item files first (richer schema), falls back to .mancpn.
    /// </summary>
    /// <param name="gameDir">The game's root directory.</param>
    internal static EpicIdentifiers? ExtractLocalIdentifiers(DirectoryInfo gameDir)
    {
        // Check both .egstore and .egsstore directories
        foreach (string storeDirName in new[] { ".egstore", ".egsstore" })
        {
            string storeDirPath = Path.Combine(gameDir.FullName, storeDirName);
            if (!Directory.Exists(storeDirPath))
                continue;

            // Search in manifests/ subdirectory and store root
            string[] searchDirs =
            [
                Path.Combine(storeDirPath, "manifests"),
                storeDirPath,
            ];

            // Prefer .item files (richer schema)
            foreach (string searchDir in searchDirs)
            {
                if (!Directory.Exists(searchDir))
                    continue;

                try
                {
                    foreach (string itemFile in Directory.GetFiles(searchDir, "*.item"))
                    {
                        var itemData = ParseItemFile(itemFile);
                        if (itemData is not null)
                        {
                            return new EpicIdentifiers(
                                CatalogNamespace: itemData.CatalogNamespace,
                                CatalogItemId: itemData.CatalogItemId,
                                AppName: itemData.AppName,
                                DisplayName: itemData.DisplayName,
                                LaunchExecutable: itemData.LaunchExecutable);
                        }
                    }
                }
                catch (IOException)
                {
                    // Permission error — continue with next search dir
                    continue;
                }
            }

            // Fall back to .mancpn files
            try
            {
                foreach (string mancpnFile in Directory.GetFiles(storeDirPath, "*.mancpn"))
                {
                    var identifiers = ParseMancpnFile(mancpnFile);
                    if (identifiers is not null)
                    {
                        return identifiers;
                    }
                }
            }
            catch (IOException)
            {
                // Permission error — continue with next store dir
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a relative LaunchExecutable path to an absolute path.
    /// If the path is already rooted, returns it as-is.
    /// </summary>
    /// <param name="installLocation">The game's install location (absolute path).</param>
    /// <param name="launchExecutable">The relative launch executable path from the .item file.</param>
    internal static string ResolveLaunchExecutable(string installLocation, string launchExecutable)
    {
        if (string.IsNullOrEmpty(launchExecutable))
            return string.Empty;

        if (Path.IsPathRooted(launchExecutable))
            return launchExecutable;

        // LaunchExecutable is relative to InstallLocation
        return Path.GetFullPath(Path.Combine(installLocation, launchExecutable));
    }

    /// <summary>
    /// Cross-references a game folder against the global .item manifest directory.
    /// Matches InstallLocation against the game folder path (case-insensitive, trailing separator stripped).
    /// Returns the matching .item data, or null if no match found.
    /// </summary>
    /// <param name="gameDir">The game's root directory.</param>
    /// <param name="manifestsDir">Override path to the global manifests directory. If null, uses default.</param>
    internal static EpicItemData? CrossReferenceGlobalManifests(
        DirectoryInfo gameDir,
        string? manifestsDir = null)
    {
        string manifestsPath = manifestsDir
            ?? Environment.GetEnvironmentVariable("EPIC_MANIFESTS_DIR")
            ?? EpicManifestPaths.DefaultManifestsDir;

        if (!Directory.Exists(manifestsPath))
            return null;

        // Normalize game folder path for comparison
        string target = NormalizePath(gameDir.FullName);

        try
        {
            foreach (string itemFile in Directory.GetFiles(manifestsPath, "*.item"))
            {
                var itemData = ParseItemFile(itemFile);
                if (itemData is null)
                    continue;

                // Compare InstallLocation against game folder (case-insensitive, trailing separator stripped)
                string installLoc = NormalizePath(itemData.InstallLocation);
                if (!string.IsNullOrEmpty(installLoc) && installLoc == target)
                {
                    return itemData;
                }
            }
        }
        catch (IOException)
        {
            // Permission error or similar — return null
        }

        return null;
    }

    /// <summary>
    /// Normalizes a path for comparison: converts to lowercase, strips trailing separators.
    /// Handles both Windows backslash and forward slash separators.
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path
            .ToLowerInvariant()
            .TrimEnd('\\', '/');
    }
}

/// <summary>
/// Path configuration for Epic Games Store manifest directories.
/// </summary>
internal static class EpicManifestPaths
{
    /// <summary>
    /// Default global manifests directory on Windows: %ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\
    /// </summary>
    internal static string DefaultManifestsDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");

    /// <summary>
    /// Gets the effective manifests directory path.
    /// Checks: override path → EPIC_MANIFESTS_DIR env var → default ProgramData path.
    /// </summary>
    /// <param name="overridePath">Optional override path.</param>
    internal static string GetManifestsDir(string? overridePath = null) =>
        overridePath
        ?? Environment.GetEnvironmentVariable("EPIC_MANIFESTS_DIR")
        ?? DefaultManifestsDir;
}
