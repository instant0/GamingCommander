using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GamingCommander.App.Services;
using GamingCommander.App.ViewModels;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

public partial class WizardWindow : Window
{
    private readonly WizardViewModel _vm;
    private readonly StackPanel _entriesPanel;
    private readonly TextBlock _progressText;

    /// <summary>First-run wizard window. Guides user through initial library root configuration.</summary>
    public WizardWindow(IConfigService configService, IGamesDatabaseService dbService)
    {
        InitializeComponent();

        var blacklist = new BlacklistLoader(AppDomain.CurrentDomain.BaseDirectory).Load();
        _vm = new WizardViewModel(configService, dbService, this, blacklist);
        DataContext = _vm;
        _entriesPanel = this.FindControl<StackPanel>("EntriesPanel")!;
        _progressText = this.FindControl<TextBlock>("ScanProgressText")!;

        this.FindControl<Button>("AddFolderButton")!.Click += async (_, _) =>
        {
            await _vm.AddEntryAsync();
        };
        this.FindControl<Button>("SkipButton")!.Click += (_, _) => _vm.Cancel();
        this.FindControl<Button>("FinishButton")!.Click += (_, _) => _vm.Finish();

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
                Background = AppTheme.ButtonBgDanger,
                Foreground = AppTheme.TextDanger,
                Padding = new Thickness(8, 4),
                MinWidth = 30,
            };
            removeBtn.Click += (_, _) => _vm.RemoveEntry(captured);
            Grid.SetColumn(removeBtn, 4);

            var scanBtn = new Button
            {
                Content = entry.IsScanned ? "Rescan" : "Scan",
                Background = AppTheme.ButtonBgAction,
                Foreground = AppTheme.TextAccent,
                Padding = new Thickness(8, 4),
                MinWidth = 60,
                IsEnabled = !entry.IsScanning,
            };
            scanBtn.Click += async (_, _) =>
            {
                scanBtn.IsEnabled = false;
                scanBtn.Content = "...";
                await _vm.ScanEntryAsync(captured);
                RenderEntries();
            };
            Grid.SetColumn(scanBtn, 3);

            var combo = new ComboBox
            {
                ItemsSource = GameSourceParser.SourceDisplayNames,
                SelectedItem = entry.SelectedType,
                MinWidth = 100,
                Margin = new Thickness(8, 0),
            };
            combo.SelectionChanged += (_, _) => entry.SelectedType = combo.SelectedItem?.ToString() ?? "Standalone";
            Grid.SetColumn(combo, 1);

            var scanBadge = new TextBlock
            {
                Text = entry.IsScanning ? "scanning..."
                    : entry.GameCount > 0 ? $"{entry.GameCount} games"
                    : entry.IsScanned ? "0 games"
                    : "not scanned",
                Foreground = AppTheme.TextSuccess,
                FontSize = AppTheme.FontSizeLabel,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };
            Grid.SetColumn(scanBadge, 2);

            var border = new Border
            {
                BorderBrush = AppTheme.SeparatorBorder,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6),
                Child = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(110, GridUnitType.Pixel),
                        new ColumnDefinition(90, GridUnitType.Pixel),
                        new ColumnDefinition(70, GridUnitType.Pixel),
                        new ColumnDefinition(40, GridUnitType.Pixel),
                    ],
                    Children =
                    {
                        new TextBlock { Text = entry.Path, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = AppTheme.FontSizeBody },
                        combo,
                        scanBadge,
                        scanBtn,
                        removeBtn,
                    },
                },
            };

            _entriesPanel.Children.Add(border);
        }
    }
}
