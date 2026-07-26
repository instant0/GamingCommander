namespace GamingCommander.App.ViewModels;

/// <summary>
/// Represents a library root entry in the setup dialog.
/// </summary>
public sealed class LibraryRootEntry : GamingCommander.UI.ViewModels.ReactiveObject
{
    public LibraryRootEntry(string path, string defaultType, int gameCount)
    {
        Path = path;
        _defaultType = defaultType;
        DefaultType = defaultType;
        GameCount = gameCount;
    }

    /// <summary>Absolute path to the library root directory.</summary>
    public string Path { get; }

    /// <summary>Number of games discovered under this root.</summary>
    public int GameCount { get; set; }

    /// <summary>Default game source type for this root (e.g., "Steam", "Standalone").</summary>
    public string DefaultType
    {
        get => _defaultType;
        set => SetProperty(ref _defaultType, value);
    }
    private string _defaultType = string.Empty;

    /// <summary>True while this root is being scanned.</summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }
    private bool _isScanning;

    /// <summary>True if scanning has completed for this entry (at least once).</summary>
    public bool IsScanned
    {
        get => _isScanned;
        set => SetProperty(ref _isScanned, value);
    }
    private bool _isScanned;
}
