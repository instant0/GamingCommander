using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>F8: pick a tag/store or type a wildcard. Clear restores the normal list.</summary>
internal static class FilterWindow
{
    public static async Task<GameFilter?> ShowAsync(
        Window owner,
        IReadOnlyList<GameFilterOption> options,
        GameFilter? current)
    {
        var list = new ListBox
        {
            ItemsSource = options,
            MinHeight = 200,
            MaxHeight = 320,
        };
        if (current is not null)
        {
            GameFilterOption? selected = options.FirstOrDefault(o =>
                o.Kind == current.Kind
                && o.Value.Equals(current.Value, StringComparison.OrdinalIgnoreCase));
            if (selected is not null)
                list.SelectedItem = selected;
        }

        var wildcard = new TextBox
        {
            Watermark = "Wildcard (name, folder, tag, store)",
            Text = current?.Kind == GameFilterKind.Wildcard ? current.Value : string.Empty,
        };

        var window = new Window
        {
            Title = "Filter games",
            Width = 440,
            Height = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
        };

        GameFilter? result = current;
        bool applied = false;

        var apply = new Button { Content = "Apply", Padding = new Thickness(12, 6) };
        apply.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(wildcard.Text))
                result = new GameFilter(GameFilterKind.Wildcard, wildcard.Text.Trim());
            else if (list.SelectedItem is GameFilterOption option)
                result = new GameFilter(option.Kind, option.Value);
            else
                result = null;
            applied = true;
            window.Close();
        };

        var clear = new Button
        {
            Content = "Clear",
            Padding = new Thickness(12, 6),
            Margin = new Thickness(8, 0, 0, 0),
        };
        clear.Click += (_, _) =>
        {
            result = null;
            applied = true;
            window.Close();
        };

        wildcard.KeyDown += (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Enter)
                return;
            list.SelectedItem = null;
            apply.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        };

        window.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Tags and store labels from every library. Apply one, or type a wildcard.",
                    TextWrapping = TextWrapping.Wrap,
                },
                list,
                wildcard,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { apply, clear },
                },
            },
        };

        await window.ShowDialog(owner).ConfigureAwait(true);
        return applied ? result : current;
    }
}
