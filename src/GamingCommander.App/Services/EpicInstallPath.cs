using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>Compare Epic InstallLocation to a scanned game folder using the shared path rule.</summary>
internal static class EpicInstallPath
{
    /// <summary>Comparison form. Case is preserved; compare with <see cref="PathText.Equals"/>.</summary>
    public static string Normalize(string path) => PathText.NormalizeForCompare(path);

    public static string FolderName(string path) => PathText.Leaf(path);

    /// <summary>
    /// True only when both strings name the same folder. A shared folder name is not a match:
    /// two libraries can contain different games in folders with the same name.
    /// </summary>
    public static bool Same(string? a, string? b) => PathText.Equals(a, b);
}
