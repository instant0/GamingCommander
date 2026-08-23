using System.Text.Json;

namespace GamingCommander.App.Services;

/// <summary>Writes an identification .item from .egstore/*.mancpn + a launch exe. No GraphQL.</summary>
internal static class EpicItemWriter
{
    public static bool TryWrite(string gameFolder, string manifestsDir, out string path, out string error)
    {
        path = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(gameFolder) || !Directory.Exists(gameFolder))
        {
            error = "Game folder missing.";
            return false;
        }

        if (!TryReadMancpn(gameFolder, out string ns, out string itemId, out string app, out string guid))
        {
            error = "No .egstore/*.mancpn (need catalog ids).";
            return false;
        }

        string exeRel = FindLaunchExe(gameFolder);
        string install = gameFolder.Replace('/', '\\');
        string fileName = guid + ".item";
        try
        {
            Directory.CreateDirectory(manifestsDir);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        path = Path.Combine(manifestsDir, fileName);
        if (File.Exists(path))
        {
            error = "Epic .item already exists.";
            return false;
        }

        string folder = Path.GetFileName(gameFolder.TrimEnd('\\', '/'));
        var payload = new Dictionary<string, object?>
        {
            ["FormatVersion"] = 0,
            ["bIsIncompleteInstall"] = false,
            ["LaunchCommand"] = "",
            ["LaunchExecutable"] = exeRel.Replace('/', '\\'),
            ["ManifestLocation"] = install + "/.egstore",
            ["bIsApplication"] = true,
            ["bIsExecutable"] = exeRel.Length > 0,
            ["AppCategories"] = new[] { "public", "games", "applications" },
            ["DisplayName"] = TitleTextSafe(folder),
            ["InstallationGuid"] = guid,
            ["InstallLocation"] = install,
            ["StagingLocation"] = install + "\\.egstore\\bps",
            ["TechnicalType"] = "public,games,applications",
            ["MandatoryAppFolderName"] = folder,
            ["CatalogNamespace"] = ns,
            ["CatalogItemId"] = itemId,
            ["AppName"] = app,
            ["MainGameCatalogNamespace"] = ns,
            ["MainGameCatalogItemId"] = itemId,
            ["MainGameAppName"] = app,
        };

        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string TitleTextSafe(string folder)
    {
        try
        {
            return Core.Services.TitleText.FromFolderName(folder);
        }
        catch
        {
            return folder;
        }
    }

    private static bool TryReadMancpn(
        string gameFolder, out string ns, out string itemId, out string app, out string guid)
    {
        ns = itemId = app = guid = "";
        foreach (string store in new[] { ".egstore", ".egsstore" })
        {
            string dir = Path.Combine(gameFolder, store);
            if (!Directory.Exists(dir))
                continue;
            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*.mancpn");
            }
            catch
            {
                continue;
            }

            if (files.Length == 0)
                continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(files[0]));
                JsonElement r = doc.RootElement;
                ns = r.TryGetProperty("CatalogNamespace", out var a) ? a.GetString() ?? "" : "";
                itemId = r.TryGetProperty("CatalogItemId", out var b) ? b.GetString() ?? "" : "";
                app = r.TryGetProperty("AppName", out var c) ? c.GetString() ?? "" : "";
                guid = Path.GetFileNameWithoutExtension(files[0]);
                if (ns.Length > 0 && itemId.Length > 0 && app.Length > 0)
                    return true;
            }
            catch (JsonException)
            {
            }
        }

        return false;
    }

    private static string FindLaunchExe(string gameFolder)
    {
        foreach (string rel in new[] { "Binaries/Win64", "Binaries/Win32", "Binaries", "" })
        {
            string dir = string.IsNullOrEmpty(rel) ? gameFolder : Path.Combine(gameFolder, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(dir))
                continue;
            string[] exes;
            try
            {
                exes = Directory.GetFiles(dir, "*.exe");
            }
            catch
            {
                continue;
            }

            foreach (string exe in exes)
            {
                if (ExecutableDiscovery.IsForbiddenLaunchExe(exe))
                    continue;
                string stem = Path.GetFileNameWithoutExtension(exe);
                if (stem.Equals("UE3Redist", StringComparison.OrdinalIgnoreCase))
                    continue;
                return Path.GetRelativePath(gameFolder, exe).Replace('/', '\\');
            }
        }

        return "";
    }
}
