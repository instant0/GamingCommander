using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Base class for ViewModels implementing INotifyPropertyChanged with a SetProperty helper.
/// </summary>
public abstract class ReactiveObject : INotifyPropertyChanged
{
    /// <summary>Raised when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises the PropertyChanged event for the specified property.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field and raises PropertyChanged if the value changed.
    /// Returns true if the value was updated.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
