using System.Security.Cryptography;
using System.Text;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

public sealed class FolderScanner
{
    private readonly IReadOnlySet<string> _hiddenFolderNames;
    private readonly IReadOnlyList<string> _noiseExePatterns;
    private readonly IReadOnlySet<string> _noiseDirectoryPatterns;
    private readonly IReadOnlyList<string> _launcherPatterns;

    /// <summary>
    /// Default hardcoded patterns for backward compatibility (tests, etc.).
    /// Production code should use the constructor that accepts BlacklistData.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultNoiseExePatterns =
    [
        // Launcher / platform helper executables
        "launcher", "launch", "updater", "bootstrap", "redlaunch",
        "epicgameslauncher", "goggalaxy", "ea app", "eaapp", "ubisoft",
        // Anti-cheat / DRM
        "anticheat", "easyanticheat", "eac", "battleye", "punkbuster",
        // Installers / redistributables
        "installer", "setup", "redist", "commonredist", "vcredist",
        "dxsetup", "oalinst", "dotnetruntime", "directx", "xna",
        // Uninstallers
        "unins", "uninstall",
    ];

    private static readonly IReadOnlyList<string> DefaultLauncherPatterns =
    [
        "launcher", "launch", "updater", "bootstrap", "redlaunch",
        "epicgameslauncher", "goggalaxy", "ea app", "ubisoft",
    ];

    public FolderScanner()
        : this([], DefaultNoiseExePatterns, [], DefaultLauncherPatterns)
    {
    }

    public FolderScanner(IEnumerable<string> hiddenFolderNames)
        : this(hiddenFolderNames, DefaultNoiseExePatterns, [], DefaultLauncherPatterns)
    {
    }

