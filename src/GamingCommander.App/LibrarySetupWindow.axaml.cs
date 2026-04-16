using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GamingCommander.App.Services;
using GamingCommander.App.ViewModels;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

public partial class LibrarySetupWindow : Window
{
    private readonly LibrarySetupViewModel _vm;

    public LibrarySetupWindow(
        IConfigService configService,
        IGamesDatabaseService dbService,
        ILibraryManager libraryManager,
        FolderScanner scanner)
    {
        InitializeComponent();

        _vm = new LibrarySetupViewModel(configService, dbService, libraryManager, scanner, this);
        DataContext = _vm;

        this.FindControl<Button>("AddRootButton").Click += async (_, _) =>
        {
            await _vm.AddRootAsync();
            RenderRoots();
        };
        this.FindControl<Button>("CloseButton").Click += (_, _) => _vm.Close();

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
                Background = new SolidColorBrush(Color.Parse("#1A2A3A")),
                Foreground = new SolidColorBrush(Color.Parse("#8CD8FF")),
                Padding = new Thickness(12, 4),
                FontSize = 11,
            };
            rescanBtn.Click += async (_, _) =>
            {
                await _vm.RescanAsync(captured);
                RenderRoots();
            };

            var removeBtn = new Button
            {
                Content = "Remove",
                Background = new SolidColorBrush(Color.Parse("#3A1A1A")),
                Foreground = new SolidColorBrush(Color.Parse("#FF6B6B")),
                Padding = new Thickness(12, 4),
                FontSize = 11,
            };
            removeBtn.Click += (_, _) =>
            {
                _vm.RemoveEntry(captured);
                RenderRoots();
            };

            var combo = new ComboBox
            {
                ItemsSource = _vm.AvailableTypes,
                SelectedItem = entry.DefaultType,
                MinWidth = 100,
            };
            combo.SelectionChanged += (_, _) => entry.DefaultType = combo.SelectedItem?.ToString() ?? "Standalone";

            var nameBlock = new TextBlock
            {
                Text = entry.Path,
                FontWeight = FontWeight.Bold,
                FontSize = 13,
            };
            var countBlock = new TextBlock
            {
                Text = $"{entry.GameCount} game(s) — {entry.DefaultType}",
                Foreground = new SolidColorBrush(Color.Parse("#6A7E8E")),
                FontSize = 11,
            };

            var row = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8) };
            row.Children.Add(new Grid
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
            });
            row.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0), Children = { rescanBtn, removeBtn } });

            panel.Children.Add(row);
            panel.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#1A2A3A")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 8, 0, 0),
            });
        }

        if (_vm.Entries.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "(no library roots configured — click '+ Add Root' to begin)",
                Foreground = new SolidColorBrush(Color.Parse("#6A7E8E")),
                FontStyle = FontStyle.Italic,
                Margin = new Thickness(0, 8),
            });
        }
    }
}
