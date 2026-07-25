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
    private readonly GameEntry _originalGame;
    private readonly string _rootPath;
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _dbService;
    private readonly string _gameFolderPath;

    /// <summary>The editable display name of the game.</summary>
    public string DisplayName { get; set; }
    /// <summary>The currently selected game source type (e.g., "Steam", "GOG").</summary>
    public string SelectedType { get; set; }
    /// <summary>The full path to the game's primary executable.</summary>
    public string ExecutablePath { get; set; }
    /// <summary>The full path to the game's launcher executable (if any).</summary>
    public string LauncherPath { get; set; }
    /// <summary>Command-line arguments passed to the game on launch.</summary>
    public string CommandLineArguments { get; set; }
    /// <summary>The path to the game's store manifest file (Epic .item, etc.).</summary>
    public string ManifestPath { get; set; }

    /// <summary>F4 game editing dialog. Allows user to modify game metadata (name, type, executable, launcher, args).</summary>
    public GameSetupWindow(
        GameEntry game,
        string rootPath,
        IConfigService configService,
        IGamesDatabaseService dbService)
    {
        InitializeComponent();

        _originalGame = game;
        _rootPath = rootPath;
        _configService = configService;
        _dbService = dbService;

        // Determine game folder path for file picker start location
        _gameFolderPath = !string.IsNullOrEmpty(game.ExecutablePath)
            ? Path.GetDirectoryName(game.ExecutablePath) ?? rootPath
            : rootPath;

        DisplayName = game.DisplayName;
        SelectedType = game.GameSource.ToString();
        ExecutablePath = game.ExecutablePath;
        LauncherPath = game.LauncherPath;
        CommandLineArguments = game.CommandLineArguments;
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
        panel.Children.Add(MakeComboRow("Game Type", GameSourceParser.SourceDisplayNames, SelectedType, 1));
        panel.Children.Add(MakeFieldRow("Executable Path", ExecutablePath, 2, false, true, "Browse..."));
        panel.Children.Add(MakeFieldRow("Launch Args", CommandLineArguments, 3, false, false, ""));
        panel.Children.Add(MakeFieldRow("Launcher Path", LauncherPath, 4, false, true, "Browse..."));

        // Only show Epic Manifest field for Epic games (BUG-7)
        if (SelectedType == "Epic")
            panel.Children.Add(MakeFieldRow("Epic Manifest", ManifestPath, 5, false, true, "Browse..."));

        // Folder field removed — redundant with path shown at top (BUG-8)

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 16, 0, 0) };

        var deleteBtn = new Button
        {
            Content = "Delete Entry",
            Background = AppTheme.ButtonBgDanger,
            Foreground = AppTheme.TextDanger,
            Padding = new Thickness(16, 8),
        };
        deleteBtn.Click += (_, _) => DeleteAndClose();

        var cancelBtn = new Button
        {
            Content = "Cancel",
            Background = AppTheme.ButtonBgCancel,
            Foreground = AppTheme.TextMuted,
            Padding = new Thickness(16, 8),
            MinWidth = 80,
        };
        cancelBtn.Click += (_, _) => Close();

        var saveBtn = new Button
        {
            Content = "Save",
            Background = AppTheme.ButtonBgSuccess,
            Foreground = AppTheme.TextSuccess,
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

    private StackPanel MakeFieldRow(string label, string value, int fieldIndex, bool readOnly, bool isFile, string pickerLabel)
    {
        var textBox = new TextBox
        {
            Text = value,
            IsReadOnly = readOnly,
            Background = readOnly ? AppTheme.ReadOnlyFieldBg : AppTheme.PaneBg,
            Foreground = readOnly ? AppTheme.TextDimmed : AppTheme.TextPrimary,
        };

        switch (fieldIndex)
        {
            case 0: textBox.TextChanged += (_, _) => DisplayName = textBox.Text ?? ""; break;
            case 2: textBox.TextChanged += (_, _) => ExecutablePath = textBox.Text ?? ""; break;
            case 3: textBox.TextChanged += (_, _) => CommandLineArguments = textBox.Text ?? ""; break;
            case 4: textBox.TextChanged += (_, _) => LauncherPath = textBox.Text ?? ""; break;
            case 5: textBox.TextChanged += (_, _) => ManifestPath = textBox.Text ?? ""; break;
        }

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, Foreground = AppTheme.TextMuted, FontSize = AppTheme.FontSizeLabel });

        if (isFile && !readOnly)
        {
            var grid = new Grid { ColumnDefinitions = [new ColumnDefinition(1, GridUnitType.Star), new ColumnDefinition(GridLength.Auto)] };
            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            var picker = new Button
            {
                Content = pickerLabel,
                Background = AppTheme.ButtonBgAction,
                Foreground = AppTheme.TextAccent,
                Padding = new Thickness(8, 4),
                FontSize = AppTheme.FontSizeLabel,
            };
            picker.Click += async (_, _) =>
            {
                if (fieldIndex == 2 || fieldIndex == 4) // Executable Path or Launcher Path
                {
                    var result = await StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions
                        {
                            Title = $"Select {label}",
                            FileTypeFilter = [new FilePickerFileType("Executable") { Patterns = ["*.exe"] }],
                        });
                    if (result.Count > 0) textBox.Text = result[0].Path.LocalPath;
                }
                else
                {
                    var result = await StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions
                        {
                            Title = $"Select {label}",
                        });
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

    private StackPanel MakeComboRow(string label, string[] items, string selected, int fieldIndex)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, Foreground = AppTheme.TextMuted, FontSize = AppTheme.FontSizeLabel });

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

    /// <summary>
    /// Saves the edited game entry to the database and closes the dialog.
    /// </summary>
    private void SaveAndClose()
    {
        GameSourceKind newType = GameSourceParser.ParseFromString(SelectedType);

        AppConfig config = _configService.Load();
        GameSourceKind rootDefault = config.LibraryRoots
            .FirstOrDefault(r => r.RootPath.Equals(_rootPath, StringComparison.OrdinalIgnoreCase))?
            .DefaultType ?? GameSourceKind.Standalone;

        var updated = _originalGame with
        {
            DisplayName = DisplayName,
            GameSource = newType,
            IsSourceOverridden = newType != rootDefault,
            ExecutablePath = ExecutablePath,
            LauncherPath = LauncherPath,
            CommandLineArguments = CommandLineArguments,
            ManifestPath = ManifestPath,
        };

        _dbService.UpdateGameEntry(_rootPath, updated);
        Close();
    }

    /// <summary>
    /// Deletes the game entry from the database and closes the dialog.
    /// </summary>
    private void DeleteAndClose()
    {
        _dbService.DeleteGameEntry(_rootPath, _originalGame.Id);
        Close();
    }
}