    public FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        BlacklistData blacklist)
        : this(
            hiddenFolderNames,
            blacklist.ExeNamePatterns.Count > 0 ? blacklist.ExeNamePatterns : DefaultNoiseExePatterns,
            blacklist.DirectoryPatterns,
            DefaultLauncherPatterns)
    {
    }

    private FolderScanner(
        IEnumerable<string> hiddenFolderNames,
        IReadOnlyList<string> noiseExePatterns,
        IReadOnlyList<string> noiseDirectoryPatterns,
        IReadOnlyList<string> launcherPatterns)
    {
        _hiddenFolderNames = new HashSet<string>(hiddenFolderNames, StringComparer.OrdinalIgnoreCase);
        _noiseExePatterns = noiseExePatterns;
        _noiseDirectoryPatterns = new HashSet<string>(noiseDirectoryPatterns, StringComparer.OrdinalIgnoreCase);
        _launcherPatterns = launcherPatterns;
    }

    public IReadOnlyList<GameEntry> Scan(string rootPath, GameSourceKind defaultType)
    {
        if (!Directory.Exists(rootPath))
            return [];

        var entries = new List<GameEntry>();

        foreach (DirectoryInfo subDir in GetDirectoriesSafe(rootPath))
        {
            // Skip user-configured hidden folders
            if (_hiddenFolderNames.Count > 0 && _hiddenFolderNames.Contains(subDir.Name))
                continue;

            // Skip known noise/redist directories from blacklist
            if (IsNoiseDirectory(subDir.Name))
                continue;

            // Skip folders that are clearly not game directories:
            // no .exe at top level AND no game marker files anywhere in the subtree
            string[] exeFiles = GetFilesSafe(subDir, "*.exe");
            if (exeFiles.Length == 0 && !HasGameMarkerFile(subDir))
                continue;

            GameSourceKind resolvedType = DetectType(subDir, defaultType);
            bool isOverride = resolvedType != defaultType;
            string? exePath = FindPrimaryExecutable(subDir, exeFiles);
            string? launcherPath = FindLauncherExecutable(subDir, exePath);
            string manifestPath = FindEpicManifest(subDir);
            string displayName = NormalizeDisplayName(subDir.Name);

            string id = ComputeId(rootPath, subDir.Name);

            entries.Add(new GameEntry(
                Id: id,
                FolderName: subDir.Name,
                DisplayName: displayName,
                GameSource: resolvedType,
                Override: isOverride,
                ExecutablePath: exePath ?? string.Empty,
                LauncherPath: launcherPath ?? string.Empty,
                CmdlineArgs: string.Empty,
                ManifestPath: manifestPath,
                LastScanned: DateTimeOffset.UtcNow,
                LastModified: GetLastWriteTimeSafe(subDir),
                Extra: []));
        }

        return entries;
    }

    private bool IsNoiseDirectory(string dirName)
    {
        if (_noiseDirectoryPatterns.Count == 0)
            return false;

        string lower = dirName.ToLowerInvariant();
        return _noiseDirectoryPatterns.Any(p => lower.Contains(p));
    }

    private static GameSourceKind DetectType(DirectoryInfo subDir, GameSourceKind rootDefault)
    {
        string[] markerFiles = Directory.GetFiles(subDir.FullName, "*", SearchOption.AllDirectories);
        string lowerDirName = subDir.Name.ToLowerInvariant();

        foreach (string file in markerFiles)
        {
            string name = Path.GetFileName(file).ToLowerInvariant();

            if (name == "steam_appid.txt")
                return GameSourceKind.Steam;

            if (name == ".egsstore" || name == ".egstore")
                return GameSourceKind.Epic;

            if (name == "goggame.yml" || name == "goggame.info")
                return GameSourceKind.Gog;

            if (name.StartsWith("eaapp_") || name == ".ea.web" || lowerDirName.Contains("ea games") || lowerDirName.Contains("electronic arts"))
                return GameSourceKind.EaApp;

            if (name == "ubisoft game launcher url" || lowerDirName.Contains("ubisoft"))
                return GameSourceKind.UbisoftConnect;
        }

        return rootDefault;
    }

    private string? FindPrimaryExecutable(DirectoryInfo dir, string[] exeFiles)
    {
        if (exeFiles.Length == 0) return null;
        if (exeFiles.Length == 1) return exeFiles[0];

        var candidates = new List<string>();
        var excluded = new List<string>();

        foreach (string exe in exeFiles)
        {
            if (IsNonGameExe(exe))
                excluded.Add(exe);
            else
                candidates.Add(exe);
        }

        // No candidates at all — fall back to largest excluded exe
        if (candidates.Count == 0)
            return excluded.OrderByDescending(f => new FileInfo(f).Length).First();

        // Prefer exe whose name matches the parent folder name
        string folderName = dir.Name;
        var nameMatches = candidates.Where(exe => ExeNameMatchesFolderName(exe, folderName)).ToList();

        if (nameMatches.Count > 0)
            return nameMatches.OrderByDescending(f => new FileInfo(f).Length).First();

        // Fall back to largest candidate by file size
        return candidates.OrderByDescending(f => new FileInfo(f).Length).First();
    }

    private string? FindLauncherExecutable(DirectoryInfo dir, string? primaryExe)
    {
        string[] exeFiles = GetFilesSafe(dir, "*.exe");
        if (exeFiles.Length <= 1) return null;
        return FindLauncherExecutableFromList(exeFiles, primaryExe);
    }

    private string? FindLauncherExecutableFromList(string[] exeFiles, string? excludePath)
    {
        foreach (string exe in exeFiles)
        {
            if (exe == excludePath) continue;
            string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
            if (_launcherPatterns.Any(ln => name.Contains(ln)))
                return exe;
        }

        return null;
    }

    private bool IsNonGameExe(string exePath)
    {
        string name = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
        return _noiseExePatterns.Any(p => name.Contains(p));
    }

    private static bool ExeNameMatchesFolderName(string exePath, string folderName)
    {
        string exeStem = Path.GetFileNameWithoutExtension(exePath);
        string folderLower = folderName.ToLowerInvariant();
        string exeLower = exeStem.ToLowerInvariant();

        // Direct substring match either direction
        if (folderLower.Contains(exeLower) || exeLower.Contains(folderLower))
            return true;

        // Token-level matching (split on spaces, underscores, hyphens, dots, colons)
        char[] separators = [' ', '_', '-', '.', ':'];
        string[] folderTokens = folderLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        string[] exeTokens = exeLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        return folderTokens.Any(t => exeTokens.Contains(t) && t.Length > 1);
    }

    private static bool HasGameMarkerFile(DirectoryInfo dir)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(dir.FullName, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file).ToLowerInvariant();

                if (name is "steam_appid.txt" or ".egsstore" or ".egstore"
                    or "goggame.yml" or "goggame.info"
                    or "ubisoft game launcher url")
                    return true;

                if (name.StartsWith("eaapp_") || name == ".ea.web")
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static string FindEpicManifest(DirectoryInfo dir)
    {
        string[] egsPaths =
        [
            Path.Combine(dir.FullName, ".egsstore", "manifests"),
            Path.Combine(dir.FullName, ".egstore", "manifests"),
            Path.Combine(dir.FullName, "manifests"),
        ];

        foreach (string manifestsDir in egsPaths)
        {
            if (!Directory.Exists(manifestsDir)) continue;

            try
            {
                foreach (FileInfo jsonFile in new DirectoryInfo(manifestsDir).GetFiles("*.json"))
                {
                    return jsonFile.FullName;
                }
            }
            catch
            {
            }
        }

        return string.Empty;
    }

    private static string NormalizeDisplayName(string folderName)
    {
        return folderName
            .Replace("Remastered", "")
            .Replace("Definitive Edition", "")
            .Replace("Enhanced Edition", "")
            .Replace("Ultimate Edition", "")
            .Replace("Special Edition", "")
            .Replace("GOTY", "")
            .Replace("Edition", "")
            .Replace("_", " ")
            .Replace("-", " ")
            .Trim();
    }

    private static string ComputeId(string rootPath, string folderName)
    {
        string combined = $"{rootPath}|{folderName}";
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static DirectoryInfo[] GetDirectoriesSafe(string path)
    {
        try
        {
            return new DirectoryInfo(path).GetDirectories();
        }
        catch
        {
            return [];
        }
    }

    private static string[] GetFilesSafe(DirectoryInfo dir, string pattern)
    {
        try
        {
            return dir.GetFiles(pattern, SearchOption.TopDirectoryOnly)
                .Select(f => f.FullName)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)
    {
        try
        {
            return dir.LastWriteTime;
        }
        catch
        {
            return DateTimeOffset.MinValue;
        }
    }
}
