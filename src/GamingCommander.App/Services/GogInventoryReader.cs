using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Reads GOG's best-available inventory from <c>GOG.com\Games</c> registry keys and merges
/// any <c>goggame-*.info</c> found in the install folder. Registry-only records are kept so a
/// missing folder still imports; absence from the registry is never treated as removal.
/// </summary>
internal sealed class GogInventoryReader : IStoreInventoryReader
{
    private const string GamesKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games";

    private static readonly IReadOnlySet<string> NoNoisePatterns = new HashSet<string>();

    private readonly IRegistryReader _registry;

    /// <summary>Creates a reader over the supplied registry accessor.</summary>
    public GogInventoryReader(IRegistryReader registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public GameSourceKind Kind => GameSourceKind.Gog;

    /// <inheritdoc />
    public StoreManifestFingerprint? Fingerprint => null;

    /// <inheritdoc />
    public IReadOnlyList<StoreInventoryEntry> Read()
    {
        IReadOnlyList<string> ids;
        try
        {
            ids = _registry.EnumerateSubKeyNames(GamesKey);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var entries = new List<StoreInventoryEntry>(ids.Count);
        foreach (string id in ids)
        {
            string key = GamesKey + "\\" + id;
            IReadOnlyDictionary<string, string> values;
            try
            {
                values = _registry.ReadKeyValues(key);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            string path = Value(values, "path");
            if (string.IsNullOrWhiteSpace(path))
                continue;

            path = PathText.TrimTrailingSeparators(path);
            string gameId = Value(values, "gameID");
            if (string.IsNullOrWhiteSpace(gameId))
                gameId = id;
            string name = Value(values, "gameName");
            string? buildId = ValueOrNull(values, "BUILDID");
            string? installDate = ValueOrNull(values, "INSTALLDATE");
            string? launchCommand = ValueOrNull(values, "launchCommand");

            bool folderExists = Directory.Exists(path);
            string? launchExe = null;
            string? launchArguments = null;
            bool fromInfo = false;

            if (folderExists)
            {
                var gameDir = new DirectoryInfo(path);
                if (GogInfoParser.TryParse(gameDir, NoNoisePatterns, out GogInfoParser.GogGameInfo? info)
                    && info is not null)
                {
                    fromInfo = true;
                    if (!string.IsNullOrWhiteSpace(info.Title))
                        name = info.Title;
                    if (!string.IsNullOrWhiteSpace(info.ExePath) && File.Exists(info.ExePath))
                    {
                        launchExe = info.ExePath;
                        launchArguments = info.LaunchArgs;
                    }
                }
            }

            launchExe ??= ResolveRegistryExe(values, path);

            entries.Add(new StoreInventoryEntry(
                Kind,
                gameId,
                string.IsNullOrWhiteSpace(name) ? $"GOG {gameId}" : name,
                path,
                launchExe,
                StoreReportedState.Installed,
                FolderExists: folderExists,
                ExeExists: launchExe is not null && File.Exists(launchExe),
                new GogInventoryMetadata(
                    gameId,
                    buildId,
                    installDate,
                    launchCommand,
                    launchArguments,
                    FromRegistryOnly: !fromInfo)));
        }

        return entries;
    }

    /// <summary>Uses <c>exe</c> or <c>exeFile</c> only when the file really exists under the install root.</summary>
    private static string? ResolveRegistryExe(IReadOnlyDictionary<string, string> values, string installPath)
    {
        string exe = Value(values, "exe");
        if (!string.IsNullOrWhiteSpace(exe))
        {
            string candidate = Path.IsPathRooted(exe)
                ? exe
                : Path.Combine(installPath, exe);
            if (File.Exists(candidate))
                return candidate;
        }

        string exeFile = Value(values, "exeFile");
        if (!string.IsNullOrWhiteSpace(exeFile))
        {
            string candidate = Path.IsPathRooted(exeFile)
                ? exeFile
                : Path.Combine(installPath, exeFile);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string Value(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out string? value) ? value : string.Empty;

    private static string? ValueOrNull(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
