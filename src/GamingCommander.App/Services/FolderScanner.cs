using System.Security.Cryptography;
using System.Text;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

public sealed class FolderScanner
{
    public IReadOnlyList<GameEntry> Scan(string rootPath, GameSourceKind defaultType)
    {
        if (!Directory.Exists(rootPath))
            return [];

        var entries = new List<GameEntry>();

        foreach (DirectoryInfo subDir in GetDirectoriesSafe(rootPath))
        {
            GameSourceKind resolvedType = DetectType(subDir, defaultType);
            bool isOverride = resolvedType != defaultType;
            string? exePath = FindPrimaryExecutable(subDir);
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

    private static string? FindPrimaryExecutable(DirectoryInfo dir)
    {
        string[] exeFiles = GetFilesSafe(dir, "*.exe");
        if (exeFiles.Length == 0) return null;

        if (exeFiles.Length == 1) return exeFiles[0];

        string? launcher = FindLauncherExecutableFromList(exeFiles, null);

        return exeFiles
            .Where(f => f != launcher)
            .OrderByDescending(f => new FileInfo(f).Length)
            .FirstOrDefault() ?? launcher;
    }

    private static string? FindLauncherExecutable(DirectoryInfo dir, string? primaryExe)
    {
        string[] exeFiles = GetFilesSafe(dir, "*.exe");
        if (exeFiles.Length <= 1) return null;
        return FindLauncherExecutableFromList(exeFiles, primaryExe);
    }

    private static string? FindLauncherExecutableFromList(string[] exeFiles, string? excludePath)
    {
        string[] launcherNames =
        [
            "launcher", "launch", "updater", "bootstrap", "redlaunch",
            "epicgameslauncher", "goggalaxy", "ea app", "ubisoft",
        ];

        foreach (string exe in exeFiles)
        {
            if (exe == excludePath) continue;
            string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
            if (launcherNames.Any(ln => name.Contains(ln)))
                return exe;
        }

        return null;
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
