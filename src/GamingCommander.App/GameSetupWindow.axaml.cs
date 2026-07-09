using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App;

public partial class GameSetupWindow : Window
{
    private readonly GameEntry _original;
    private readonly string _rootPath;
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _dbService;

    public string DisplayName { get; set; }
    public string SelectedType { get; set; }
    public string ExecutablePath { get; set; }
    public string LauncherPath { get; set; }
    public string CmdlineArgs { get; set; }
    public string ManifestPath { get; set; }

    public string[] AvailableTypes { get; } =
    [
        "Standalone", "Steam", "GOG", "Epic", "EA App", "Ubisoft Connect",
    ];

    public GameSetupWindow(
        GameEntry game,
        string rootPath,
        IConfigService configService,
        IGamesDatabaseService dbService)
    {
        InitializeComponent();

        _original = game;
        _rootPath = rootPath;
        _configService = configService;
        _dbService = dbService;

        DisplayName = game.DisplayName;
        SelectedType = game.GameSource.ToString();
        ExecutablePath = game.ExecutablePath;
        LauncherPath = game.LauncherPath;
        CmdlineArgs = game.CmdlineArgs;
        ManifestPath = game.ManifestPath;

        this.FindControl<TextBlock>("TitleText")!.Text = $"Configure: {game.DisplayName}";
        this.FindControl<TextBlock>("SubtitleText")!.Text = game.ExecutablePath;

        Loaded += (_, _) => RenderFields();
    }

