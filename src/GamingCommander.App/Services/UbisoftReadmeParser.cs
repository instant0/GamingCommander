namespace GamingCommander.App.Services;

/// <summary>
/// Parses Ubisoft Connect Support/Readme/ text files for game metadata.
/// Ubisoft games conventionally ship a Support/Readme/ directory containing text files
/// where the first 4 lines are: publisher, game title, copyright, blank.
/// Returns the game title and publisher when found.
/// </summary>
internal static class UbisoftReadmeParser
{
    /// <summary>
    /// Result of parsing a Ubisoft Support/Readme/ file.
    /// </summary>
    internal sealed record UbisoftReadmeInfo(string? Publisher, string? GameTitle);

    /// <summary>
    /// Attempts to parse Ubisoft Support/Readme/ metadata from a game directory.
    /// Searches for text files in Support/Readme/ (case-insensitive) and reads
    /// the first 4 lines per Ubisoft convention.
    /// </summary>
    /// <param name="gameDir">The game directory to search.</param>
    /// <returns>Parsed readme info, or null if no readable readme found.</returns>
    internal static UbisoftReadmeInfo? TryParse(DirectoryInfo gameDir)
    {
        // Search for Support/Readme/ (case-insensitive) — common Ubisoft convention
        string[] searchPaths =
        [
            Path.Combine(gameDir.FullName, "Support", "Readme"),
            Path.Combine(gameDir.FullName, "support", "readme"),
            Path.Combine(gameDir.FullName, "Support", "readme"),
            Path.Combine(gameDir.FullName, "support", "Readme"),
        ];

        string? readmeDir = null;
        foreach (string path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                readmeDir = path;
                break;
            }
        }

        if (readmeDir is null)
            return null;

        // Find first text file in the readme directory
        try
        {
            string[] textFiles = Directory.GetFiles(readmeDir, "*.txt");
            if (textFiles.Length == 0)
            {
                // Also try files without extension (some Ubisoft readmes have no extension)
                textFiles = Directory.GetFiles(readmeDir, "*")
                    .Where(f => !Path.GetFileName(f).Contains('.'))
                    .ToArray();
            }

            if (textFiles.Length == 0)
                return null;

            // Read first 4 lines from the first text file
            string[] lines = File.ReadAllLines(textFiles[0]);
            if (lines.Length == 0)
                return null;

            string? publisher = lines.Length >= 1 ? lines[0]?.Trim() : null;
            string? gameTitle = lines.Length >= 2 ? lines[1]?.Trim() : null;

            // Validate: reject empty/whitespace-only values
            if (string.IsNullOrWhiteSpace(publisher))
                publisher = null;
            if (string.IsNullOrWhiteSpace(gameTitle))
                gameTitle = null;

            // If we have no useful data, return null
            if (publisher is null && gameTitle is null)
                return null;

            return new UbisoftReadmeInfo(publisher, gameTitle);
        }
        catch
        {
            return null;
        }
    }
}
