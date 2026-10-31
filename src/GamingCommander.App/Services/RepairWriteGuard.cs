using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>Which repair write a plan describes; the kind selects the presence rule and the undo.</summary>
internal enum RepairWriteKind
{
    /// <summary>Create an absent <c>appmanifest_&lt;appId&gt;.acf</c>; delete it if the copy fails.</summary>
    SteamAcfCreate,

    /// <summary>Modify an existing Epic <c>.item</c>; restore the backup if the copy fails.</summary>
    EpicItemModify,
}

/// <summary>
/// A dry-run description of one repair write: where it would go, how big the payload is,
/// which marker validated the folder, and whether it would run.
/// </summary>
/// <param name="TargetPath">The store file the write would touch.</param>
/// <param name="BackupPath">Epic backup location; <c>null</c> for Steam, which never overwrites.</param>
/// <param name="StagedLength">Payload length in bytes; zero when the plan is invalid.</param>
/// <param name="Marker">The store marker that validated the folder.</param>
/// <param name="IsValid">True when preflight accepted the target and payload.</param>
/// <param name="Message">One short sentence for the UI and the journal.</param>
internal sealed record RepairPlan(
    string TargetPath,
    string? BackupPath,
    int StagedLength,
    string Marker,
    bool IsValid,
    string Message);

/// <summary>
/// The one guard both repair writers use: preflight, stage, copy, length check, undo, journal.
/// It deliberately has no store-specific code beyond the presence rule and the undo selected by
/// <see cref="RepairWriteKind"/>.
/// </summary>
internal static class RepairWriteGuard
{
    internal const string SteamAction = "steam-acf";

    internal const string EpicAction = "epic-item";

    internal const string JournalFileName = "repair-journal.jsonl";

    /// <summary>Journal location, under the application data root.</summary>
    internal static string JournalPath => Path.Combine(AppPaths.AppDataRoot, JournalFileName);

    /// <summary>
    /// Validates a store folder and target without writing anything. A missing directory is a
    /// refusal: this guard never creates a launcher-owned folder.
    /// </summary>
    internal static bool TryPreflight(
        string targetDir,
        string targetPath,
        RepairWriteKind kind,
        out RepairPlan plan,
        out string error)
    {
        plan = new RepairPlan(targetPath, null, 0, string.Empty, false, string.Empty);
        error = string.Empty;

        if (!IsSafeAbsolutePath(targetDir, out error))
            return Refuse(out plan, targetPath, error);

        if (!IsSafeAbsolutePath(targetPath, out error))
            return Refuse(out plan, targetPath, error);

        if (!PathText.IsUnder(targetPath, targetDir))
        {
            error = $"Target is outside the store folder: '{targetPath}'.";
            return Refuse(out plan, targetPath, error);
        }

        if (!Directory.Exists(targetDir))
        {
            error = $"Store folder does not exist and will not be created: '{targetDir}'.";
            return Refuse(out plan, targetPath, error);
        }

        return kind == RepairWriteKind.SteamAcfCreate
            ? SteamPreflight(targetDir, targetPath, out plan, out error)
            : EpicPreflight(targetDir, targetPath, out plan, out error);
    }

