using System.Text.Json;

namespace GamingCommander.App.Services;

/// <summary>Writes an identification .item from .egstore/*.mancpn + a launch exe. No GraphQL.</summary>
internal static class EpicItemWriter
{
    public static bool TryWrite(
        string gameFolder,
        string manifestsDir,
        out string path,
        out string error,
        string? displayName = null)
    {
        path = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(gameFolder) || !Directory.Exists(gameFolder))
        {
            error = "Game folder missing.";
            return false;
        }

        string guid = FindManifestGuid(gameFolder);
        if (string.IsNullOrEmpty(guid))
            TryReadMancpn(gameFolder, out _, out _, out _, out guid);
        string manifestPath = Path.Combine(gameFolder, ".egstore", guid + ".manifest");
        if (!File.Exists(manifestPath))
            manifestPath = Path.Combine(gameFolder, ".egsstore", guid + ".manifest");

        string launch = FindLaunchExe(gameFolder).Replace('\\', '/').TrimStart('/');
        string appFromManifest = "";
        if (EpicBinaryManifest.TryRead(manifestPath, out EpicBinaryManifest.Header? head) && head is not null)
        {
            if (!string.IsNullOrWhiteSpace(head.LaunchExe))
                launch = head.LaunchExe.Replace('\\', '/').TrimStart('/');
            appFromManifest = StripStaging(head.AppName);
        }

        string query = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : TitleTextSafe(Path.GetFileName(gameFolder.TrimEnd('\\', '/')));
        string ns = "";
        string itemId = "";
        string title = query;
        string localApp = "";
        TryReadMancpn(gameFolder, out ns, out itemId, out localApp, out _);
        if (ns.Length == 0 || itemId.Length == 0)
            TryReadOvt(gameFolder, out ns, out itemId, out localApp, out _);
        // GraphQL must not replace local catalog ids (documented). Keyword search is Director's Cut.
        if (ns.Length == 0 || itemId.Length == 0)
        {
            error = "No catalog ids in .mancpn/.ovt (GraphQL keyword search not used for ids).";
            return false;
        }

        if (string.IsNullOrEmpty(guid))
        {
            error = "No .egstore/*.manifest (need InstallationGuid).";
            return false;
        }

        try
        {
            Directory.CreateDirectory(manifestsDir);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        path = Path.Combine(manifestsDir, guid + ".item");
        if (File.Exists(path) && !IsHollowItem(path))
        {
            error = "Epic .item already exists.";
            return false;
        }

        string folder = EpicInstallPath.FolderName(gameFolder);
        string install = ToDocumentedInstall(gameFolder);
        // Documented: AppName = .manifest strip Staging (Boga). Never .ovt sub.
        string app = !string.IsNullOrEmpty(appFromManifest) ? appFromManifest : folder;

        var payload = new Dictionary<string, object?>
        {
            ["FormatVersion"] = 0,
            ["EoshRevision"] = "",
            ["bIsIncompleteInstall"] = false,
            ["LaunchCommand"] = "",
            ["LaunchExecutable"] = launch,
            ["ManifestLocation"] = install + "/.egstore",
            ["CompleteManifestPath"] = "",
            ["PendingManifestPath"] = "",
            ["ManifestHash"] = "",
            ["SDMetaHash"] = "",
            ["SDMetaLocation"] = "",
            ["bIsApplication"] = true,
            ["bIsExecutable"] = launch.Length > 0,
            ["bIsManaged"] = false,
            ["bNeedsValidation"] = false,
            ["bSDMetaMigrated"] = false,
            ["bRequiresAuth"] = true,
            ["bAllowMultipleInstances"] = false,
            ["bCanRunOffline"] = true,
            ["bAllowUriCmdArgs"] = false,
            ["bLaunchElevated"] = false,
            ["BaseURLs"] = Array.Empty<string>(),
            ["BuildLabel"] = "Live",
            ["AppCategories"] = new[] { "public", "games", "applications" },
            ["ChunkDbs"] = Array.Empty<object>(),
            ["CompatibleApps"] = Array.Empty<string>(),
            ["DisplayName"] = title,
            ["InstallationGuid"] = guid,
            ["InstallLocation"] = install,
            ["InstallSessionId"] = "00000000000000000000000000000000",
            ["InstallTags"] = Array.Empty<string>(),
            ["InstallComponents"] = Array.Empty<string>(),
            ["HostInstallationGuid"] = "00000000000000000000000000000000",
            ["PrereqSHA1Hash"] = "",
            ["LastPrereqSucceededSHA1Hash"] = "",
            ["StagingLocation"] = install + "\\.egstore\\bps",
            ["TechnicalType"] = "public,games,applications",
            ["VaultThumbnailUrl"] = "",
            ["VaultTitleText"] = "",
            ["InstallSize"] = 0,
            ["MainWindowProcessName"] = "",
            ["ProcessNames"] = Array.Empty<string>(),
            ["BackgroundProcessNames"] = Array.Empty<string>(),
            ["IgnoredProcessNames"] = Array.Empty<string>(),
            ["DlcProcessNames"] = Array.Empty<string>(),
            ["MandatoryAppFolderName"] = folder,
            ["OwnershipToken"] = "true",
            ["SidecarConfigRevision"] = 0,
            ["SidecarDeploymentId"] = "",
            ["PreloadState"] = 0,
            ["CatalogNamespace"] = ns,
            ["CatalogItemId"] = itemId,
            ["AppName"] = app,
            ["AllowedUriEnvVars"] = Array.Empty<string>(),
        };

        try
        {
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool IsHollowItem(string itemPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(itemPath));
            JsonElement r = doc.RootElement;
            string eosh = r.TryGetProperty("EoshRevision", out var e) ? e.GetString() ?? "" : "";
            long size = r.TryGetProperty("InstallSize", out var s) && s.TryGetInt64(out long n) ? n : 0;
            return size == 0 && string.IsNullOrWhiteSpace(eosh);
        }
        catch
        {
            return true;
        }
    }

