namespace GamingCommander.App.ViewModels;

/// <summary>
/// Represents a library (anchor) entry in the setup dialog.
/// <see cref="Name"/> holds the anchor name (a physical path for Standalone
/// libraries, or a display name such as "Steam" / "Epic Games Store" otherwise).
/// </summary>
public sealed class LibraryEntry : GamingCommander.UI.ViewModels.ReactiveObject
{
    public LibraryEntry(string name, string defaultType, int gameCount)
    {
        Name = name;
        _defaultType = defaultType;
        DefaultType = defaultType;
        GameCount = gameCount;
    }

    /// <summary>Anchor name (path for Standalone libraries, display name for platform anchors).</summary>
    public string Name { get; }

    /// <summary>Number of physical folders owned by this anchor (folder count).</summary>
    public int FolderCount { get; set; } = 1;

    /// <summary>Number of games discovered under this library.</summary>
    public int GameCount { get; set; }

    /// <summary>Default game source type for this library (e.g., "Steam", "Standalone").</summary>
    public string DefaultType
    {
        get => _defaultType;
        set => SetProperty(ref _defaultType, value);
    }
    private string _defaultType = string.Empty;

    /// <summary>True while this library is being scanned.</summary>
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