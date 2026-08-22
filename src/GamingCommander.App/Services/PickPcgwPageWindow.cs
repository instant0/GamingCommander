using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>F3: pick among several PCGW pages for the same title.</summary>
internal static class PickPcgwPageWindow
{
    private sealed record Choice(string Title)
    {
        public override string ToString() => PcgwTitleFilter.FormatChoice(Title);
    }

    public static async Task<string?> ShowAsync(
        Window owner,
        IReadOnlyList<string> titles,
        string? preferred)
    {
        var choices = titles.Select(t => new Choice(t)).ToList();
        Choice? selected = choices.FirstOrDefault(c =>
            preferred is not null && c.Title.Equals(preferred, StringComparison.OrdinalIgnoreCase));

        var list = new ListBox
        {
            ItemsSource = choices,
            SelectedItem = selected ?? choices[0],
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
            chosen = (list.SelectedItem as Choice)?.Title;
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
