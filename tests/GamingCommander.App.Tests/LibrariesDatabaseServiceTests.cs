using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for the libraries.json service: named anchors round-trip through disk and a
/// missing or corrupt file falls back to an empty library list without throwing.
/// </summary>
public sealed class LibrariesDatabaseServiceTests
{
    [Fact]
    public void Upsert_Get_Remove_RoundTripThroughDisk()
    {
        using var temp = new TempDirectory("gc_libs_");
        string path = temp.Combine("libraries.json");

        var service = new LibrariesDatabaseService(path);
        service.Upsert(new Library("Steam", GameSourceKind.Steam, [@"D:\SteamLibrary"]));
        service.Upsert(new Library("GOG", GameSourceKind.Gog, [@"E:\GOG", @"F:\GOG2"]));

        // Fresh instance proves the values came from disk, not memory.
        var reloaded = new LibrariesDatabaseService(path);
        Assert.Equal(2, reloaded.Libraries.Count);

        Library? steam = reloaded.Get("Steam");
        Assert.NotNull(steam);
        Assert.Equal(GameSourceKind.Steam, steam!.Type);
        Assert.Equal([@"D:\SteamLibrary"], steam.Folders);

        Library? gog = reloaded.Get("GOG");
        Assert.NotNull(gog);
        Assert.Equal(GameSourceKind.Gog, gog!.Type);
        Assert.Equal([@"E:\GOG", @"F:\GOG2"], gog.Folders);

        Assert.True(reloaded.Remove("GOG"));
        Assert.Null(new LibrariesDatabaseService(path).Get("GOG"));
    }

    [Fact]
    public void MissingOrCorruptFile_ReturnsEmptyDefaults()
    {
        using var temp = new TempDirectory("gc_libs_");
        string path = temp.Combine("libraries.json");

        // Missing file: empty libraries, and a read must not create the file.
        Assert.Empty(new LibrariesDatabaseService(path).Libraries);
        Assert.False(File.Exists(path));

        // Corrupt file: empty libraries, damaged bytes preserved for inspection.
        File.WriteAllText(path, "not valid json {{{");
        Assert.Empty(new LibrariesDatabaseService(path).Libraries);
        Assert.True(File.Exists(path + ".corrupt"));
    }
}
