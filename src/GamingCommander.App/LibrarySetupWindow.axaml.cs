using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GamingCommander.App.Services;
using GamingCommander.App.ViewModels;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

public partial class LibrarySetupWindow : Window
{
    private readonly LibrarySetupViewModel _vm;

    /// <summary>Unified library setup window. Handles first-run onboarding and ongoing library management.</summary>
    public LibrarySetupWindow(
        IConfigService configService,
        IGamesDatabaseService dbService,
        ILibraryManager libraryManager,
        SteamInstallPathLocator? steamLocator = null,
        bool isFirstRun = false)
    {
        InitializeComponent();

        _vm = new LibrarySetupViewModel(configService, dbService, libraryManager, this, steamLocator, isFirstRun);
        DataContext = _vm;

        var addBtn = this.FindControl<Button>("AddRootButton")!;
        var statusText = this.FindControl<TextBlock>("ScanStatusText")!;

        var epicBtn = this.FindControl<Button>("AddEpicButton");
        if (epicBtn is not null)
        {
            epicBtn.Click += async (_, _) =>
            {
                epicBtn.IsEnabled = false;
                statusText.Text = "Adding Epic catalog...";
                await _vm.AddEpicCatalogAsync();
                statusText.Text = string.IsNullOrEmpty(_vm.ScanStatus)
                    ? "Epic catalog added."
                    : _vm.ScanStatus;
                RenderRoots();
                epicBtn.IsEnabled = true;
            };
        }

        var steamBtn = this.FindControl<Button>("AddSteamButton");
        if (steamBtn is not null)
        {
            // Q4: hidden when Steam is not installed (no registry InstallPath / empty vdf).
            steamBtn.IsVisible = _vm.CanAddSteamLibraries;
            steamBtn.Click += async (_, _) =>
            {
                steamBtn.IsEnabled = false;
                statusText.Text = "Adding Steam libraries (from libraryfolders.vdf)...";
                await _vm.AddSteamLibrariesAsync();
                statusText.Text = string.IsNullOrEmpty(_vm.ScanStatus)
                    ? "Steam catalog added."
                    : _vm.ScanStatus;
                RenderRoots();
                steamBtn.IsVisible = _vm.CanAddSteamLibraries;
                steamBtn.IsEnabled = true;
            };
        }

        addBtn.Click += async (_, _) =>
        {
            addBtn.IsEnabled = false;
            statusText.Text = "Opening folder picker...";
            await _vm.AddLibraryFolderAsync();
            // Show rejection reason if set, otherwise default message
            statusText.Text = string.IsNullOrEmpty(_vm.ScanStatus)
                ? "Scanning complete."
                : _vm.ScanStatus;
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

        foreach (LibraryEntry entry in _vm.Entries)
        {
            LibraryEntry captured = entry;

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
                rescanBtn.IsEnabled = false;
                rescanBtn.Content = "...";
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
                MinWidth = 148,
                Margin = new Thickness(8, 0, 8, 0),
            };
            combo.SelectionChanged += (_, _) => entry.DefaultType = combo.SelectedItem?.ToString() ?? "Standalone";

            var nameBlock = new TextBlock
            {
                Text = entry.Name,
                FontWeight = FontWeight.Bold,
                FontSize = AppTheme.FontSizeItem,
            };

            // Scan progress badge: "Scanning..." / "✓ N games" / "0 games" / "Not scanned"
            string statusBadge = entry.IsScanning ? "⏳ Scanning..."
                : entry.GameCount > 0 ? $"✓ {entry.GameCount} games"
                : entry.IsScanned ? "0 games"
                : "Not scanned";

            var statusColor = entry.IsScanning ? AppTheme.TextSuccess
                : entry.GameCount > 0 ? AppTheme.TextSuccess
                : AppTheme.TextMuted;

            var countBlock = new TextBlock
            {
                Text = statusBadge,
                Foreground = statusColor,
                FontSize = AppTheme.FontSizeLabel,
            };

            var row = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8) };
            var topRow = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(GridLength.Auto),
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
                Text = "(no libraries configured — click '+ Add Folder' to begin)",
                Foreground = AppTheme.TextMuted,
                FontStyle = FontStyle.Italic,
                Margin = new Thickness(0, 8),
            });
        }
    }
}
