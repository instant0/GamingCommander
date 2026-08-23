using System.Globalization;
using System.Text;

namespace GamingCommander.App.Services;

/// <summary>
/// Writes the identification ACF subset from <c>docs/research/steam_acf_schema.md</c>.
/// Needs a real Steam AppID. Does not invent depot blocks.
/// </summary>
internal static class SteamAcfWriter
{
    public static string Build(
        string appId,
        string name,
        string installdir,
        string? sizeOnDisk = null,
        string? lastUpdated = null,
        string? buildId = null)
    {
        string unix = lastUpdated ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine("\"AppState\"");
        sb.AppendLine("{");
        Append(sb, "appid", appId.Trim());
        Append(sb, "Universe", "1");
        Append(sb, "name", name.Trim());
        Append(sb, "StateFlags", "4");
        Append(sb, "installdir", installdir.Trim());
        Append(sb, "LastUpdated", unix);
        Append(sb, "SizeOnDisk", string.IsNullOrWhiteSpace(sizeOnDisk) ? "0" : sizeOnDisk.Trim());
        Append(sb, "buildid", string.IsNullOrWhiteSpace(buildId) ? "0" : buildId.Trim());
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Writes <c>steamapps/appmanifest_{appId}.acf</c>. False if AppID bad or file exists.</summary>
    public static bool TryWrite(
        string libraryRoot,
        string appId,
        string name,
        string installdir,
        out string path,
        out string error)
    {
        path = string.Empty;
        error = string.Empty;
        if (!IsAppId(appId))
        {
            error = "Steam AppID must be digits.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(libraryRoot) || string.IsNullOrWhiteSpace(installdir))
        {
            error = "Library root and install folder are required.";
            return false;
        }

        string steamapps = Path.Combine(libraryRoot, "steamapps");
        try
        {
            Directory.CreateDirectory(steamapps);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        path = Path.Combine(steamapps, "appmanifest_" + appId.Trim() + ".acf");
        if (File.Exists(path))
        {
            error = "ACF already exists.";
            return false;
        }

        try
        {
            File.WriteAllText(path, Build(appId, name, installdir));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool IsAppId(string? appId) =>
        !string.IsNullOrWhiteSpace(appId) && appId.Trim().All(char.IsDigit);

    private static void Append(StringBuilder sb, string key, string value)
    {
        sb.Append("\t\"");
        sb.Append(key);
        sb.Append("\"\t\t\"");
        sb.Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
        sb.AppendLine("\"");
    }
}
