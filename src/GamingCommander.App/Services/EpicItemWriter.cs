using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Modifies an Epic <c>.item</c> Epic already wrote, after the user moved the game folder.
/// It never creates an item and never replaces one that does not parse: only the members this
/// build owns are updated, and every other member keeps its value and position.
/// </summary>
internal static class EpicItemWriter
{
    public static bool TryWrite(
        string gameFolder,
        string manifestsDir,
        out string path,
        out string error,
        string? displayName = null) =>
        TryWrite(gameFolder, manifestsDir, out path, out _, out error, displayName);

    /// <summary>
    /// Updates an existing <c>.item</c> through the shared guard. False and no write when the
    /// item is absent or unreadable, the folder is not the scan's manifests directory, or the
    /// backup/copy cannot be completed.
    /// </summary>
    public static bool TryWrite(
        string gameFolder,
        string manifestsDir,
        out string path,
        out string backupPath,
        out string error,
        string? displayName = null)
    {
        path = string.Empty;
        backupPath = string.Empty;
        error = string.Empty;
        _ = displayName; // The item's own DisplayName is preserved; this is the UI's label.

        RepairPlan plan = Prepare(gameFolder, manifestsDir, out string content);
        path = plan.IsValid ? plan.TargetPath : string.Empty;
        backupPath = plan.IsValid ? plan.BackupPath ?? string.Empty : string.Empty;
        if (!plan.IsValid)
        {
            error = plan.Message;
            Journal(plan, 0, "refused", error);
            return false;
        }

        string? stagedPath = null;
        try
        {
            int memberCount = MemberCountOf(plan.TargetPath);
            if (!RepairWriteGuard.TryStage(
                    Path.GetFileNameWithoutExtension(plan.TargetPath) + "_" + Guid.NewGuid().ToString("N") + ".item",
                    content,
                    staged => ParsesItemFile(staged, memberCount),
                    out string staged,
                    out error))
            {
                Journal(plan, 0, "failed", error);
                return false;
            }

            stagedPath = staged;
            if (!RepairWriteGuard.TryCreateBackup(plan.TargetPath, plan.BackupPath!, out error))
            {
                Journal(plan, RepairWriteGuard.LengthOf(plan.TargetPath), "failed", error);
                return false;
            }

            if (!RepairWriteGuard.TryCopyAndVerify(
                    stagedPath,
                    plan.TargetPath,
                    plan.BackupPath,
                    staged => ParsesItemFile(staged, memberCount),
                    out error))
            {
                Journal(plan, RepairWriteGuard.LengthOf(plan.TargetPath), "failed", error);
                return false;
            }

            Journal(plan, RepairWriteGuard.LengthOf(plan.TargetPath), "ok", "Existing .item updated.");
            return true;
        }
        finally
        {
            if (stagedPath is not null)
                RepairWriteGuard.DeleteStaged(stagedPath);
        }
    }

    /// <summary>
    /// Returns where the update would go, its backup, and the payload length. Writes and creates
    /// nothing; the shell shows this before asking.
    /// </summary>
    public static RepairPlan Preview(string gameFolder, string manifestsDir) =>
        Prepare(gameFolder, manifestsDir, out _);

