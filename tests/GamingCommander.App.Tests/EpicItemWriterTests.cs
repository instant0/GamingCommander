using System.Text.Json.Nodes;
using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class EpicItemWriterTests : IDisposable
{
    private const string GuidText = "AABBCCDDEEFF00112233445566778899";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ew_" + Guid.NewGuid().ToString("N")[..8]);

    public EpicItemWriterTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (!Directory.Exists(_dir))
            return;

        foreach (string path in Directory.EnumerateFileSystemEntries(_dir, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(
                        path,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        Directory.Delete(_dir, true);
    }

    [Fact]
    public void TryWrite_MovedGame_UpdatesOwnedMembersAndPreservesTheRest()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        string original = ValidItem(
            install: @"e:\OldLibrary\Orphan",
            launch: "",
            extra: "\"EpicUnknownMember\": { \"nested\": [1, 2, 3] },\n  \"SecondUnknown\": \"keep\"");
        File.WriteAllText(target, original);
        JsonObject before = JsonNode.Parse(original)!.AsObject();

        RepairPlan preview = EpicItemWriter.Preview(game, manifests);
        Assert.True(preview.IsValid, preview.Message);
        Assert.Equal(target, preview.TargetPath);
        Assert.NotNull(preview.BackupPath);
        Assert.True(preview.StagedLength > 0);
        Assert.False(File.Exists(preview.BackupPath));

        Assert.True(
            EpicItemWriter.TryWrite(game, manifests, out string path, out string backup, out string error),
            error);

        Assert.Equal(target, path);
        Assert.True(File.Exists(backup));
        Assert.Equal(original, File.ReadAllText(backup));

        JsonObject after = JsonNode.Parse(File.ReadAllText(target))!.AsObject();
        Assert.Equal(before.Count, after.Count);
        Assert.Equal(
            before.Select(pair => pair.Key),
            after.Select(pair => pair.Key));

        string install = game.Replace('/', '\\').TrimEnd('\\');
        Assert.Equal(install, after["InstallLocation"]!.GetValue<string>());
        Assert.Equal(install + "/.egstore", after["ManifestLocation"]!.GetValue<string>());
        Assert.Equal("Binaries\\Win64\\Orphan.exe", after["LaunchExecutable"]!.GetValue<string>());

        foreach (string member in new[]
                 {
                     "FormatVersion", "InstallationGuid", "CatalogNamespace", "CatalogItemId",
                     "AppName", "DisplayName", "EpicUnknownMember", "SecondUnknown",
                 })
        {
            Assert.True(JsonNode.DeepEquals(before[member], after[member]), $"member {member} changed");
        }
    }

    [Fact]
    public void TryWrite_NonEmptyLaunchExecutable_IsPreserved()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        File.WriteAllText(target, ValidItem(@"e:\Old\Orphan", "Old.exe"));

        Assert.True(
            EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error),
            error);

        JsonObject after = JsonNode.Parse(File.ReadAllText(target))!.AsObject();
        Assert.Equal("Old.exe", after["LaunchExecutable"]!.GetValue<string>());
    }

    [Fact]
    public void TryWrite_AbsentTarget_RefusedAndNoItemCreated()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");

        Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error));
        Assert.Contains("cannot create", error);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public void TryWrite_MissingManifestsDir_RefusedAndDirectoryNotCreated()
    {
        string game = CreateGameFolder();
        string manifests = Path.Combine(_dir, "NoManifests");

        Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error));
        Assert.Contains("will not be created", error);
        Assert.False(Directory.Exists(manifests));
    }

    [Fact]
    public void TryWrite_CorruptTarget_RefusedWithoutBackupOrChange()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        File.WriteAllText(target, "{ not json");
        string[] backupsBefore = BackupFilesFor(GuidText + ".item").ToArray();

        Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error));
        Assert.Contains("could not be read", error);
        Assert.Equal("{ not json", File.ReadAllText(target));
        Assert.Equal(backupsBefore, BackupFilesFor(GuidText + ".item"));
    }

    [Fact]
    public void TryWrite_NotTheScanManifestsDir_Refused()
    {
        string manifests = Path.Combine(_dir, "NotTheManifestsDir");
        Directory.CreateDirectory(manifests);
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        File.WriteAllText(target, ValidItem(@"e:\Old\Orphan", ""));

        Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error));
        Assert.Contains("no other .item", error);
    }

    [Fact]
    public void TryWrite_DifferentInstallationGuid_Refused()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        string original = ValidItem(@"e:\Old\Orphan", "").Replace(
            $"\"InstallationGuid\": \"{GuidText}\"",
            "\"InstallationGuid\": \"00000000000000000000000000000000\"",
            StringComparison.Ordinal);
        File.WriteAllText(target, original);

        Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error));
        Assert.Contains("different installation", error);
        Assert.Equal(original, File.ReadAllText(target));
    }

    [Fact]
    public void TryWrite_MissingRequiredMember_Refused()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        string noCatalogId = """
            {
              "InstallationGuid": "AABBCCDDEEFF00112233445566778899",
              "InstallLocation": "e:\\Old\\Orphan",
              "ManifestLocation": "e:\\Old\\Orphan/.egstore",
              "CatalogNamespace": "ns"
            }
            """;
        File.WriteAllText(target, noCatalogId);

        Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out _, out string error));
        Assert.Contains("CatalogItemId", error);
        Assert.Equal(noCatalogId, File.ReadAllText(target));
    }

    [Fact]
    public void Preview_WritesAndCreatesNothing()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        string original = ValidItem(@"e:\Old\Orphan", "");
        File.WriteAllText(target, original);
        string[] backupsBefore = BackupFilesFor(GuidText + ".item").ToArray();

        RepairPlan plan = EpicItemWriter.Preview(game, manifests);

        Assert.True(plan.IsValid, plan.Message);
        Assert.Equal(target, plan.TargetPath);
        Assert.NotNull(plan.BackupPath);
        Assert.True(plan.StagedLength > 0);
        Assert.Equal(original, File.ReadAllText(target));
        Assert.Equal(backupsBefore, BackupFilesFor(GuidText + ".item"));
    }

    [Fact]
    public void TryWrite_ReadOnlyTarget_FailsAndKeepsOriginal()
    {
        string manifests = NewManifestsDir();
        string game = CreateGameFolder();
        string target = Path.Combine(manifests, GuidText + ".item");
        string original = ValidItem(@"e:\Old\Orphan", "");
        File.WriteAllText(target, original);
        MakeReadOnly(target);

        try
        {
            Assert.False(EpicItemWriter.TryWrite(game, manifests, out _, out string backup, out string error));
            Assert.NotEmpty(error);
            Assert.Equal(original, File.ReadAllText(target));
            Assert.True(File.Exists(backup));
        }
        finally
        {
            MakeWritable(target);
        }
    }

    private string NewManifestsDir()
    {
        string manifests = Path.Combine(_dir, "Manifests");
        Directory.CreateDirectory(manifests);
        // The scan's manifests folder always holds other items.
        File.WriteAllText(Path.Combine(manifests, "00000000000000000000000000000000.item"), "{}");
        return manifests;
    }

    private string CreateGameFolder()
    {
        string game = Path.Combine(_dir, "Games", "Orphan");
        string egstore = Path.Combine(game, ".egstore");
        Directory.CreateDirectory(egstore);
        File.WriteAllBytes(Path.Combine(egstore, GuidText + ".manifest"), [1]);
        Directory.CreateDirectory(Path.Combine(game, "Binaries", "Win64"));
        File.WriteAllBytes(Path.Combine(game, "Binaries", "Win64", "Orphan.exe"), new byte[8]);
        return game;
    }

    private static string ValidItem(string install, string launch, string extra = "\"Unknown\": 1")
    {
        string escaped = install.Replace(@"\", @"\\");
        return $$"""
            {
              "FormatVersion": 0,
              "LaunchExecutable": "{{launch}}",
              "ManifestLocation": "{{escaped}}/.egstore",
              "DisplayName": "Orphan",
              {{extra}},
              "InstallationGuid": "{{GuidText}}",
              "InstallLocation": "{{escaped}}",
              "CatalogNamespace": "ns",
              "CatalogItemId": "id",
              "AppName": "Orphan"
            }
            """;
    }

    private static IEnumerable<string> BackupFilesFor(string fileName)
    {
        string root = Path.Combine(AppPaths.AppDataRoot, "backups");
        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories)
            : [];
    }

    private static void MakeReadOnly(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(path, FileAttributes.ReadOnly);
        }
        else
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
    }

    private static void MakeWritable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
        else
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
    }
}
