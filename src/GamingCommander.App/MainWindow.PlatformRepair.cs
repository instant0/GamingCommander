using System.Diagnostics;
using Avalonia.Input;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App;

/// <summary>
/// Platform repair (Epic .item / Steam ACF writes) + folder-open handlers
/// member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// </summary>
public partial class MainWindow
{
    private void WriteEpicItem_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        _ = WriteEpicItemAsync();

    private async Task WriteEpicItemAsync()
    {
        GameEntry? game = GetSelectedGame();
        if (game is null)
        {
            SetStatusWithAutoClear("Select a game first.");
            return;
        }

        if (!await EpicRepairDialog.ConfirmAsync(this, game.DisplayName).ConfigureAwait(true))
        {
            SetStatusWithAutoClear("Epic .item write cancelled.");
            return;
        }

        string gameFolder = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.GameFolder, "");
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            // GameFolder metadata may be absent; the entry's FolderPath IS the physical
            // game folder (never the anchor name — anchors can be display-only catalogs).
            gameFolder = game.FolderPath;
        }
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            SetStatusWithAutoClear("No game folder known for this entry. Rescan the library first.");
            return;
        }

        string manifests = EpicManifestPaths.DefaultManifestsDir;
        if (!EpicItemWriter.TryWrite(
                gameFolder, manifests, out string path, out string error, game.DisplayName))
        {
            SetStatusWithAutoClear(error);
            return;
        }

        foreach (GameEntry row in _dbService.Load().Games)
        {
            if (row.GameSource != GameSourceKind.Epic)
                continue;
            string folder = row.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.GameFolder,
                Path.Combine(Path.GetDirectoryName(row.FolderPath) ?? "", row.FolderName));
            if (!EpicInstallPath.Same(folder, gameFolder))
                continue;
            var extra = new Dictionary<string, string>(row.PlatformMetadata)
            {
                [PlatformMetadataKeys.EpicStatus] = "Installed",
                [PlatformMetadataKeys.EpicItemPath] = path,
                [PlatformMetadataKeys.GameFolder] = gameFolder,
            };
            _dbService.UpdateGameEntry(row with { PlatformMetadata = extra, ManifestPath = path });
        }

        _viewModel?.Reload();
        SetStatusWithAutoClear(
            $"Wrote {Path.GetFileName(path)}. Epic Update can officialize ONE of these per launcher run. Quit Epic before writing another.");
    }

    private void WriteSteamAcf_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null)
            return;

        GameEntry? game = GetSelectedGame();
        if (game is null)
        {
            SetStatusWithAutoClear("Select a game first.");
            return;
        }

        string appId = _viewModel.SteamAppIdForAcf;
        if (!SteamAcfWriter.IsAppId(appId))
        {
            SetStatusWithAutoClear("No Steam AppID yet. F3 lookup first, then write the ACF.");
            return;
        }

        string lib = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.LibraryRoot, "")
            is { Length: > 0 } stored
            ? stored
            : LibraryManager.NormalizeLibraryRoot(game.FolderPath);
        if (!SteamAcfWriter.TryWrite(
                lib, appId, game.DisplayName, game.FolderName, out string path, out string error))
        {
            SetStatusWithAutoClear(error);
            return;
        }

        var extra = new Dictionary<string, string>(game.PlatformMetadata)
        {
            [PlatformMetadataKeys.SteamStatus] = "Installed",
            [PlatformMetadataKeys.SteamAppId] = appId,
            [PlatformMetadataKeys.AcfFilePath] = path,
            [PlatformMetadataKeys.AcfLibraryPath] = lib,
        };
        var updated = game with
        {
            CommandLineArguments = $"steam://rungameid/{appId}",
            ManifestPath = path,
            PlatformMetadata = extra,
        };
        _dbService.UpdateGameEntry(updated);
        _viewModel.Reload();
        SetStatusWithAutoClear($"Wrote {Path.GetFileName(path)}");
    }

    private void OpenConfigPath_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        OpenFolderFromDisplay(_viewModel?.DetailsConfigPath);

    private void OpenSavePath_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        OpenFolderFromDisplay(_viewModel?.DetailsSavePath);

    private void OpenGameFolder_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        OpenFolderFromDisplay(_viewModel?.DetailsGameFolder);

    private void OpenFolderFromDisplay(string? displayPath)
    {
        string? gameDir = _viewModel?.SelectedItem?.InstallDirectory;
        if (!WindowsExplorer.TryBuildOpenFolder(displayPath, gameDir, out string fileName, out string arguments))
        {
            SetStatusWithAutoClear("Path is not a local Windows folder.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            SetStatusWithAutoClear($"Could not open folder: {ex.Message}");
        }
    }
}