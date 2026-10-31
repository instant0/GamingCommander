using System.Diagnostics;
using Avalonia.Input;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App;

/// <summary>
/// Platform repair (Epic .item update / Steam ACF creation) + folder-open handlers
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

        string gameFolder = game.Location.GameFolder;
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            // Location.GameFolder may be absent; the entry's FolderPath IS the physical
            // game folder (never the anchor name — anchors can be display-only catalogs).
            gameFolder = game.FolderPath;
        }
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            SetStatusWithAutoClear("No game folder known for this entry. Rescan the library first.");
            return;
        }

        string manifests = EpicManifestPaths.DefaultManifestsDir;
        RepairPlan preview = EpicItemWriter.Preview(gameFolder, manifests);
        if (!preview.IsValid)
        {
            // Preflight failed: do not offer a repair that cannot run.
            SetStatusWithAutoClear(preview.Message);
            return;
        }

        if (!await EpicRepairDialog.ConfirmAsync(this, game.DisplayName, preview).ConfigureAwait(true))
        {
            SetStatusWithAutoClear("Epic registration repair cancelled.");
            return;
        }

        if (!EpicItemWriter.TryWrite(
                gameFolder, manifests, out string path, out string backup, out string error, game.DisplayName))
        {
            SetStatusWithAutoClear(
                $"{error} Journal: {RepairWriteGuard.JournalPath}");
            return;
        }

        foreach (GameEntry row in _dbService.Load().Games)
        {
            if (row.GameSource != GameSourceKind.Epic)
                continue;
            string folder = row.Location.GameFolder is { Length: > 0 } gf
                ? gf
                : Path.Combine(Path.GetDirectoryName(row.FolderPath) ?? "", row.FolderName);
            if (!EpicInstallPath.Same(folder, gameFolder))
                continue;
            _dbService.UpdateGameEntry(row with
            {
                Epic = row.Epic with { Status = "Installed", ItemPath = path },
                Location = row.Location with { GameFolder = gameFolder },
                Launch = row.Launch with { ManifestPath = path },
            });
        }

        _viewModel?.Reload();
        SetStatusWithAutoClear(
            $"Repaired {Path.GetFileName(path)}; backup {backup}. Journal: {RepairWriteGuard.JournalPath}. "
            + "Epic Update can officialize ONE of these per launcher run; quit Epic before repairing another.");
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

        string lib = game.Location.LibraryRoot is { Length: > 0 } stored
            ? stored
            : LibraryManager.NormalizeLibraryRoot(game.FolderPath);

        RepairPlan preview = SteamAcfWriter.Preview(lib, appId, game.DisplayName, game.FolderName);
        if (!preview.IsValid)
        {
            SetStatusWithAutoClear(preview.Message);
            return;
        }

        SetStatusWithAutoClear(
            $"ACF target {preview.TargetPath} ({preview.StagedLength} bytes; marker {preview.Marker}).");
        if (!SteamAcfWriter.TryWrite(
                lib, appId, game.DisplayName, game.FolderName, out string path, out string error))
        {
            SetStatusWithAutoClear($"{error} Journal: {RepairWriteGuard.JournalPath}");
            return;
        }

        var updated = game with
        {
            Launch = game.Launch with
            {
                CommandLineArguments = $"steam://rungameid/{appId}",
                ManifestPath = path,
            },
            Acf = game.Acf with { Status = "Installed", FilePath = path, LibraryPath = lib },
            Store = game.Store with { SteamAppId = appId },
        };
        _dbService.UpdateGameEntry(updated);
        _viewModel.Reload();
        SetStatusWithAutoClear(
            $"Created {path} ({preview.StagedLength} bytes). Journal: {RepairWriteGuard.JournalPath}");
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