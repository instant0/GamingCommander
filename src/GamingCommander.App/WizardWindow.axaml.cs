using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GamingCommander.App.ViewModels;
using GamingCommander.Core;

namespace GamingCommander.App;

public partial class WizardWindow : Window
{
    private readonly WizardViewModel _vm;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _progressText;

    public WizardWindow(IConfigService configService, IGamesDatabaseService dbService)
    {
        InitializeComponent();

        _vm = new WizardViewModel(configService, dbService, this);
        DataContext = _vm;
        _entriesPanel = this.FindControl<StackPanel>("EntriesPanel")!;
        _progressText = this.FindControl<TextBlock>("ScanProgressText")!;

        this.FindControl<Button>("AddFolderButton").Click += async (_, _) =>
        {
            await _vm.AddEntryAsync();
        };
        this.FindControl<Button>("SkipButton").Click += (_, _) => _vm.Cancel();
        this.FindControl<Button>("FinishButton").Click += (_, _) => _vm.Finish();

        _vm.Entries.CollectionChanged += (_, _) => RenderEntries();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WizardViewModel.ScanStatus))
                _progressText.Text = _vm.ScanStatus;
        };

        Loaded += (_, _) => RenderEntries();
    }

    private void RenderEntries()
    {
        _entriesPanel.Children.Clear();
        foreach (WizardLibraryEntry entry in _vm.Entries)
        {
            WizardLibraryEntry captured = entry;

            var removeBtn = new Button
            {
                Content = "X",
                Background = new SolidColorBrush(Color.Parse("#3A1A1A")),
                Foreground = new SolidColorBrush(Color.Parse("#FF6B6B")),
                Padding = new Thickness(8, 4),
                MinWidth = 30,
            };
            removeBtn.Click += (_, _) => _vm.RemoveEntry(captured);

            var combo = new ComboBox
            {
                ItemsSource = _vm.AvailableTypes,
                SelectedItem = entry.SelectedType,
                MinWidth = 100,
                Margin = new Thickness(8, 0),
            };
            combo.SelectionChanged += (_, _) => entry.SelectedType = combo.SelectedItem?.ToString() ?? "Standalone";

            var scanBadge = new TextBlock
            {
                Text = entry.GameCount > 0 ? $"{entry.GameCount} games"
                    : entry.IsScanned ? "0 games"
                    : "not scanned",
                Foreground = new SolidColorBrush(Color.Parse("#7FB7A5")),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#243340")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6),
                Child = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(110, GridUnitType.Pixel),
                        new ColumnDefinition(90, GridUnitType.Pixel),
                        new ColumnDefinition(40, GridUnitType.Pixel),
                    ],
                    Children =
                    {
                        new TextBlock { Text = entry.Path, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 },
                        combo,
                        scanBadge,
                        removeBtn,
                    },
                },
            };

            _entriesPanel.Children.Add(border);
        }
    }
}
