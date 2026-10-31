using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class SteamAcfWriterTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "acf_" + Guid.NewGuid().ToString("N")[..8]);

    public SteamAcfWriterTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(_dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                catch (IOException)
                {
                }
            }

            Directory.Delete(_dir, true);
        }
    }

    [Fact]
    public void Build_RoundTripsThroughParser()
    {
        string text = SteamAcfWriter.Build("222750", "Wargame: AirLand Battle", "Wargame Airland Battle");
        string file = Path.Combine(_dir, "appmanifest_222750.acf");
        File.WriteAllText(file, text);

        AcfInfo? parsed = SteamAcfParser.ParseAcfFile(file, _dir);
        Assert.NotNull(parsed);
        Assert.Equal("222750", parsed.AppId);
        Assert.Equal("Wargame: AirLand Battle", parsed.Name);
        Assert.Equal("Wargame Airland Battle", parsed.Installdir);
        Assert.Equal("4", parsed.StateFlags);
    }

    [Fact]
    public void TryWrite_CreatesFileOnce()
    {
        string library = NewLibrary();

        Assert.True(SteamAcfWriter.TryWrite(
            library, "10", "Game", "GameFolder", out string path, out string error));
        Assert.True(File.Exists(path));
        Assert.Empty(error);
        Assert.Equal(Path.Combine(library, "steamapps", "appmanifest_10.acf"), path);

        Assert.False(SteamAcfWriter.TryWrite(library, "10", "Game", "GameFolder", out _, out string again));
        Assert.Equal("ACF already exists; this writer never overwrites.", again);
    }

    [Fact]
    public void TryWrite_LeavesOnlyTheAcfInSteamapps()
    {
        string library = NewLibrary();
        Assert.True(SteamAcfWriter.TryWrite(library, "10", "Game", "GameFolder", out _, out string error), error);

        string steamapps = Path.Combine(library, "steamapps");
        string[] files = Directory.GetFiles(steamapps).Select(Path.GetFileName).OrderBy(x => x).ToArray()!;
        Assert.Equal(["appmanifest_10.acf", "libraryfolders.vdf"], files);
    }

    [Fact]
    public void TryWrite_MissingSteamapps_RefusedAndDirectoryNotCreated()
    {
        string library = Path.Combine(_dir, "NoSteamapps");

        Assert.False(SteamAcfWriter.TryWrite(library, "10", "Game", "GameFolder", out _, out string error));
        Assert.Contains("will not be created", error);
        Assert.False(Directory.Exists(Path.Combine(library, "steamapps")));
    }

    [Fact]
    public void TryWrite_MissingLibraryFolders_Refused()
    {
        string library = Path.Combine(_dir, "NoMarker");
        Directory.CreateDirectory(Path.Combine(library, "steamapps"));

        Assert.False(SteamAcfWriter.TryWrite(library, "10", "Game", "GameFolder", out _, out string error));
        Assert.Contains("libraryfolders.vdf", error);
        Assert.False(File.Exists(Path.Combine(library, "steamapps", "appmanifest_10.acf")));
    }

    [Theory]
    [InlineData("relative/library")]
    [InlineData(@"\\server\share")]
    public void TryWrite_RelativeOrUnc_RefusedBeforeFilesystemWrites(string library)
    {
        Assert.False(SteamAcfWriter.TryWrite(library, "10", "Game", "GameFolder", out string path, out string error));
        Assert.Empty(path);
        Assert.True(error.Contains("absolute", StringComparison.OrdinalIgnoreCase)
            || error.Contains("UNC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TryWrite_BadAppIdOrEmptyInstalldir_Refused()
    {
        string library = NewLibrary();
        Assert.False(SteamAcfWriter.TryWrite(library, "abc", "Game", "GameFolder", out _, out string badId));
        Assert.Contains("digits", badId);
        Assert.False(SteamAcfWriter.TryWrite(library, "10", "Game", " ", out _, out string noDir));
        Assert.Contains("required", noDir);
    }

    [Fact]
    public void Preview_ReportsTargetAndLengthWithoutWriting()
    {
        string library = NewLibrary();
        string target = Path.Combine(library, "steamapps", "appmanifest_10.acf");

        RepairPlan plan = SteamAcfWriter.Preview(library, "10", "Game", "GameFolder");

        Assert.True(plan.IsValid, plan.Message);
        Assert.Equal(target, plan.TargetPath);
        Assert.Null(plan.BackupPath);
        Assert.True(plan.StagedLength > 0);
        Assert.Equal("libraryfolders.vdf", plan.Marker);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public void Preview_MissingMarker_IsInvalidAndCreatesNothing()
    {
        string library = Path.Combine(_dir, "PreviewNoMarker");
        Directory.CreateDirectory(Path.Combine(library, "steamapps"));

        RepairPlan plan = SteamAcfWriter.Preview(library, "10", "Game", "GameFolder");

        Assert.False(plan.IsValid);
        Assert.Contains("libraryfolders.vdf", plan.Message);
        Assert.False(File.Exists(Path.Combine(library, "steamapps", "appmanifest_10.acf")));
    }

    [Fact]
    public void TryWrite_ReadOnlySteamapps_FailsAndLeavesNoAcf()
    {
        string library = NewLibrary();
        string steamapps = Path.Combine(library, "steamapps");
        MakeReadOnlyDirectory(steamapps);
        if (!OperatingSystem.IsWindows())
            Assert.Equal(
                UnixFileMode.UserRead | UnixFileMode.UserExecute,
                File.GetUnixFileMode(steamapps));

        try
        {
            Assert.False(SteamAcfWriter.TryWrite(
                library, "10", "Game", "GameFolder", out _, out string error));

            Assert.NotEmpty(error);
            Assert.False(File.Exists(Path.Combine(steamapps, "appmanifest_10.acf")));
        }
        finally
        {
            MakeWritableDirectory(steamapps);
        }
    }

    [Fact]
    public void IsAppId_RejectsJunk()
    {
        Assert.False(SteamAcfWriter.IsAppId(""));
        Assert.False(SteamAcfWriter.IsAppId("abc"));
        Assert.True(SteamAcfWriter.IsAppId("222750"));
    }

    private string NewLibrary()
    {
        string library = Path.Combine(_dir, "library");
        string steamapps = Path.Combine(library, "steamapps");
        Directory.CreateDirectory(steamapps);
        File.WriteAllText(Path.Combine(steamapps, "libraryfolders.vdf"), "\"libraryfolders\"\n{\n}\n");
        return library;
    }

    private static void MakeReadOnlyDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            File.SetAttributes(path, FileAttributes.ReadOnly);
        else
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
    }

    private static void MakeWritableDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            File.SetAttributes(path, FileAttributes.Normal);
        else
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }
}