    /// <summary><c>e:\Games\DeathStranding</c> — lowercase drive, documented in epic_item_format.md.</summary>
    private static string ToDocumentedInstall(string path)
    {
        string win = path.Replace('/', '\\').TrimEnd('\\');
        if (win.Length >= 2 && win[1] == ':')
            win = char.ToLowerInvariant(win[0]) + win[1..];
        return win;
    }

    private static string StripStaging(string appName)
    {
        if (appName.EndsWith("Staging", StringComparison.Ordinal))
            return appName[..^7];
        return appName;
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

    /// <summary>
    /// Ownership token leftover after Epic deletes .mancpn.
    /// Death Stranding: folder = AppName, JWT <c>ent[0]</c> = namespace + catalogItemId
    /// (may be a <b>dev</b> namespace — still enough to write an identification .item).
    /// </summary>
    private static bool TryReadOvt(
        string gameFolder, out string ns, out string itemId, out string app, out string guid)
    {
        ns = itemId = app = guid = "";
        foreach (string store in new[] { ".egstore", ".egsstore" })
        {
            string dir = Path.Combine(gameFolder, store);
            if (!Directory.Exists(dir))
                continue;
            string[] ovts;
            try
            {
                ovts = Directory.GetFiles(dir, "*.ovt", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (string ovt in ovts)
            {
                if (!TryParseOvt(ovt, out ns, out itemId, out app))
                    continue;
                guid = FindManifestGuid(gameFolder);
                if (string.IsNullOrEmpty(app))
                    app = Path.GetFileName(Path.GetDirectoryName(ovt) ?? "") ?? "";
                return ns.Length > 0 && itemId.Length > 0;
            }
        }

        return false;
    }

    private static bool TryParseOvt(string ovtPath, out string ns, out string itemId, out string app)
    {
        ns = itemId = app = "";
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ovtPath));
            string? token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;
            if (string.IsNullOrWhiteSpace(token))
                return false;
            if (token.StartsWith("egoc1~", StringComparison.OrdinalIgnoreCase))
                token = token[6..];
            string[] parts = token.Split('.');
            if (parts.Length < 2)
                return false;
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            using var jwt = JsonDocument.Parse(Convert.FromBase64String(payload));
            JsonElement root = jwt.RootElement;
            if (root.TryGetProperty("sub", out var sub))
                app = sub.GetString() ?? "";
            if (!root.TryGetProperty("ent", out var ent) || ent.ValueKind != JsonValueKind.Array)
                return false;
            foreach (JsonElement e in ent.EnumerateArray())
            {
                itemId = e.TryGetProperty("catalogItemId", out var c) ? c.GetString() ?? "" : "";
                ns = e.TryGetProperty("namespace", out var n) ? n.GetString() ?? "" : "";
                if (itemId.Length > 0 && ns.Length > 0)
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string FindManifestGuid(string gameFolder)
    {
        foreach (string store in new[] { ".egstore", ".egsstore" })
        {
            string dir = Path.Combine(gameFolder, store);
            if (!Directory.Exists(dir))
                continue;
            try
            {
                string[] files = Directory.GetFiles(dir, "*.manifest");
                if (files.Length > 0)
                    return Path.GetFileNameWithoutExtension(files[0]);
            }
            catch
            {
            }
        }

        return "";
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
                if (stem.Equals("UE3Redist", StringComparison.OrdinalIgnoreCase)
                    || stem.Contains("crashpad", StringComparison.OrdinalIgnoreCase)
                    || stem.Contains("crashreporter", StringComparison.OrdinalIgnoreCase))
                    continue;
                return Path.GetRelativePath(gameFolder, exe).Replace('/', '\\');
            }
        }

        return "";
    }
}
