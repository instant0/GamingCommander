using Avalonia.Controls;
using Avalonia.Input;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

/// <summary>
/// Library setup / filter dialog member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// </summary>
public partial class MainWindow
{
    private async Task OpenFilterAsync()
    {
        if (_viewModel is null)
            return;

        // Opening the F8 dialog ends any typed search session (Plan 122).
        if (_viewModel.IsSearching)
            _viewModel.CancelSearch();

        GameFilter? chosen = await FilterWindow.ShowAsync(
            this, _viewModel.CollectFilterOptions(), _viewModel.ActiveFilter).ConfigureAwait(true);
        if (chosen is null)
        {
            if (_viewModel.IsFilterActive)
                _viewModel.ClearFilter();
            SetStatusWithAutoClear("Filter cleared.");
            return;
        }

        _viewModel.ApplyFilter(chosen);
    }

    private void TagBadgePressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_viewModel is null || sender is not Control control)
            return;
        string? name = control.Tag as string ?? (control.DataContext as TagBadgeViewModel)?.Name;
        if (string.IsNullOrWhiteSpace(name))
            return;
        // A tag click replaces any typed search session (Plan 122).
        if (_viewModel.IsSearching)
            _viewModel.CancelSearch();
        _viewModel.ApplyFilter(new GameFilter(GameFilterKind.Tag, name));
        e.Handled = true;
    }

    private async Task OpenLibrarySetupAsync()
    {
        // Steam locator uses the same Windows-only registry reader as the scanner.
        IRegistryReader registryReader = OperatingSystem.IsWindows()
            ? new WindowsRegistryReader()
            : new NullRegistryReader();

        var window = new LibrarySetupWindow(
            _configService, _dbService, _libraryManager, new SteamInstallPathLocator(registryReader));
        await window.ShowDialog(this);

        _viewModel?.Reload();
        await SyncOnlineGateFromConfigAsync().ConfigureAwait(true);
    }

    private async Task OpenGameSetupAsync()
    {
        if (_viewModel is null) return;
        if (_viewModel.IsAtRootLevel) return;

        var item = _viewModel.SelectedItem;
        if (item?.GameId is null) return;

        var game = item.GameId is null ? null : FindGameById(item.GameId);
        if (game is null) return;

        IReadOnlyList<GameMetadataCommandLine> catalog =
            _viewModel.GetSidecar(game.Id)?.Details?.CommandLine ?? [];
        if (_onlineGate is { AllowsHttp: true })
            _ = RefreshMetadataForGameAsync(game);

        // Picker start folder = the game's physical folder. (Historically the
        // library *anchor name* was passed as rootPath — wrong when the anchor
        // label is not a filesystem path, e.g. a Steam anchor.)
        var window = new GameSetupWindow(game, game.FolderPath, _configService, _dbService, catalog);
        await window.ShowDialog(this);

        _viewModel.Reload();
    }
}