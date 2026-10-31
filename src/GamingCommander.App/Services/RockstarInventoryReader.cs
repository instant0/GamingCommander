using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Reads Rockstar's best-available inventory from per-game registry keys and <c>title.rgl</c>.
/// The Launcher key is skipped, the registry is not a complete catalogue, and
/// <c>Settings.json</c> is not parsed. Read-only.
/// </summary>
internal sealed class RockstarInventoryReader : IStoreInventoryReader
{
    private const string BaseKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games";

    private readonly IRegistryReader _registry;

    /// <summary>Creates a reader over the supplied registry accessor.</summary>
    public RockstarInventoryReader(IRegistryReader registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public GameSourceKind Kind => GameSourceKind.Rockstar;

    /// <inheritdoc />
    public StoreManifestFingerprint? Fingerprint => null;

    /// <inheritdoc />
    public IReadOnlyList<StoreInventoryEntry> Read()
    {
        IReadOnlyList<string> names;
        try
        {
            names = _registry.EnumerateSubKeyNames(BaseKey);
        }
        catch (Exception ex) when (IsReadFailure(ex))
        {
            return [];
        }

        var entries = new List<StoreInventoryEntry>(names.Count);
        foreach (string gameName in names)
        {
            // The Launcher key is the launcher itself, not a game.
            if (gameName.Equals("Launcher", StringComparison.OrdinalIgnoreCase))
                continue;

            string key = BaseKey + "\\" + gameName;
            string installFolder;
            try
            {
                installFolder = _registry.ReadStringValue(key, "InstallFolder") ?? string.Empty;
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(installFolder))
                continue;

            string installPath = PathText.TrimTrailingSeparators(installFolder);
            bool folderExists = Directory.Exists(installPath);
            (string? titleId, string? rglPath) = folderExists
                ? ReadTitle(installPath)
                : (null, null);

            entries.Add(new StoreInventoryEntry(
                Kind,
                StoreId: titleId ?? string.Empty,
                gameName,
                installPath,
                LaunchExe: null,
                folderExists ? StoreReportedState.Installed : StoreReportedState.Unknown,
                FolderExists: folderExists,
                ExeExists: false,
                new RockstarInventoryMetadata(gameName, titleId, rglPath, key)));
        }

        return entries;
    }

    /// <summary>Reads <c>title.rgl</c> only when it is a short, printable text value.</summary>
    private static (string? TitleId, string? RglPath) ReadTitle(string installPath)
    {
        string path = Path.Combine(installPath, "title.rgl");
        try
        {
            if (!File.Exists(path))
                return (null, null);

            string text = File.ReadAllText(path).Trim();
            if (text.Length == 0 || text.Length > 256 || text.Any(char.IsControl))
                return (null, path);

            return (text, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return (null, null);
        }
    }

    private static bool IsReadFailure(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or System.Security.SecurityException;
}
