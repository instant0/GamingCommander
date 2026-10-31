using System.Globalization;
using System.Text;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Writes the identification ACF subset from <c>docs/research/steam_acf_schema.md</c>.
/// Needs a real Steam AppID. Does not invent depot blocks. Creates only: an existing
/// ACF is never overwritten.
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
        sb.AppendLine($"\"{SteamAcfFields.AppState}\"");
        sb.AppendLine("{");
        Append(sb, SteamAcfFields.AppId, appId.Trim());
        Append(sb, SteamAcfFields.Universe, "1");
        Append(sb, SteamAcfFields.Name, name.Trim());
        Append(sb, SteamAcfFields.StateFlags, "4");
        Append(sb, SteamAcfFields.InstallDir, installdir.Trim());
        Append(sb, SteamAcfFields.LastUpdated, unix);
        Append(sb, SteamAcfFields.SizeOnDisk, string.IsNullOrWhiteSpace(sizeOnDisk) ? "0" : sizeOnDisk.Trim());
        Append(sb, SteamAcfFields.BuildId, string.IsNullOrWhiteSpace(buildId) ? "0" : buildId.Trim());
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Returns where the ACF would go and whether the folder would accept it. Writes and
    /// creates nothing; this is what the shell shows before asking.
    /// </summary>
    public static RepairPlan Preview(
        string libraryRoot,
        string appId,
        string name,
        string installdir) =>
        Prepare(libraryRoot, appId, name, installdir, out _);

    /// <summary>
    /// Creates <c>steamapps/appmanifest_{appId}.acf</c> through the shared guard. False and
    /// no target when the folder is not a Steam library, the ACF exists, or the copy fails.
    /// </summary>
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

        RepairPlan plan = Prepare(libraryRoot, appId, name, installdir, out string content);
        path = plan.IsValid ? plan.TargetPath : string.Empty;
        if (!plan.IsValid)
        {
            error = plan.Message;
            Journal(plan, 0, "refused", error);
            return false;
        }

        string? stagedPath = null;
        try
        {
            if (!RepairWriteGuard.TryStage(
                    $"appmanifest_{appId.Trim()}_{Guid.NewGuid():N}.acf",
                    content,
                    staged => ParsesAcfFile(staged, libraryRoot, appId, installdir),
                    out string staged,
                    out error))
            {
                Journal(plan, RepairWriteGuard.LengthOf(plan.TargetPath), "failed", error);
                return false;
            }

            stagedPath = staged;
            if (!RepairWriteGuard.TryCopyAndVerify(
                    stagedPath,
                    plan.TargetPath,
                    backupPath: null,
                    parses: staged => ParsesAcfFile(staged, libraryRoot, appId, installdir),
                    out error))
            {
                Journal(plan, RepairWriteGuard.LengthOf(plan.TargetPath), "failed", error);
                return false;
            }

            Journal(plan, RepairWriteGuard.LengthOf(plan.TargetPath), "ok", "ACF created.");
            return true;
        }
        finally
        {
            if (stagedPath is not null)
                RepairWriteGuard.DeleteStaged(stagedPath);
        }
    }

    public static bool IsAppId(string? appId) =>
        !string.IsNullOrWhiteSpace(appId) && appId.Trim().All(char.IsDigit);

    private static RepairPlan Prepare(
        string libraryRoot,
        string appId,
        string name,
        string installdir,
        out string content)
    {
        content = string.Empty;

        if (!IsAppId(appId))
            return Invalid(string.Empty, "Steam AppID must be digits.");

        if (string.IsNullOrWhiteSpace(libraryRoot) || string.IsNullOrWhiteSpace(installdir))
            return Invalid(string.Empty, "Library root and install folder are required.");

        string steamapps = Path.Combine(libraryRoot, "steamapps");
        string target = Path.Combine(steamapps, "appmanifest_" + appId.Trim() + ".acf");

        if (!RepairWriteGuard.TryPreflight(
                steamapps, target, RepairWriteKind.SteamAcfCreate, out RepairPlan plan, out string error))
        {
            return plan with { IsValid = false, Message = error };
        }

        content = Build(appId, name, installdir);
        if (!ParsesAcfText(content, appId, installdir))
            return plan with { IsValid = false, Message = "The built ACF did not parse; nothing was written." };

        return plan with
        {
            StagedLength = Encoding.UTF8.GetByteCount(content),
            Message = $"Create {Path.GetFileName(target)} ({Encoding.UTF8.GetByteCount(content)} bytes).",
        };
    }

    /// <summary>Parses a staged or copied ACF through the scanner's own parser.</summary>
    private static bool ParsesAcfFile(
        string path,
        string libraryRoot,
        string appId,
        string installdir)
    {
        AcfInfo? info = SteamAcfParser.ParseAcfFile(path, libraryRoot);
        return info is not null
            && string.Equals(info.AppId, appId.Trim(), StringComparison.Ordinal)
            && string.Equals(info.Installdir, installdir.Trim(), StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(info.Name);
    }

    /// <summary>In-memory check before a byte reaches staging.</summary>
    private static bool ParsesAcfText(string text, string appId, string installdir)
    {
        var fields = VdfParser.ExtractFields(
            text,
            new[]
            {
                SteamAcfFields.AppId, SteamAcfFields.Name, SteamAcfFields.InstallDir,
                SteamAcfFields.StateFlags, SteamAcfFields.LastUpdated,
                SteamAcfFields.SizeOnDisk, SteamAcfFields.BuildId,
            });
        if (fields is null)
            return false;

        return string.Equals(fields.GetValueOrDefault(SteamAcfFields.AppId), appId.Trim(), StringComparison.Ordinal)
            && string.Equals(fields.GetValueOrDefault(SteamAcfFields.InstallDir), installdir.Trim(), StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(fields.GetValueOrDefault(SteamAcfFields.Name));
    }

    private static RepairPlan Invalid(string target, string message) =>
        new(target, null, 0, string.Empty, false, message);

    private static void Journal(RepairPlan plan, long finalLength, string outcome, string message) =>
        RepairWriteGuard.Journal(
            RepairWriteGuard.SteamAction,
            plan.TargetPath,
            plan.BackupPath,
            plan.StagedLength,
            finalLength,
            outcome,
            message);

    private static void Append(StringBuilder sb, string key, string value)
    {
        sb.Append("\t\"");
        sb.Append(key);
        sb.Append("\"\t\t\"");
        sb.Append(value.Replace("\\", "\\\\").Replace("\"", "\\\""));
        sb.AppendLine("\"");
    }
}