    private void RenderFields()
    {
        var panel = this.FindControl<StackPanel>("FieldsPanel")!;
        panel.Children.Clear();

        panel.Children.Add(MakeFieldRow("Display Name", DisplayName, 0, false, false, ""));
        panel.Children.Add(MakeComboRow("Game Type", AvailableTypes, SelectedType, 1));
        panel.Children.Add(MakeFieldRow("Executable Path", ExecutablePath, 2, false, true, "Browse..."));
        panel.Children.Add(MakeFieldRow("Launcher Path", LauncherPath, 3, false, true, "Browse..."));
        panel.Children.Add(MakeFieldRow("Launch Args", CmdlineArgs, 4, false, false, ""));
        panel.Children.Add(MakeFieldRow("Epic Manifest", ManifestPath, 5, false, true, "Browse..."));
        panel.Children.Add(MakeFieldRow("Folder", _original.FolderName, 6, true, false, ""));

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 16, 0, 0) };

        var deleteBtn = new Button
        {
            Content = "Delete Entry",
            Background = new SolidColorBrush(Color.Parse("#3A1A1A")),
            Foreground = new SolidColorBrush(Color.Parse("#FF6B6B")),
            Padding = new Thickness(16, 8),
        };
        deleteBtn.Click += (_, _) => DeleteAndClose();

        var cancelBtn = new Button
        {
            Content = "Cancel",
            Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            Foreground = new SolidColorBrush(Color.Parse("#6A7E8E")),
            Padding = new Thickness(16, 8),
            MinWidth = 80,
        };
        cancelBtn.Click += (_, _) => Close();

        var saveBtn = new Button
        {
            Content = "Save",
            Background = new SolidColorBrush(Color.Parse("#1A3A2A")),
            Foreground = new SolidColorBrush(Color.Parse("#7FB7A5")),
            Padding = new Thickness(20, 8),
            FontWeight = FontWeight.Bold,
            MinWidth = 80,
        };
        saveBtn.Click += (_, _) => SaveAndClose();

        btnRow.Children.Add(deleteBtn);
        btnRow.Children.Add(cancelBtn);
        btnRow.Children.Add(saveBtn);
        panel.Children.Add(btnRow);
    }

    private StackPanel MakeFieldRow(string label, string value, int fieldIdx, bool readOnly, bool isFile, string pickerLabel)
    {
        var textBox = new TextBox
        {
            Text = value,
            IsReadOnly = readOnly,
            Background = readOnly
                ? new SolidColorBrush(Color.Parse("#0A0F14"))
                : new SolidColorBrush(Color.Parse("#0F141A")),
            Foreground = readOnly
                ? new SolidColorBrush(Color.Parse("#4A5E6E"))
                : new SolidColorBrush(Color.Parse("#D7E2F0")),
        };

        switch (fieldIdx)
        {
            case 0: textBox.TextChanged += (_, _) => DisplayName = textBox.Text ?? ""; break;
            case 2: textBox.TextChanged += (_, _) => ExecutablePath = textBox.Text ?? ""; break;
            case 3: textBox.TextChanged += (_, _) => LauncherPath = textBox.Text ?? ""; break;
            case 4: textBox.TextChanged += (_, _) => CmdlineArgs = textBox.Text ?? ""; break;
            case 5: textBox.TextChanged += (_, _) => ManifestPath = textBox.Text ?? ""; break;
        }

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.Parse("#6A7E8E")), FontSize = 11 });

        if (isFile && !readOnly)
        {
            var grid = new Grid { ColumnDefinitions = [new ColumnDefinition(1, GridUnitType.Star), new ColumnDefinition(GridLength.Auto)] };
            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            var picker = new Button
            {
                Content = pickerLabel,
                Background = new SolidColorBrush(Color.Parse("#1A3A4A")),
                Foreground = new SolidColorBrush(Color.Parse("#8CD8FF")),
                Padding = new Thickness(8, 4),
                FontSize = 11,
            };
            picker.Click += async (_, _) =>
            {
                if (fieldIdx == 2 || fieldIdx == 3)
                {
                    var result = await StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions { Title = $"Select {label}", FileTypeFilter = [new FilePickerFileType("Executable") { Patterns = ["*.exe"] }] });
                    if (result.Count > 0) textBox.Text = result[0].Path.LocalPath;
                }
                else
                {
                    var result = await StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions { Title = $"Select {label}" });
                    if (result.Count > 0) textBox.Text = result[0].Path.LocalPath;
                }
            };
            Grid.SetColumn(picker, 1);
            grid.Children.Add(picker);
            panel.Children.Add(grid);
        }
        else
        {
            panel.Children.Add(textBox);
        }

        return panel;
    }

    private StackPanel MakeComboRow(string label, string[] items, string selected, int fieldIdx)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.Parse("#6A7E8E")), FontSize = 11 });

        var combo = new ComboBox
        {
            ItemsSource = items,
            SelectedItem = items.Contains(selected) ? selected : items[0],
            MinWidth = 200,
        };
        combo.SelectionChanged += (_, _) => SelectedType = combo.SelectedItem?.ToString() ?? "Standalone";
        panel.Children.Add(combo);
        return panel;
    }

    private void SaveAndClose()
    {
        GameSourceKind newType = SelectedType switch
        {
            "Steam" => GameSourceKind.Steam,
            "GOG" => GameSourceKind.Gog,
            "Epic" => GameSourceKind.Epic,
            "EA App" => GameSourceKind.EaApp,
            "Ubisoft Connect" => GameSourceKind.UbisoftConnect,
            _ => GameSourceKind.Standalone,
        };

        AppConfig config = _configService.Load();
        GameSourceKind rootDefault = config.LibraryRoots
            .FirstOrDefault(r => r.Path.Equals(_rootPath, StringComparison.OrdinalIgnoreCase))?
            .DefaultType ?? GameSourceKind.Standalone;

        var updated = _original with
        {
            DisplayName = DisplayName,
            GameSource = newType,
            Override = newType != rootDefault,
            ExecutablePath = ExecutablePath,
            LauncherPath = LauncherPath,
            CmdlineArgs = CmdlineArgs,
            ManifestPath = ManifestPath,
        };

        _dbService.UpdateGameEntry(_rootPath, updated);
        Close();
    }

    private void DeleteAndClose()
    {
        _dbService.DeleteGameEntry(_rootPath, _original.Id);
        Close();
    }
}
