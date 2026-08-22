using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GamingCommander.App.Services;

/// <summary>F3: pick among several PCGW pages for the same title.</summary>
internal static class PickPcgwPageWindow
{
    public static async Task<string?> ShowAsync(
        Window owner,
        IReadOnlyList<string> titles,
        string? preferred)
    {
        var list = new ListBox
        {
            ItemsSource = titles,
            SelectedItem = preferred is not null && titles.Contains(preferred) ? preferred : titles[0],
            MinHeight = 180,
        };

        var window = new Window
        {
            Title = "Multiple PCGW pages",
            Width = 480,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
        };

        string? chosen = null;
        var ok = new Button { Content = "Use this page", Padding = new Thickness(12, 6) };
        ok.Click += (_, _) =>
        {
            chosen = list.SelectedItem as string;
            window.Close();
        };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(12, 6), Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => window.Close();

        window.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Several wiki pages match. Pick the one for this install.",
                    TextWrapping = TextWrapping.Wrap,
                },
                list,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { ok, cancel },
                },
            },
        };

        await window.ShowDialog(owner).ConfigureAwait(true);
        return chosen;
    }
}
