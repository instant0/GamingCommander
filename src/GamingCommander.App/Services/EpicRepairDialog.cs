using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GamingCommander.App.Services;

/// <summary>Shown before repairing an Epic registration — one official Update per launcher run.</summary>
internal static class EpicRepairDialog
{
    public static async Task<bool> ConfirmAsync(Window owner, string gameName, RepairPlan plan)
    {
        var window = new Window
        {
            Title = "Repair Epic registration",
            Width = 520,
            Height = 420,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        bool ok = false;
        var go = new Button { Content = "Repair registration", Padding = new Thickness(12, 6) };
        go.Click += (_, _) =>
        {
            ok = true;
            window.Close();
        };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(12, 6), Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => window.Close();

        string title = string.IsNullOrWhiteSpace(gameName)
            ? "Repair the Epic registration?"
            : $"Repair the Epic registration for {gameName}?";
        string detail =
            $"This updates the existing .item; it does not create one.\n\n" +
            $"Target: {plan.TargetPath}\n" +
            $"Payload: {plan.StagedLength} bytes\n" +
            $"Folder marker: {plan.Marker}\n" +
            $"Backup: {plan.BackupPath}";

        window.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.Bold,
                },
                new TextBlock
                {
                    Text = detail,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas, Menlo, monospace"),
                    FontSize = 12,
                },
                new TextBlock
                {
                    Text =
                        "Epic can officialize only ONE of these per launcher run.\n\n" +
                        "1. Quit Epic if it is running.\n" +
                        "2. Confirm this repair.\n" +
                        "3. Start Epic and use Update on this title only.\n" +
                        "4. Quit Epic before repairing another game.\n\n" +
                        "If you repair several registrations and then open Epic, unofficial ones can disappear.",
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