    /// <summary>
    /// Writes <paramref name="content"/> under <c>data/staging/</c>, reads it back and lets the
    /// caller prove it parses. The bytes that pass here are the bytes that get copied.
    /// </summary>
    internal static bool TryStage(
        string name,
        string content,
        Func<string, bool> parses,
        out string stagedPath,
        out string error)
    {
        stagedPath = string.Empty;
        error = string.Empty;

        try
        {
            stagedPath = AppPaths.EnsureAppDataPath(
                Path.Combine(AppPaths.AppDataRoot, "staging", SafeStagingName(name)));
            File.WriteAllText(stagedPath, content);

            if (!parses(stagedPath))
            {
                error = "The staged file did not parse; nothing was written to the store folder.";
                DeleteQuietly(stagedPath);
                stagedPath = string.Empty;
                return false;
            }

            return true;
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            error = ex.Message;
            DeleteQuietly(stagedPath);
            stagedPath = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Copies the staged bytes to the target with one <see cref="File.Copy(string,string,bool)"/>,
    /// compares the length, and re-parses the target. A mismatch or I/O failure is undone:
    /// Steam deletes the file it created, Epic restores the backup.
    /// </summary>
    /// <param name="copy">
    /// Test seam only. Production passes <c>null</c> and the guard uses <see cref="File.Copy(string,string,bool)"/>.
    /// </param>
    internal static bool TryCopyAndVerify(
        string stagedPath,
        string targetPath,
        string? backupPath,
        Func<string, bool>? parses,
        out string error,
        Action<string, string>? copy = null)
    {
        error = string.Empty;

        long stagedLength;
        try
        {
            stagedLength = new FileInfo(stagedPath).Length;
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            error = ex.Message;
            return false;
        }

        try
        {
            if (copy is null)
                File.Copy(stagedPath, targetPath, overwrite: true);
            else
                copy(stagedPath, targetPath);

            long finalLength = new FileInfo(targetPath).Length;
            if (finalLength != stagedLength)
            {
                string note = Undo(targetPath, backupPath);
                error = $"Copy length mismatch ({finalLength} of {stagedLength} bytes).{note}";
                return false;
            }

            if (parses is not null && !parses(targetPath))
            {
                string note = Undo(targetPath, backupPath);
                error = $"The copied file did not parse.{note}";
                return false;
            }

            return true;
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            string note = Undo(targetPath, backupPath);
            error = ex.Message + note;
            return false;
        }
    }

    /// <summary>
    /// Copies the original to <paramref name="backupPath"/> under <c>data/backups/</c>. A failed
    /// backup is a refusal: the write must not run without the bytes it can restore.
    /// </summary>
    internal static bool TryCreateBackup(string targetPath, string backupPath, out string error)
    {
        error = string.Empty;
        try
        {
            AppPaths.EnsureAppDataPath(backupPath);
            File.Copy(targetPath, backupPath, overwrite: false);
            return true;
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            error = $"The backup could not be made; nothing was written. {ex.Message}";
            return false;
        }
    }

    /// <summary>Deletes a staged payload; missing is success.</summary>
    internal static void DeleteStaged(string stagedPath) => DeleteQuietly(stagedPath);

    /// <summary>
    /// Appends one JSON line to <c>data/repair-journal.jsonl</c>. Journal failure is swallowed:
    /// a full disk must not turn a successful repair into a reported failure.
    /// </summary>
    internal static void Journal(
        string action,
        string targetPath,
        string? backupPath,
        long stagedLength,
        long finalLength,
        string outcome,
        string message)
    {
        try
        {
            string line = JsonSerializer.Serialize(new
            {
                timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                action,
                target = targetPath,
                backup = backupPath,
                stagedLength,
                finalLength,
                outcome,
                message,
            });
            File.AppendAllText(AppPaths.EnsureAppDataPath(JournalPath), line + Environment.NewLine);
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            // A journal that cannot be written must never break the repair itself.
        }
    }

    /// <summary>Length of the target when it exists, otherwise zero. Used for journaling.</summary>
    internal static long LengthOf(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            // The target is absent or unreadable; zero is the honest answer for the journal.
            return 0;
        }
    }

    /// <summary>Builds the timestamped Epic backup path; nothing is created.</summary>
    internal static string NewBackupPath(string targetPath)
    {
        string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture);
        return Path.Combine(AppPaths.AppDataRoot, "backups", stamp, Path.GetFileName(targetPath));
    }

