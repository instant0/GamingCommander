namespace GamingCommander.App.ViewModels;

/// <summary>
/// Represents a library folder entry in the first-run wizard.
/// </summary>
public sealed class WizardLibraryEntry : GamingCommander.UI.ViewModels.ReactiveObject
{
    public WizardLibraryEntry(string path, string selectedType)
    {
        Path = path;
        _selectedType = selectedType;
        SelectedType = selectedType;
    }

    /// <summary>Absolute path to the library folder.</summary>
    public string Path { get; }

    /// <summary>Selected game source type (e.g., "Steam", "Standalone").</summary>
    public string SelectedType
    {
        get => _selectedType;
        set => SetProperty(ref _selectedType, value);
    }
    private string _selectedType = string.Empty;

    /// <summary>Number of games found during scanning.</summary>
    public int GameCount
    {
        get => _gameCount;
        set => SetProperty(ref _gameCount, value);
    }
    private int _gameCount;

    /// <summary>True if scanning has completed for this entry.</summary>
    public bool IsScanned
    {
        get => _isScanned;
        set => SetProperty(ref _isScanned, value);
    }
    private bool _isScanned;

    /// <summary>True if a scan is currently in progress for this entry.</summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }
    private bool _isScanning;
}
