using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App;

public partial class GameSetupWindow : Window
{
    private readonly GameEntry _originalGame;
    private readonly string _rootPath;
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _dbService;
    private readonly string _gameFolderPath;
    private readonly IReadOnlyList<GameMetadataCommandLine> _catalog;
    private readonly List<CheckBox> _catalogChecks = [];
    private TextBox? _extrasBox;
    private TextBlock? _previewText;
    private bool _syncingExtras;

    /// <summary>The editable display name of the game.</summary>
    public string DisplayName { get; set; }
    /// <summary>The currently selected game source type (e.g., "Steam", "GOG").</summary>
    public string SelectedType { get; set; }
    /// <summary>The full path to the game's primary executable.</summary>
    public string ExecutablePath { get; set; }
    /// <summary>The full path to the game's launcher executable (if any).</summary>
    public string LauncherPath { get; set; }
    /// <summary>Command-line arguments passed to the game on launch (Steam URI or legacy extras).</summary>
    public string CommandLineArguments { get; set; }
    /// <summary>Constructed extras from PCGW toggles / free text. Applied on exe launch only.</summary>
    public string ExtraLaunchArguments { get; set; }
    /// <summary>The path to the game's store manifest file (Epic .item, etc.).</summary>
    public string ManifestPath { get; set; }
    /// <summary>User-defined tags (comma-separated input).</summary>
    public string TagsInput { get; set; }
    /// <summary>The current tags list (for display).</summary>
    private List<string> _currentTags;

    /// <summary>F4 game editing dialog. Allows user to modify game metadata (name, type, executable, launcher, args, tags).</summary>
    public GameSetupWindow(
        GameEntry game,
        string rootPath,
        IConfigService configService,
        IGamesDatabaseService dbService,
        IReadOnlyList<GameMetadataCommandLine>? catalog = null)
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
        ExtraLaunchArguments = game.ExtraLaunchArguments;
        _catalog = catalog ?? [];
        ManifestPath = game.ManifestPath;
        _currentTags = new List<string>(game.Tags);
        TagsInput = TagNormalizer.ToCommaSeparated(_currentTags);

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
        panel.Children.Add(MakeCatalogSection());
        panel.Children.Add(MakeExtrasRow());
        panel.Children.Add(MakePreviewRow());
        panel.Children.Add(MakeFieldRow("Launcher Path", LauncherPath, 4, false, true, "Browse..."));

        // Only show Epic Manifest field for Epic games (BUG-7)
        if (SelectedType == "Epic")
            panel.Children.Add(MakeFieldRow("Epic Manifest", ManifestPath, 5, false, true, "Browse..."));

        // Tags field (comma-separated)
        panel.Children.Add(MakeTagsRow("Tags (comma-separated)", TagsInput, 6));

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

