using GamingCommander.Core.Services;

namespace GamingCommander.App;

/// <summary>
/// Owns the application data directory (<c>{base}/data</c>). This is the only place a
/// directory is created for JSON state; anything outside it is refused, never created.
/// </summary>
public static class AppPaths
{
    // Resolved once so every path getter and every store agrees on where app data lives.
    private static readonly string _appDataRoot =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

    /// <summary>Absolute path of the application data directory.</summary>
    public static string AppDataRoot => _appDataRoot;

    /// <summary>
    /// Ensures the parent directory of <paramref name="filePath"/> exists, creating it only when
    /// the path is inside <see cref="AppDataRoot"/>. A path outside the app root is a programming
    /// error, so it throws instead of creating an external directory.
    /// </summary>
    /// <returns><paramref name="filePath"/>, unchanged.</returns>
    public static string EnsureAppDataPath(string filePath)
    {
        if (!PathText.IsUnder(filePath, AppDataRoot))
        {
            throw new InvalidOperationException(
                $"Refusing to create a directory outside the application data root: '{filePath}'.");
        }

        // CreateDirectory is idempotent; an existence probe would only add a call and a race.
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return filePath;
    }
}
