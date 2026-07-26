namespace GamingCommander.App.Services;

/// <summary>
/// Parses EA's legacy installer log file (__Installer/InstallLog.txt) for game metadata.
/// Extracts authoritative game name, display name, and developer/studio.
///
/// The log file is UTF-16 encoded and contains key-value lines from the EAInstaller.
/// The Install Location field may reference an old/wrong path (user moved the game),
/// but the game name and studio fields are reliable.
/// </summary>
internal static class EaInstallLogParser
{
    /// <summary>
    /// Result of parsing an EA InstallLog.txt file.
    /// </summary>
    internal record EaGameInfo(
        string GameName,
        string DisplayName,
        string Studio);

    /// <summary>
    /// Attempts to parse __Installer/InstallLog.txt from the game directory.
    /// Returns the most recent install session's metadata.
    /// </summary>
    /// <param name="gameDir">The game's root directory.</param>
    /// <param name="info">The parsed EA metadata, or null if not found.</param>
    /// <returns>True if EA metadata was found and parsed successfully.</returns>
    internal static bool TryParse(DirectoryInfo gameDir, out EaGameInfo? info)
    {
        info = null;

        string logPath = Path.Combine(gameDir.FullName, "__Installer", "InstallLog.txt");
        if (!File.Exists(logPath))
            return false;

        string? gameName = null;
        string? displayName = null;
        string? studio = null;

        try
        {
            // InstallLog.txt is UTF-16 encoded
            string content = File.ReadAllText(logPath, System.Text.Encoding.Unicode);

            // Parse line by line — take the LAST session's values
            // (multiple install sessions may exist; the last one is current)
            foreach (string rawLine in content.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');

                // Extract (Config)Game Name: Dragon Age Inquisition
                if (line.Contains("(Config)Game Name:", StringComparison.OrdinalIgnoreCase))
                {
                    gameName = ExtractValueAfterColon(line);
                }
                // Extract (Config)Display Game Name: Dragon Age™: Inquisition
                else if (line.Contains("(Config)Display Game Name:", StringComparison.OrdinalIgnoreCase))
                {
                    displayName = ExtractValueAfterColon(line);
                }
                // Extract (Config)Studio: BioWare
                else if (line.Contains("(Config)Studio:", StringComparison.OrdinalIgnoreCase))
                {
                    studio = ExtractValueAfterColon(line);
                }
            }
        }
        catch (System.Text.DecoderFallbackException)
        {
            // Encoding issue — skip
            return false;
        }
        catch (IOException)
        {
            // File read error — skip
            return false;
        }

        if (string.IsNullOrEmpty(gameName) && string.IsNullOrEmpty(displayName))
            return false;

        info = new EaGameInfo(
            GameName: gameName ?? "",
            DisplayName: displayName ?? gameName ?? "",
            Studio: studio ?? "");

        return true;
    }

    /// <summary>
    /// Extracts the value after the colon in a field definition.
    /// e.g., "19:28:30  (Config)Studio: BioWare" → "BioWare"
    /// Skips timestamp colons (HH:MM:SS) by searching after the field keyword.
    /// </summary>
    private static string ExtractValueAfterColon(string line)
    {
        // Find the field keyword colon — e.g., after "Game Name:" or "Studio:"
        // The timestamp uses HH:MM:SS format, so search after position 9 (past the timestamp)
        int searchStart = line.Length > 9 ? 9 : 0;
        int idx = line.IndexOf(':', searchStart);
        if (idx < 0 || idx >= line.Length - 1)
            return string.Empty;

        return line[(idx + 1)..].Trim();
    }
}
