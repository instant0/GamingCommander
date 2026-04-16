using GamingCommander.Core.Models;

namespace GamingCommander.Core.Tests;

public sealed class GameRecordTests
{
    [Fact]
    public void RecordPreservesKeyboardAndPointerCapabilities()
    {
        GameRecord record = new(
            Id: "standalone-1",
            Title: "Test Game",
            Source: GameSourceKind.Standalone,
            InstallPath: @"C:\Games\Test",
            LaunchTarget: @"C:\Games\Test\game.exe",
            ExecutablePath: @"C:\Games\Test\game.exe",
            LastModified: DateTimeOffset.FromUnixTimeSeconds(1700000000),
            SupportsPointerInteraction: true,
            SupportsKeyboardOnlyFlow: true);

        Assert.True(record.SupportsPointerInteraction);
        Assert.True(record.SupportsKeyboardOnlyFlow);
        Assert.Equal("Test Game", record.Title);
    }
}
