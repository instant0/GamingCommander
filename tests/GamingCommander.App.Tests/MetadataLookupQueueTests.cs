using GamingCommander.App.Services.Metadata;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class MetadataLookupQueueTests
{
    [Fact]
    public async Task Enqueue_ProcessesInOrder_WhenFlagOn()
    {
        var fake = new FakeMetadataService();
        using var queue = new MetadataLookupQueue(fake, new StubConfig(true));
        var done = new List<string>();
        queue.ItemCompleted += (id, _) => done.Add(id);

        queue.Enqueue([Make("a"), Make("b"), Make("a")]);
        await queue.WaitUntilIdleAsync();

        Assert.Equal(["a", "b"], fake.Seen);
        Assert.Equal(["a", "b"], done);
    }

    [Fact]
    public async Task Enqueue_OfflineGate_DoesNothing()
    {
        var fake = new FakeMetadataService();
        var gate = new MetadataOnlineGate();
        gate.ReportFailure();
        using var queue = new MetadataLookupQueue(fake, new StubConfig(true), gate);
        queue.Enqueue([Make("a")]);
        await queue.WaitUntilIdleAsync();
        Assert.Empty(fake.Seen);
    }

    [Fact]
    public async Task Enqueue_FlagOff_DoesNothing()
    {
        var fake = new FakeMetadataService();
        using var queue = new MetadataLookupQueue(fake, new StubConfig(false));
        queue.Enqueue([Make("a")]);
        await queue.WaitUntilIdleAsync();
        Assert.Empty(fake.Seen);
    }

    private static GameEntry Make(string id) =>
        new(
            Id: id,
            FolderName: id,
            DisplayName: id,
            GameSource: GameSourceKind.Standalone,
            IsSourceOverridden: false,
            ExecutablePath: @"D:\g.exe",
            LauncherPath: "",
            CommandLineArguments: "",
            ManifestPath: "",
            LastScanned: DateTimeOffset.UnixEpoch,
            LastModified: DateTimeOffset.UnixEpoch,
            PlatformMetadata: [],
            Tags: [],
            UserOverrides: []);

    private sealed class FakeMetadataService : IMetadataService
    {
        public List<string> Seen { get; } = [];

        public Task<GameMetadataRecord?> RefreshAsync(
            string gameEntryId,
            string? steamAppId,
            string? displayName,
            CancellationToken cancellationToken = default)
        {
            Seen.Add(gameEntryId);
            return Task.FromResult<GameMetadataRecord?>(new GameMetadataRecord { GameEntryId = gameEntryId });
        }
    }

    private sealed class StubConfig : IConfigService
    {
        private readonly AppConfig _config;

        public StubConfig(bool enable) =>
            _config = new AppConfig([], [], [], IsFirstRun: false, EnableOnlineMetadata: enable);

        public AppConfig Load() => _config;
        public void Save(AppConfig config) { }
    }
}
