namespace GamingCommander.App.Services;

/// <summary>Compare Epic InstallLocation to a scanned game folder (slash/case/trailing junk).</summary>
internal static class EpicInstallPath
{
    public static string Normalize(string path) =>
        path.Replace('/', '\\').Trim().TrimEnd('\\').ToLowerInvariant();

    public static string FolderName(string path)
    {
        string n = Normalize(path);
        int slash = n.LastIndexOf('\\');
        return slash >= 0 ? n[(slash + 1)..] : n;
    }

    public static bool Same(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        string na = Normalize(a);
        string nb = Normalize(b);
        if (na == nb)
            return true;
        string fa = FolderName(na);
        string fb = FolderName(nb);
        return fa.Length >= 3 && fa == fb;
    }
}
