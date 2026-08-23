using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GamingCommander.App.Services;

/// <summary>Shown when the user clicks Write Epic .item — one official Update per launcher run.</summary>
internal static class EpicRepairDialog
{
    public static async Task<bool> ConfirmAsync(Window owner, string gameName)
    {
        var window = new Window
        {
            Title = "Repair Epic .item",
            Width = 460,
            Height = 320,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        bool ok = false;
        var go = new Button { Content = "Write .item", Padding = new Thickness(12, 6) };
        go.Click += (_, _) =>
        {
            ok = true;
            window.Close();
        };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(12, 6), Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => window.Close();

        window.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(gameName) ? "Write an Epic identification .item?" : $"Write an Epic identification .item for {gameName}?",
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.Bold,
                },
                new TextBlock
                {
                    Text =
                        "Epic can officialize only ONE of these per launcher run.\n\n" +
                        "1. Quit Epic if it is running.\n" +
                        "2. Confirm this write.\n" +
                        "3. Start Epic and use Update on this title only.\n" +
                        "4. Quit Epic before repairing another game.\n\n" +
                        "If you write several .item files and then open Epic, unofficial ones can disappear.",
                    TextWrapping = TextWrapping.Wrap,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 12, 0, 0),
                    Children = { go, cancel },
                },
            },
        };

        await window.ShowDialog(owner).ConfigureAwait(true);
        return ok;
    }
}