    private static RepairPlan Prepare(string gameFolder, string manifestsDir, out string content)
    {
        content = string.Empty;

        if (string.IsNullOrWhiteSpace(gameFolder) || !Directory.Exists(gameFolder))
            return Invalid(string.Empty, "Game folder missing.");

        if (string.IsNullOrWhiteSpace(manifestsDir))
            return Invalid(string.Empty, "Epic manifests folder is unknown.");

        string guid = FindManifestGuid(gameFolder);
        if (string.IsNullOrEmpty(guid))
            TryReadMancpn(gameFolder, out _, out _, out _, out guid);
        if (string.IsNullOrEmpty(guid))
            return Invalid(string.Empty, "No .egstore/*.manifest (need InstallationGuid).");

        string target = Path.Combine(manifestsDir, guid + ".item");
        if (!RepairWriteGuard.TryPreflight(
                manifestsDir, target, RepairWriteKind.EpicItemModify, out RepairPlan plan, out string error))
        {
            return plan with { IsValid = false, Message = error };
        }

        try
        {
            JsonObject? item = JsonNode.Parse(File.ReadAllText(target)) as JsonObject;
            if (item is null)
                return plan with { IsValid = false, Message = "The .item could not be read; nothing was written." };

            string existingGuid = StringMember(item, "InstallationGuid");
            if (!string.Equals(existingGuid, guid, StringComparison.OrdinalIgnoreCase))
                return plan with
                {
                    IsValid = false,
                    Message = $"'{Path.GetFileName(target)}' names a different installation; nothing was written.",
                };

            int memberCount = item.Count;
            string install = ToDocumentedInstall(gameFolder);
            item[EpicItemSchema.InstallLocation] = install;

            // Only existing members are updated: adding one would change the count that proves
            // unknown members were preserved. Real items always carry ManifestLocation.
            if (item.ContainsKey("ManifestLocation"))
                item["ManifestLocation"] = install + "/.egstore";

            if (item[EpicItemSchema.LaunchExecutable] is JsonValue launchValue
                && launchValue.TryGetValue(out string? currentLaunch)
                && string.IsNullOrWhiteSpace(currentLaunch))
            {
                string launch = FindLaunchExe(gameFolder);
                if (launch.Length > 0)
                    item[EpicItemSchema.LaunchExecutable] = launch;
            }

            if (item.Count != memberCount)
                return plan with
                {
                    IsValid = false,
                    Message = "Refusing to write: the member count changed; nothing was written.",
                };

            content = item.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            if (!ParsesItemText(content, memberCount, out error))
                return plan with { IsValid = false, Message = error };

            int bytes = Encoding.UTF8.GetByteCount(content);
            return plan with
            {
                StagedLength = bytes,
                Message = $"Update {Path.GetFileName(target)} ({bytes} bytes); "
                    + $"backup to {plan.BackupPath}.",
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException
            or InvalidOperationException)
        {
            return plan with { IsValid = false, Message = "The .item could not be read; nothing was written." };
        }
    }

    /// <summary>Parses a staged/copied file and requires the pre-modification member count.</summary>
    private static bool ParsesItemFile(string path, int expectedMemberCount)
    {
        try
        {
            return ParsesItemText(File.ReadAllText(path), expectedMemberCount, out _);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool ParsesItemText(string text, int expectedMemberCount, out string error)
    {
        error = string.Empty;
        try
        {
            if (JsonNode.Parse(text) is not JsonObject item)
            {
                error = "The built .item did not parse; nothing was written.";
                return false;
            }

            if (item.Count != expectedMemberCount)
            {
                error = "The built .item changed the member count; nothing was written.";
                return false;
            }

            foreach (string member in new[]
                     {
                         "InstallationGuid",
                         EpicItemSchema.InstallLocation,
                         EpicItemSchema.CatalogNamespace,
                         EpicItemSchema.CatalogItemId,
                     })
            {
                if (string.IsNullOrWhiteSpace(StringMember(item, member)))
                {
                    error = $"The built .item has no {member}; nothing was written.";
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            error = "The built .item did not parse; nothing was written.";
            return false;
        }
    }

    private static int MemberCountOf(string path)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) is JsonObject item ? item.Count : 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return 0;
        }
    }

    private static string StringMember(JsonObject item, string member) =>
        item[member] is JsonValue value && value.TryGetValue(out string? text) ? text ?? string.Empty : string.Empty;

    private static RepairPlan Invalid(string target, string message) =>
        new(target, null, 0, string.Empty, false, message);

    private static void Journal(RepairPlan plan, long finalLength, string outcome, string message) =>
        RepairWriteGuard.Journal(
            RepairWriteGuard.EpicAction,
            plan.TargetPath,
            plan.BackupPath,
            plan.StagedLength,
            finalLength,
            outcome,
            message);

    /// <summary><c>e:\Games\DeathStranding</c> — lowercase drive, documented in epic_item_format.md.</summary>
    private static string ToDocumentedInstall(string path)
    {
        string win = path.Replace('/', '\\').TrimEnd('\\');
        if (win.Length >= 2 && win[1] == ':')
            win = char.ToLowerInvariant(win[0]) + win[1..];
        return win;
    }

    private static bool TryReadMancpn(
        string gameFolder, out string ns, out string itemId, out string app, out string guid)
    {
        ns = itemId = app = guid = "";
        foreach (string store in new[] { ".egstore", ".egsstore" })
        {
            string dir = Path.Combine(gameFolder, store);
            string[] files;
            try
            {
                // A missing .egstore/.egsstore throws into the catch below.
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
                ns = r.TryGetProperty(EpicItemSchema.CatalogNamespace, out var a) ? a.GetString() ?? "" : "";
                itemId = r.TryGetProperty(EpicItemSchema.CatalogItemId, out var b) ? b.GetString() ?? "" : "";
                app = r.TryGetProperty(EpicItemSchema.AppName, out var c) ? c.GetString() ?? "" : "";
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

    private static string FindManifestGuid(string gameFolder)
    {
        foreach (string store in new[] { ".egstore", ".egsstore" })
        {
            string dir = Path.Combine(gameFolder, store);
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
        // Prefer Binaries/* over the game root; the root ("") is the last resort.
        string exe = CheapExeFinder.FindFirst(
            gameFolder,
            ["Binaries/Win64", "Binaries/Win32", "Binaries", string.Empty],
            IsEpicNoiseExe);
        return exe.Length == 0 ? string.Empty : Path.GetRelativePath(gameFolder, exe).Replace('/', '\\');
    }

    /// <summary>
    /// Epic-specific extra exe-name skips beyond IsForbiddenLaunchExe: middleware
    /// installers and crash-report helpers that must never become the launch exe.
    /// </summary>
    private static bool IsEpicNoiseExe(string exe)
    {
        string stem = Path.GetFileNameWithoutExtension(exe);
        return stem.Equals("UE3Redist", StringComparison.OrdinalIgnoreCase)
            || stem.Contains("crashpad", StringComparison.OrdinalIgnoreCase)
            || stem.Contains("crashreporter", StringComparison.OrdinalIgnoreCase);
    }
}
