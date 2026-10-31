using System.Text.Json;
using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class RepairWriteGuardTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rwg_" + Guid.NewGuid().ToString("N")[..8]);

    public RepairWriteGuardTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Theory]
    [InlineData("relative/folder")]
    [InlineData(@"\\server\share")]
    public void Preflight_RelativeOrUnc_Refused(string folder)
    {
        string target = Path.Combine(folder, "appmanifest_1.acf");

        Assert.False(RepairWriteGuard.TryPreflight(
            folder, target, RepairWriteKind.SteamAcfCreate, out RepairPlan plan, out string error));

        Assert.False(plan.IsValid);
        Assert.True(error.Contains("absolute", StringComparison.OrdinalIgnoreCase)
            || error.Contains("UNC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preflight_MissingDirectory_RefusedAndNotCreated()
    {
        string missing = Path.Combine(_dir, "missing", "steamapps");
        string target = Path.Combine(missing, "appmanifest_1.acf");

        Assert.False(RepairWriteGuard.TryPreflight(
            missing, target, RepairWriteKind.SteamAcfCreate, out _, out string error));

        Assert.Contains("will not be created", error);
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void Preflight_Steam_RequiresMarkerAndAbsentTarget()
    {
        string steamapps = Path.Combine(_dir, "steamapps");
        Directory.CreateDirectory(steamapps);
        string target = Path.Combine(steamapps, "appmanifest_1.acf");

        Assert.False(RepairWriteGuard.TryPreflight(
            steamapps, target, RepairWriteKind.SteamAcfCreate, out _, out string error));
        Assert.Contains("libraryfolders.vdf", error);

        File.WriteAllText(Path.Combine(steamapps, "libraryfolders.vdf"), "\"libraryfolders\" { }");
        Assert.True(RepairWriteGuard.TryPreflight(
            steamapps, target, RepairWriteKind.SteamAcfCreate, out RepairPlan plan, out error), error);
        Assert.True(plan.IsValid);
        Assert.Equal("libraryfolders.vdf", plan.Marker);
        Assert.Null(plan.BackupPath);

        File.WriteAllText(target, "existing");
        Assert.False(RepairWriteGuard.TryPreflight(
            steamapps, target, RepairWriteKind.SteamAcfCreate, out _, out error));
        Assert.Contains("never overwrites", error);
    }

    [Fact]
    public void Preflight_Epic_RequiresExistingParsableItem()
    {
        string manifests = Path.Combine(_dir, "manifests");
        Directory.CreateDirectory(manifests);
        string target = Path.Combine(manifests, "GUID.item");
        File.WriteAllText(Path.Combine(manifests, "OTHER.item"), "{}");

        Assert.False(RepairWriteGuard.TryPreflight(
            manifests, target, RepairWriteKind.EpicItemModify, out _, out string error));
        Assert.Contains("cannot create", error);

        File.WriteAllText(target, "{ not json");
        Assert.False(RepairWriteGuard.TryPreflight(
            manifests, target, RepairWriteKind.EpicItemModify, out _, out error));
        Assert.Contains("could not be read", error);

        File.WriteAllText(target, """{ "InstallationGuid": "GUID", "InstallLocation": "e:\\g" }""");
        Assert.False(RepairWriteGuard.TryPreflight(
            manifests, target, RepairWriteKind.EpicItemModify, out _, out error));
        Assert.Contains("CatalogNamespace", error);
    }

    [Fact]
    public void Preflight_Epic_ValidItemGetsBackupPath()
    {
        string manifests = Path.Combine(_dir, "manifests");
        Directory.CreateDirectory(manifests);
        string target = Path.Combine(manifests, "GUID.item");
        File.WriteAllText(Path.Combine(manifests, "OTHER.item"), "{}");
        File.WriteAllText(target, """
            {
              "InstallationGuid": "GUID",
              "InstallLocation": "e:\\g",
              "CatalogNamespace": "ns",
              "CatalogItemId": "id"
            }
            """);

        Assert.True(RepairWriteGuard.TryPreflight(
            manifests, target, RepairWriteKind.EpicItemModify, out RepairPlan plan, out string error), error);

        Assert.True(plan.IsValid);
        Assert.Equal(target, plan.TargetPath);
        Assert.NotNull(plan.BackupPath);
        Assert.Contains(Path.Combine(AppPaths.AppDataRoot, "backups"), plan.BackupPath);
        Assert.False(Directory.Exists(Path.GetDirectoryName(plan.BackupPath)));
    }

    [Fact]
    public void Stage_WritesUnderStagingAndReadsBack()
    {
        string name = "stage-" + Guid.NewGuid().ToString("N") + ".acf";
        string? seen = null;

        Assert.True(RepairWriteGuard.TryStage(
            name, "payload", path => { seen = File.ReadAllText(path); return true; }, out string staged, out string error), error);

        Assert.StartsWith(Path.Combine(AppPaths.AppDataRoot, "staging"), staged);
        Assert.Equal("payload", seen);

        RepairWriteGuard.DeleteStaged(staged);
        Assert.False(File.Exists(staged));
    }

    [Fact]
    public void Stage_ParseFailureRemovesTheStagedFile()
    {
        string name = "stage-fail-" + Guid.NewGuid().ToString("N") + ".acf";

        Assert.False(RepairWriteGuard.TryStage(
            name, "payload", _ => false, out string staged, out string error));

        Assert.Empty(staged);
        Assert.Contains("did not parse", error);
        Assert.False(File.Exists(Path.Combine(AppPaths.AppDataRoot, "staging", name)));
    }

    [Fact]
    public void CopyAndVerify_SteamShortCopyDeletesTheNewTarget()
    {
        string staged = Path.Combine(_dir, "staged.acf");
        string target = Path.Combine(_dir, "appmanifest_1.acf");
        File.WriteAllText(staged, "0123456789");

        Assert.False(RepairWriteGuard.TryCopyAndVerify(
            staged, target, backupPath: null, parses: null, out string error,
            copy: (_, to) => File.WriteAllText(to, "012")));

        Assert.Contains("mismatch", error);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public void CopyAndVerify_EpicShortCopyRestoresTheBackup()
    {
        string staged = Path.Combine(_dir, "staged.item");
        string target = Path.Combine(_dir, "game.item");
        string backup = Path.Combine(_dir, "backup.item");
        File.WriteAllText(staged, "0123456789");
        File.WriteAllText(target, "original");
        File.Copy(target, backup);

        Assert.False(RepairWriteGuard.TryCopyAndVerify(
            staged, target, backup, parses: null, out string error,
            copy: (_, to) => File.WriteAllText(to, "012")));

        Assert.Contains("restored", error);
        Assert.Equal("original", File.ReadAllText(target));
    }

    [Fact]
    public void CopyAndVerify_SuccessCopiesExactBytes()
    {
        string staged = Path.Combine(_dir, "staged.acf");
        string target = Path.Combine(_dir, "appmanifest_1.acf");
        File.WriteAllText(staged, "payload");

        Assert.True(RepairWriteGuard.TryCopyAndVerify(
            staged, target, backupPath: null, parses: path => File.ReadAllText(path) == "payload",
            out string error), error);
        Assert.Equal("payload", File.ReadAllText(target));
    }

    [Fact]
    public void Journal_AppendsOneJsonLine()
    {
        string marker = "journal-" + Guid.NewGuid().ToString("N");

        RepairWriteGuard.Journal(
            RepairWriteGuard.SteamAction, marker, null, 10, 10, "refused", "missing marker");

        string line = File.ReadAllLines(RepairWriteGuard.JournalPath)
            .Last(l => l.Contains(marker, StringComparison.Ordinal));
        using JsonDocument doc = JsonDocument.Parse(line);
        JsonElement root = doc.RootElement;
        Assert.Equal("steam-acf", root.GetProperty("action").GetString());
        Assert.Equal(marker, root.GetProperty("target").GetString());
        Assert.Equal("refused", root.GetProperty("outcome").GetString());
        Assert.Equal("missing marker", root.GetProperty("message").GetString());
        Assert.Equal(10, root.GetProperty("stagedLength").GetInt64());
        Assert.True(root.TryGetProperty("timestamp", out _));
    }
}