    private StackPanel MakeTagsRow(string label, string value, int fieldIndex)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = label, Foreground = AppTheme.TextMuted, FontSize = AppTheme.FontSizeLabel });

        var textBox = new TextBox
        {
            Text = value,
            Background = AppTheme.PaneBg,
            Foreground = AppTheme.TextPrimary,
            Watermark = "e.g., RPG, Co-op, Story Rich",
        };
        textBox.TextChanged += (_, _) => TagsInput = textBox.Text ?? "";
        panel.Children.Add(textBox);

        // Show current tags as a summary below the input
        if (_currentTags.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"Current: {TagNormalizer.ToCommaSeparated(_currentTags)}",
                Foreground = AppTheme.TextDimmed,
                FontSize = AppTheme.FontSizeLabel - 1,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        return panel;
    }

    private StackPanel MakeCatalogSection()
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = "PCGW launch options",
            Foreground = AppTheme.TextMuted,
            FontSize = AppTheme.FontSizeLabel,
        });

        if (_catalog.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "No cached PCGW arguments. Enable online metadata and select the game once.",
                Foreground = AppTheme.TextDimmed,
                FontSize = AppTheme.FontSizeLabel,
                TextWrapping = TextWrapping.Wrap,
            });
            return panel;
        }

        _catalogChecks.Clear();
        foreach (GameMetadataCommandLine row in _catalog)
        {
            if (row.NeedsValue || row.Argument.Contains(' '))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"{row.Argument}  — needs value, type in extras",
                    Foreground = AppTheme.TextDimmed,
                    FontSize = AppTheme.FontSizeLabel,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(18, 0, 0, 0),
                });
                continue;
            }

            var check = new CheckBox
            {
                Content = row.Argument,
                IsChecked = LaunchArgumentComposer.ContainsToken(ExtraLaunchArguments, row.Argument),
                Foreground = AppTheme.TextPrimary,
                Tag = row.Argument,
            };
            string argument = row.Argument;
            check.IsCheckedChanged += (_, _) =>
            {
                if (_syncingExtras)
                    return;
                ExtraLaunchArguments = LaunchArgumentComposer.Toggle(
                    ExtraLaunchArguments, argument, check.IsChecked == true);
                if (_extrasBox is not null && _extrasBox.Text != ExtraLaunchArguments)
                    _extrasBox.Text = ExtraLaunchArguments;
                UpdatePreview();
            };
            _catalogChecks.Add(check);
            panel.Children.Add(check);
        }

        return panel;
    }

    private StackPanel MakeExtrasRow()
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = "Launch extras",
            Foreground = AppTheme.TextMuted,
            FontSize = AppTheme.FontSizeLabel,
        });

        _extrasBox = new TextBox
        {
            Text = ExtraLaunchArguments,
            Background = AppTheme.PaneBg,
            Foreground = AppTheme.TextPrimary,
            Watermark = "--launcher-skip -windowed",
        };
        _extrasBox.TextChanged += (_, _) =>
        {
            ExtraLaunchArguments = _extrasBox.Text ?? "";
            SyncCatalogChecks();
            UpdatePreview();
        };
        panel.Children.Add(_extrasBox);
        return panel;
    }

    private StackPanel MakePreviewRow()
    {
        var panel = new StackPanel { Spacing = 4 };
        _previewText = new TextBlock
        {
            Foreground = AppTheme.TextAccent,
            FontSize = AppTheme.FontSizeLabel,
            TextWrapping = TextWrapping.Wrap,
        };
        panel.Children.Add(_previewText);
        UpdatePreview();
        return panel;
    }

    private void SyncCatalogChecks()
    {
        _syncingExtras = true;
        try
        {
            foreach (CheckBox check in _catalogChecks)
            {
                string argument = check.Tag as string ?? "";
                check.IsChecked = LaunchArgumentComposer.ContainsToken(ExtraLaunchArguments, argument);
            }
        }
        finally
        {
            _syncingExtras = false;
        }
    }

    private void UpdatePreview()
    {
        if (_previewText is null)
            return;

        var draft = _originalGame with
        {
            CommandLineArguments = CommandLineArguments,
            ExtraLaunchArguments = ExtraLaunchArguments,
            ExecutablePath = ExecutablePath,
        };
        (string target, string args) = GameLaunchResolver.Resolve(draft);
        _previewText.Text = string.IsNullOrEmpty(args)
            ? $"Will start: {target}"
            : $"Will start: {target} {args}";
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

        // Parse tags from input
        List<string> newTags = TagNormalizer.ParseFromCommaSeparated(TagsInput);

        // Build UserOverrides — track fields the user manually changed
        var userOverrides = new Dictionary<string, string>(_originalGame.UserOverrides);
        string now = DateTimeOffset.UtcNow.ToString("O");

        // Check if display name changed
        if (!_originalGame.DisplayName.Equals(DisplayName, StringComparison.Ordinal))
        {
            userOverrides[GameEntryFields.DisplayName] = now;
        }

        // Check if executable path changed
        if (!_originalGame.ExecutablePath.Equals(ExecutablePath, StringComparison.Ordinal))
        {
            userOverrides[GameEntryFields.ExecutablePath] = now;
        }

        // Check if launcher path changed
        if (!_originalGame.LauncherPath.Equals(LauncherPath, StringComparison.Ordinal))
        {
            userOverrides[GameEntryFields.LauncherPath] = now;
        }

        // Check if command line args changed
        if (!_originalGame.CommandLineArguments.Equals(CommandLineArguments, StringComparison.Ordinal))
        {
            userOverrides[GameEntryFields.CommandLineArguments] = now;
        }

        if (!_originalGame.ExtraLaunchArguments.Equals(ExtraLaunchArguments, StringComparison.Ordinal))
            userOverrides[GameEntryFields.ExtraLaunchArguments] = now;

        // Check if manifest path changed
        if (!_originalGame.ManifestPath.Equals(ManifestPath, StringComparison.Ordinal))
        {
            userOverrides[GameEntryFields.ManifestPath] = now;
        }

        // Check if source type changed
        if (_originalGame.GameSource != newType)
        {
            userOverrides[GameEntryFields.GameSource] = now;
        }

        // Check if tags changed
        if (!_currentTags.SequenceEqual(newTags, StringComparer.OrdinalIgnoreCase))
        {
            userOverrides[GameEntryFields.Tags] = now;
        }

        var updated = _originalGame with
        {
            DisplayName = DisplayName,
            GameSource = newType,
            IsSourceOverridden = newType != rootDefault,
            ExecutablePath = ExecutablePath,
            LauncherPath = LauncherPath,
            CommandLineArguments = CommandLineArguments,
            ExtraLaunchArguments = ExtraLaunchArguments,
            ManifestPath = ManifestPath,
            Tags = newTags,
            UserOverrides = userOverrides,
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
