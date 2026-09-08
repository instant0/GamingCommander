using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GamingCommander.App.Services;

namespace GamingCommander.App;

/// <summary>
/// Command-bar / detail-panel click handlers member-part of <see cref="MainWindow"/>
/// (Plan 125 Phase 1c split).
/// </summary>
public partial class MainWindow
{
    private void ChangeType_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        _ = OpenGameSetupAsync();

    private void MultipleExeWarning_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        _ = OpenGameSetupAsync();

    private void CommandButtonPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string hotkey || _viewModel is null)
            return;

        switch (hotkey)
        {
            case "F1":
                _ = HelpDialogBuilder.ShowHelpAsync(this);
                break;
            case "F2":
                _ = OpenLibrarySetupAsync();
                break;
            case "F3":
                _ = LookupSelectedGameMetadataAsync();
                break;
            case "F4":
                _ = OpenGameSetupAsync();
                break;
            case "F5":
                _ = RefreshCurrentRootAsync();
                break;
            case "F8":
                _ = OpenFilterAsync();
                break;
            case "F10":
                Close();
                break;
        }
    }
}