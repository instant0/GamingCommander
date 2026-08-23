using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// .item LaunchExecutable is often a store stub (2K LauncherPatcher). Prefer a real game exe.
/// </summary>
internal static class EpicLaunchResolver
{
    private static readonly string[] Launchers = ["launcher", "patcher", "2klauncher", "crash"];
    private static readonly string[] NoiseExe = ["unins", "setup", "vcredist", "dxsetup", "crashpad"];

    public static bool IsStoreLauncher(string? pathOrName)
    {
        if (string.IsNullOrWhiteSpace(pathOrName))
            return false;
        string n = Path.GetFileNameWithoutExtension(pathOrName).ToLowerInvariant();
        return n.Contains("launcher") || n.Contains("patcher") || n.Contains("2klauncher");
    }

    public static (string Exe, string Launcher, IReadOnlyList<string> Candidates) Resolve(
        string installLocation,
        string? itemLaunchExecutable,
        string folderName)
    {
        string itemExe = EpicManifestParser.ResolveLaunchExecutable(
            installLocation, itemLaunchExecutable ?? "");
        if (!string.IsNullOrEmpty(itemExe) && !File.Exists(itemExe))
            itemExe = string.Empty;

        var dir = new DirectoryInfo(installLocation);
        if (!dir.Exists)
            return (itemExe, IsStoreLauncher(itemExe) ? itemExe : "", []);

        var noiseDirs = new HashSet<string>(FileSystemHelper.NoiseSubDirNames, StringComparer.OrdinalIgnoreCase)
        {
            "2klauncher",
        };
        string[] top = FileSystemHelper.GetFilesSafe(dir, "*.exe");
        var found = ExecutableDiscovery.FindPrimaryExecutable(
            dir, top, NoiseExe, noiseDirs, Launchers);

        string exe = found.ExePath ?? "";
        if (IsStoreLauncher(exe) && !string.IsNullOrEmpty(itemExe) && !IsStoreLauncher(itemExe))
            exe = itemExe;
        if (string.IsNullOrEmpty(exe) || IsStoreLauncher(exe))
        {
            if (!string.IsNullOrEmpty(itemExe) && !IsStoreLauncher(itemExe))
                exe = itemExe;
            else if (string.IsNullOrEmpty(exe))
                exe = itemExe;
        }

        string launcher = IsStoreLauncher(itemExe) ? itemExe : "";
        return (exe, launcher, found.Candidates);
    }
}
