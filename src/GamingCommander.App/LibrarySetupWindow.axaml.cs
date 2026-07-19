using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GamingCommander.App.ViewModels;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

public partial class LibrarySetupWindow : Window
{
    private readonly LibrarySetupViewModel _vm;

    /// <summary>F2 library setup window. Allows user to manage library roots and folder overrides.</summary>
    public LibrarySetupWindow(
        IConfigService configService,
        IGamesDatabaseService dbService,
        ILibraryManager libraryManager)
    {
        InitializeComponent();

        _vm = new LibrarySetupViewModel(configService, dbService, libraryManager, this);
        DataContext = _vm;

        var addBtn = this.FindControl<Button>("AddRootButton")!;
        var statusText = this.FindControl<TextBlock>("ScanStatusText")!;

        addBtn.Click += async (_, _) =>
        {
            addBtn.IsEnabled = false;
            statusText.Text = "Opening folder picker...";
            await _vm.AddRootAsync();
            statusText.Text = "Scanning complete.";
            RenderRoots();
            addBtn.IsEnabled = true;
        };
        this.FindControl<Button>("CloseButton")!.Click += (_, _) => _vm.Close();

        Loaded += (_, _) => RenderRoots();
    }

    private void RenderRoots()
    {
        var panel = this.FindControl<StackPanel>("RootsPanel")!;
        panel.Children.Clear();

        foreach (LibraryRootEntry entry in _vm.Entries)
        {
            LibraryRootEntry captured = entry;

            var rescanBtn = new Button
            {
                Content = "Rescan",
                Background = AppTheme.ButtonBgSecondary,
                Foreground = AppTheme.TextAccent,
                Padding = new Thickness(12, 4),
                FontSize = AppTheme.FontSizeLabel,
            };
            rescanBtn.Click += async (_, _) =>
            {
                await _vm.RescanAsync(captured);
                RenderRoots();
            };

            var removeBtn = new Button
            {
                Content = "Remove",
                Background = AppTheme.ButtonBgDanger,
                Foreground = AppTheme.TextDanger,
                Padding = new Thickness(12, 4),
                FontSize = AppTheme.FontSizeLabel,
            };
            removeBtn.Click += (_, _) =>
            {
                _vm.RemoveEntry(captured);
                RenderRoots();
            };

            var combo = new ComboBox
            {
                ItemsSource = GameSourceParser.SourceDisplayNames,
                SelectedItem = entry.DefaultType,
                MinWidth = 100,
            };
            combo.SelectionChanged += (_, _) => entry.DefaultType = combo.SelectedItem?.ToString() ?? "Standalone";

            var nameBlock = new TextBlock
            {
                Text = entry.Path,
                FontWeight = FontWeight.Bold,
                FontSize = AppTheme.FontSizeItem,
            };
            var countBlock = new TextBlock
            {
                Text = $"{entry.GameCount} game(s) — {entry.DefaultType}",
                Foreground = AppTheme.TextMuted,
                FontSize = AppTheme.FontSizeLabel,
            };

            var row = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8) };
            var topRow = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(110, GridUnitType.Pixel),
                ],
                Children =
                {
                    new StackPanel { Spacing = 2, Children = { nameBlock, countBlock } },
                    combo,
                },
            };
            Grid.SetColumn(combo, 1);
            row.Children.Add(topRow);
            row.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0), Children = { rescanBtn, removeBtn } });

            panel.Children.Add(row);
            panel.Children.Add(new Border
            {
                BorderBrush = AppTheme.EntryBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 8, 0, 0),
            });
        }

        if (_vm.Entries.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "(no library roots configured — click '+ Add Root' to begin)",
                Foreground = AppTheme.TextMuted,
                FontStyle = FontStyle.Italic,
                Margin = new Thickness(0, 8),
            });
        }
    }
}
