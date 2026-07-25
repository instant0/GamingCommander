using System.Text.Json;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Parses GOG goggame-*.info JSON files for game metadata.
/// Extracts title, game ID, primary executable path, and launch arguments.
/// </summary>
internal static class GogInfoParser
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Result of parsing a GOG .info file.
    /// </summary>
    internal record GogGameInfo(
        string Title,
        string GameId,
        string ExePath,
        string LaunchArgs);

    /// <summary>
    /// Attempts to parse GOG goggame-*.info files from the game directory.
    /// Searches root + 1 level of non-noise subdirectories.
    /// Prefers the main game entry (gameId == rootGameId) over DLC entries.
    /// </summary>
    /// <param name="gameDir">The game's root directory.</param>
    /// <param name="noiseDirectoryPatterns">Patterns to exclude from subdirectory search.</param>
    /// <param name="info">The parsed GOG metadata, or null if not found.</param>
    /// <returns>True if GOG metadata was found and parsed successfully.</returns>
    internal static bool TryParse(
        DirectoryInfo gameDir,
        IReadOnlySet<string> noiseDirectoryPatterns,
        out GogGameInfo? info)
    {
        info = null;

        // Build search dirs: root + 1 level of non-noise subdirs
        var searchDirs = new List<DirectoryInfo> { gameDir };
        try
        {
            foreach (DirectoryInfo subDir in FileSystemHelper.GetDirectoriesSafe(gameDir.FullName))
            {
                if (!FileSystemHelper.IsNoiseDirectory(subDir.Name, noiseDirectoryPatterns))
                {
                    searchDirs.Add(subDir);
                }
            }
        }
        catch (System.IO.IOException)
        {
            // Permission error or similar — continue with root only
        }

        string? bestName = null;
        string? bestGameId = null;
        string? bestExe = null;
        string? bestArgs = null;

        foreach (DirectoryInfo searchDir in searchDirs)
        {
            // Search for goggame-*.info files
            string[] infoFiles = FileSystemHelper.GetFilesSafe(searchDir, "goggame-*.info");

            foreach (string infoFilePath in infoFiles)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(infoFilePath);
                    using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                    });

                    JsonElement root = doc.RootElement;

                    string gameId = root.TryGetProperty("gameId", out var gid) ? gid.GetString() ?? "" : "";
                    string rootGameId = root.TryGetProperty("rootGameId", out var rgid) ? rgid.GetString() ?? "" : "";
                    string name = root.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";

                    bool isMain = gameId == rootGameId && !string.IsNullOrEmpty(gameId);

                    if (isMain)
                    {
                        bestName = name;
                        bestGameId = gameId;
                    }
                    else if (bestName is null && !string.IsNullOrEmpty(name))
                    {
                        // Fallback: use DLC entry if no main game found yet
                        bestName = name;
                        bestGameId = gameId;
                    }

                    // Extract primary exe from playTasks — only from main game or when no exe found yet
                    if ((isMain || bestExe is null)
                        && root.TryGetProperty("playTasks", out JsonElement playTasks) && playTasks.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement task in playTasks.EnumerateArray())
                        {
                            bool isPrimary = task.TryGetProperty("isPrimary", out var ip) && ip.GetBoolean();
                            string path = task.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";

                            if (isPrimary && !string.IsNullOrEmpty(path))
                            {
                                bestExe = path;
                                bestArgs = task.TryGetProperty("arguments", out var args) ? args.GetString() ?? "" : "";
                            }
                            else if (bestExe is null && !string.IsNullOrEmpty(path))
                            {
                                // Fallback: use first task with a path if no primary found
                                bestExe = path;
                                bestArgs = task.TryGetProperty("arguments", out var fallbackArgs) ? fallbackArgs.GetString() ?? "" : "";
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Malformed JSON — skip this file, try next
                    continue;
                }
                catch (System.IO.IOException)
                {
                    // File read error — skip this file, try next
                    continue;
                }
            }
        }

        if (bestName is null && bestGameId is null)
            return false;

        // Resolve relative exe path to absolute
        string resolvedExe = string.Empty;
        if (!string.IsNullOrEmpty(bestExe))
        {
            if (System.IO.Path.IsPathRooted(bestExe))
            {
                resolvedExe = bestExe;
            }
            else
            {
                // GOG .info paths are relative to the game root directory
                resolvedExe = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(gameDir.FullName, bestExe));
            }
        }

        info = new GogGameInfo(
            Title: bestName ?? "",
            GameId: bestGameId ?? "",
            ExePath: resolvedExe,
            LaunchArgs: bestArgs ?? "");

        return true;
    }
}
