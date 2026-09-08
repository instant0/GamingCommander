using Avalonia.Input;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

/// <summary>
/// Keyboard + mouse navigation member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// </summary>
public partial class MainWindow
{
    protected override async void OnKeyDown(KeyEventArgs e)
    {
        if (_viewModel is null)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Up:
                if (_viewModel.SelectedIndex > 0)
                    _viewModel.SelectedIndex--;
                e.Handled = true;
                break;

            case Key.Down:
                if (_viewModel.SelectedIndex < _viewModel.Items.Count - 1)
                    _viewModel.SelectedIndex++;
                e.Handled = true;
                break;

            case Key.Enter:
                _viewModel.NavigateInto();
                e.Handled = true;
                break;

            case Key.Back:
                if (!_viewModel.SearchBackspace())
                    _viewModel.NavigateUp();
                e.Handled = true;
                break;

            case Key.Escape:
                if (!_viewModel.CancelSearch())
                    _viewModel.NavigateUp();
                e.Handled = true;
                break;

            case Key.F1:
                _ = HelpDialogBuilder.ShowHelpAsync(this);
                e.Handled = true;
                break;

            case Key.F3:
                _ = LookupSelectedGameMetadataAsync();
                e.Handled = true;
                break;

            case Key.F4:
                await OpenGameSetupAsync();
                e.Handled = true;
                break;

            case Key.F5:
                _ = RefreshCurrentRootAsync();
                e.Handled = true;
                break;

            case Key.F8:
                _ = OpenFilterAsync();
                e.Handled = true;
                break;

            case Key.F10:
                Close();
                e.Handled = true;
                break;

            case Key.F2:
                await OpenLibrarySetupAsync();
                e.Handled = true;
                break;

            default:
                // Plan 122: unbound printable keys silently build a live search query.
                if (e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift
                    && TryMapPrintable(e.Key, out char ch))
                {
                    _viewModel.AppendSearchChar(ch);
                    e.Handled = true;
                    break;
                }
                base.OnKeyDown(e);
                break;
        }
    }

    /// <summary>
    /// Maps unmodified printable keys to characters for type-to-search.
    /// Letters, digits, space, dash, period, and apostrophe (title punctuation).
    /// </summary>
    private static bool TryMapPrintable(Key key, out char ch)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            ch = (char)('a' + (key - Key.A));
            return true;
        }
        if (key is >= Key.D0 and <= Key.D9)
        {
            ch = (char)('0' + (key - Key.D0));
            return true;
        }
        ch = key switch
        {
            Key.Space => ' ',
            Key.OemMinus => '-',
            Key.OemPeriod => '.',
            Key.OemQuotes => '\'',
            _ => default,
        };
        return ch != default;
    }

    private void LeftListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var item = _viewModel?.SelectedItem;
        if (item is null) return;

        if (item.Kind == FileSystemEntryKind.ParentDirectory)
            _viewModel!.NavigateUp();
        else if (item.Kind == FileSystemEntryKind.File)
            _ = LaunchSelectedGameAsync();
        else if (item.Kind == FileSystemEntryKind.Directory)
            _viewModel!.NavigateInto();
    }
}