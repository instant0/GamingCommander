using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App;

public partial class MainWindow : Window
{
    private ShellViewModel? _viewModel;
    private IGamesDatabaseService _dbService;
    private IConfigService _configService;
    private ILibrariesService _librariesService;

    private ILibraryManager _libraryManager;
    private IMetadataService? _metadataService;
    private MetadataLookupQueue? _metadataQueue;
    private MetadataOnlineGate? _onlineGate;
    private HttpClient? _probeHttp;

    /// <summary>Primary application window. Manages dual-pane navigation, keyboard shortcuts, and game launching.</summary>
    public MainWindow(
        ShellViewModel shellViewModel,
        IGamesDatabaseService dbService,
        ILibrariesService librariesService,
        ILibraryManager libraryManager,
        IConfigService configService,
        IMetadataService? metadataService = null,
        MetadataOnlineGate? onlineGate = null)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "startup.log"),
                $"[MainWindow ctor] InitializeComponent FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n");
            throw;
        }

        _viewModel = shellViewModel;
        _dbService = dbService;
        _librariesService = librariesService;
        _libraryManager = libraryManager;
        _configService = configService;
        _metadataService = metadataService;
        _onlineGate = onlineGate;

        if (_metadataService is not null)
        {
            _metadataQueue = new MetadataLookupQueue(_metadataService, _configService, _onlineGate);
            _metadataQueue.ItemCompleted += OnMetadataQueueItemCompleted;
            _metadataQueue.ProgressChanged += OnMetadataQueueProgress;
        }

        if (_onlineGate is not null)
        {
            _onlineGate.Changed += UpdateLookupChip;
            Opened += (_, _) => _ = ProbeOnlineAsync();
        }

        Closed += (_, _) =>
        {
            if (_onlineGate is not null)
                _onlineGate.Changed -= UpdateLookupChip;
            if (_metadataQueue is not null)
            {
                _metadataQueue.ItemCompleted -= OnMetadataQueueItemCompleted;
                _metadataQueue.ProgressChanged -= OnMetadataQueueProgress;
                _metadataQueue.Dispose();
            }
            _probeHttp?.Dispose();
        };

        UpdateLookupChip();

        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ShellViewModel.SelectedIndex))
                {
                    var listBox = this.FindControl<ListBox>("LeftListBox");
                    listBox?.ScrollIntoView(_viewModel.SelectedIndex);
                }
            };

            _viewModel.NavigationChanged += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var listBox = this.FindControl<ListBox>("LeftListBox");
                    listBox?.Focus();
                    if (_viewModel.SelectedIndex >= 0)
                        listBox?.ScrollIntoView(_viewModel.SelectedIndex);
                });
            };

            _viewModel.RequestLaunch += item => _ = LaunchSelectedGameAsync();
        }
    }
}