    private static bool SteamPreflight(
        string targetDir,
        string targetPath,
        out RepairPlan plan,
        out string error)
    {
        const string marker = "libraryfolders.vdf";
        if (!File.Exists(Path.Combine(targetDir, marker)))
        {
            error = $"Folder is not a Steam library: '{targetDir}' has no {marker}.";
            return Refuse(out plan, targetPath, error);
        }

        if (File.Exists(targetPath))
        {
            error = "ACF already exists; this writer never overwrites.";
            return Refuse(out plan, targetPath, error);
        }

        plan = new RepairPlan(targetPath, null, 0, marker, true, "Ready to create the ACF.");
        error = string.Empty;
        return true;
    }

    private static bool EpicPreflight(
        string targetDir,
        string targetPath,
        out RepairPlan plan,
        out string error)
    {
        if (!File.Exists(targetPath))
        {
            error = "The .item is missing; Epic must write it first. This build cannot create one.";
            return Refuse(out plan, targetPath, error);
        }

        if (!TryValidateEpicItem(targetPath, out error))
            return Refuse(out plan, targetPath, error);

        if (!HasAnotherItem(targetDir, targetPath))
        {
            error = $"Folder is not the Epic manifests folder used by the scan: '{targetDir}' "
                + "contains no other .item file.";
            return Refuse(out plan, targetPath, error);
        }

        plan = new RepairPlan(
            targetPath,
            NewBackupPath(targetPath),
            0,
            "existing .item in the manifests folder",
            true,
            "Ready to update the existing .item.");
        return true;
    }

    private static bool TryValidateEpicItem(string targetPath, out string error)
    {
        error = string.Empty;
        try
        {
            JsonObject? item = JsonNode.Parse(File.ReadAllText(targetPath)) as JsonObject;
            if (item is null)
            {
                error = "The .item could not be read; nothing was written.";
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
                if (!item.TryGetPropertyValue(member, out JsonNode? node)
                    || node is not JsonValue value
                    || !value.TryGetValue(out string? text)
                    || string.IsNullOrWhiteSpace(text))
                {
                    error = $"The .item has no {member}; nothing was written.";
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException
            or InvalidOperationException)
        {
            error = "The .item could not be read; nothing was written.";
            return false;
        }
    }

    private static bool HasAnotherItem(string targetDir, string targetPath)
    {
        try
        {
            return Directory.EnumerateFiles(targetDir, "*.item").Any(
                file => !PathText.Equals(file, targetPath));
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            return false;
        }
    }

    private static bool IsSafeAbsolutePath(string path, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "A store folder and target are required.";
            return false;
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal))
        {
            error = $"UNC paths are not allowed: '{path}'.";
            return false;
        }

        if (!Path.IsPathFullyQualified(path))
        {
            error = $"Path must be absolute: '{path}'.";
            return false;
        }

        return true;
    }

    private static bool Refuse(out RepairPlan plan, string targetPath, string error)
    {
        plan = new RepairPlan(targetPath, null, 0, string.Empty, false, error);
        return false;
    }

    private static string Undo(string targetPath, string? backupPath)
    {
        try
        {
            if (backupPath is null)
            {
                // File.Delete does not throw when the file is absent: that is the desired state.
                File.Delete(targetPath);
                return " The newly created file was removed.";
            }

            File.Copy(backupPath, targetPath, overwrite: true);
            return $" The original was restored from '{backupPath}'.";
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
            return backupPath is null
                ? $" The new file could not be removed: {ex.Message}"
                : $" The backup could not be restored automatically; restore '{backupPath}' by hand. {ex.Message}";
        }
    }

    private static string SafeStagingName(string name)
    {
        string leaf = Path.GetFileName(name);
        return string.IsNullOrWhiteSpace(leaf) || leaf is "." or ".."
            ? "repair-" + Guid.NewGuid().ToString("N")
            : leaf;
    }

    private static void DeleteQuietly(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (IsWriteFailure(ex))
        {
        }
    }

    private static bool IsWriteFailure(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or NotSupportedException
            or ArgumentException or InvalidOperationException;
}
