namespace GamingCommander.Core.Models;

/// <summary>Marker for store-specific inventory metadata.</summary>
public interface IStoreInventoryMetadata
{
}

/// <summary>Steam-specific inventory facts read from an ACF.</summary>
public sealed record SteamInventoryMetadata(
    string AppId,
    string? AcfPath,
    string LibraryRoot,
    string InstallDir,
    string? LastUpdated,
    string? BuildId,
    int StateFlags) : IStoreInventoryMetadata;

/// <summary>GOG-specific inventory facts from the registry and optional <c>.info</c>.</summary>
public sealed record GogInventoryMetadata(
    string GameId,
    string? BuildId,
    string? InstallDate,
    string? LaunchCommand,
    string? LaunchArguments,
    bool FromRegistryOnly) : IStoreInventoryMetadata;

/// <summary>Epic-specific inventory facts from a ProgramData <c>.item</c> or a local store folder.</summary>
public sealed record EpicInventoryMetadata(
    string? CatalogNamespace,
    string? CatalogItemId,
    string? AppName,
    string? InstallationGuid,
    string? AppVersionString,
    long? InstallSize,
    string? CompleteManifestPath,
    string? ItemPath,
    string? MancpnPath,
    bool IsCatalogItem) : IStoreInventoryMetadata;

/// <summary>Rockstar-specific inventory facts from the per-game registry key and <c>title.rgl</c>.</summary>
public sealed record RockstarInventoryMetadata(
    string RegistryGameName,
    string? TitleId,
    string? RglPath,
    string RegistryKey) : IStoreInventoryMetadata;

/// <summary>
/// One game as reported by a store's own inventory. <see cref="FolderExists"/> and
/// <see cref="ExeExists"/> are disk checks, not store claims. <see cref="ExeExists"/> is only
/// meaningful when <see cref="LaunchExe"/> is non-null.
/// </summary>
public sealed record StoreInventoryEntry(
    GameSourceKind Kind,
    string StoreId,
    string Name,
    string InstallPath,
    string? LaunchExe,
    StoreReportedState StoreState,
    bool FolderExists,
    bool ExeExists,
    IStoreInventoryMetadata? Metadata)
{
    /// <summary>True when the store records an install folder that exists on disk.</summary>
    public bool IsInstalled => FolderExists;
}
