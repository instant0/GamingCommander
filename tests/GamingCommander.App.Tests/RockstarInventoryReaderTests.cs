using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class RockstarInventoryReaderTests
{
    private const string BaseKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games";

    [Fact]
    public void RegistryPathAndTitle_ReturnsEntry()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("Grand Theft Auto V Enhanced");
        string title = temp.CreateFile(
            Path.Combine("Grand Theft Auto V Enhanced", "title.rgl"), "GTAV");
        var registry = new MockRegistryReader(
            Entry("Grand Theft Auto V Enhanced", install)
            + $"[{BaseKey}\\Grand Theft Auto V Enhanced]\r\n"
            + $"\"GTAV\"=str(2):\"{install}/GTAV.exe\"\r\n");

        StoreInventoryEntry entry = new RockstarInventoryReader(registry).Read().Single();

        Assert.Equal(GameSourceKind.Rockstar, entry.Kind);
        Assert.Equal("GTAV", entry.StoreId);
        Assert.Equal("Grand Theft Auto V Enhanced", entry.Name);
        Assert.Equal(install, entry.InstallPath);
        Assert.True(entry.FolderExists);
        Assert.Equal(StoreReportedState.Installed, entry.StoreState);
        Assert.Null(entry.LaunchExe);
        Assert.False(entry.ExeExists);

        var metadata = Assert.IsType<RockstarInventoryMetadata>(entry.Metadata);
        Assert.Equal("GTAV", metadata.TitleId);
        Assert.Equal(title, metadata.RglPath);
        Assert.Equal("Grand Theft Auto V Enhanced", metadata.RegistryGameName);
    }

    [Fact]
    public void MissingFolder_StillReturnsEntry()
    {
        using var temp = new TempDirectory();
        string missing = temp.Combine("missing");
        var registry = new MockRegistryReader(Entry("Gone Game", missing));

        StoreInventoryEntry entry = new RockstarInventoryReader(registry).Read().Single();

        Assert.Equal("Gone Game", entry.Name);
        Assert.False(entry.FolderExists);
        Assert.Equal(string.Empty, entry.StoreId);
        Assert.Equal(StoreReportedState.Unknown, entry.StoreState);
        Assert.Null(Assert.IsType<RockstarInventoryMetadata>(entry.Metadata).TitleId);
    }

    [Fact]
    public void MissingTitleFile_OmitsTitleId()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("Game");
        var registry = new MockRegistryReader(Entry("Game", install));

        StoreInventoryEntry entry = new RockstarInventoryReader(registry).Read().Single();

        Assert.Equal(string.Empty, entry.StoreId);
        var metadata = Assert.IsType<RockstarInventoryMetadata>(entry.Metadata);
        Assert.Null(metadata.TitleId);
        Assert.Null(metadata.RglPath);
    }

    [Fact]
    public void EmptyTitleFile_OmitsTitleIdButKeepsPath()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("Game");
        string title = temp.CreateFile(Path.Combine("Game", "title.rgl"), "   ");
        var registry = new MockRegistryReader(Entry("Game", install));

        StoreInventoryEntry entry = new RockstarInventoryReader(registry).Read().Single();

        Assert.Equal(string.Empty, entry.StoreId);
        var metadata = Assert.IsType<RockstarInventoryMetadata>(entry.Metadata);
        Assert.Null(metadata.TitleId);
        Assert.Equal(title, metadata.RglPath);
    }

    [Fact]
    public void ControlCharactersInTitle_AreOmitted()
    {
        using var temp = new TempDirectory();
        string install = temp.CreateDirectory("Game");
        temp.CreateFile(Path.Combine("Game", "title.rgl"), "GTA\u0000V");
        var registry = new MockRegistryReader(Entry("Game", install));

        StoreInventoryEntry entry = new RockstarInventoryReader(registry).Read().Single();

        Assert.Equal(string.Empty, entry.StoreId);
        Assert.Null(Assert.IsType<RockstarInventoryMetadata>(entry.Metadata).TitleId);
    }

    [Fact]
    public void LauncherKey_IsSkipped()
    {
        var registry = new MockRegistryReader(
            $"[{BaseKey}\\Launcher]\r\n\"InstallFolder\"=str(2):\"C:\\\\Launcher\"\r\n");

        Assert.Empty(new RockstarInventoryReader(registry).Read());
    }

    [Fact]
    public void NoRegistry_ReturnsEmpty()
    {
        Assert.Empty(new RockstarInventoryReader(new MockRegistryReader("")).Read());
    }

    private static string Entry(string gameName, string installPath) =>
        $"[{BaseKey}\\{gameName}]\r\n\"InstallFolder\"=str(2):\"{Esc(installPath)}\"\r\n";

    private static string Esc(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
