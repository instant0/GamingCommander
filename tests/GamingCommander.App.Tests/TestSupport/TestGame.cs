using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Builder for <see cref="GameEntry"/> values used by tests. Only the fields a test
/// cares about need to be set; the rest are deterministic defaults.
/// </summary>
internal sealed class TestGame
{
    public GameSourceKind Source { get; init; } = GameSourceKind.Standalone;

    /// <summary>Absolute game folder; its leaf is the default folder name and title.</summary>
    public string Folder { get; init; } = @"C:\Games\TestGame";

    /// <summary>Display title. Defaults to the folder leaf.</summary>
    public string? Title { get; init; }

    /// <summary>Store identifier, mapped to the store-specific field for <see cref="Source"/>.</summary>
    public string? StoreId { get; init; }

    public string Library { get; init; } = string.Empty;

    /// <summary>Explicit id. Defaults to the deterministic id for <see cref="Folder"/>.</summary>
    public string? Id { get; init; }

    /// <summary>Primary executable path. Defaults to <c>&lt;Folder&gt;/game.exe</c>.</summary>
    public string? ExecutablePath { get; init; }

    public GameEntry Build()
    {
        string folder = Folder.TrimEnd('\\', '/');
        string folderName = folder[(Math.Max(folder.LastIndexOf('\\'), folder.LastIndexOf('/')) + 1)..];
        string displayName = string.IsNullOrWhiteSpace(Title) ? folderName : Title;

        return new GameEntry(
            Identity: new IdentityInfo
            {
                Id = Id ?? GameEntryId.ComputeId(@"C:\TestGames", folder),
                Library = Library,
                DisplayName = displayName,
                GameSource = Source,
                IsSourceOverridden = false,
            },
            Location: new LocationInfo { FolderPath = Folder, FolderName = folderName },
            Launch: new LaunchInfo
            {
                ExecutablePath = ExecutablePath ?? System.IO.Path.Combine(Folder, "game.exe"),
            },
            Tags: [],
            UserOverrides: [],
            LastScanned: DateTimeOffset.UnixEpoch,
            LastModified: DateTimeOffset.UnixEpoch)
        {
            Store = BuildStore(),
        };
    }

    private StoreInfo BuildStore()
    {
        if (string.IsNullOrWhiteSpace(StoreId))
            return new StoreInfo();

        string key = $"{Source}:{StoreId}";
        return Source switch
        {
            GameSourceKind.Steam => new StoreInfo { StoreKey = key, SteamAppId = StoreId },
            GameSourceKind.Epic => new StoreInfo { StoreKey = key, EpicAppName = StoreId },
            GameSourceKind.Gog => new StoreInfo { StoreKey = key, GogGameId = StoreId },
            GameSourceKind.EaApp => new StoreInfo { StoreKey = key, EaContentId = StoreId },
            GameSourceKind.UbisoftConnect => new StoreInfo { StoreKey = key, UbisoftGameId = StoreId },
            GameSourceKind.Rockstar => new StoreInfo { StoreKey = key, RockstarTitleId = StoreId },
            GameSourceKind.Xbox => new StoreInfo { StoreKey = key, XboxPackageFamily = StoreId },
            GameSourceKind.BattleNet => new StoreInfo { StoreKey = key, BlizzardProduct = StoreId },
            _ => new StoreInfo { StoreKey = key },
        };
    }
}
