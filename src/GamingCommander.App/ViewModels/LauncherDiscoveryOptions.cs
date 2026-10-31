using GamingCommander.Core.Models;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App.ViewModels;

/// <summary>One radio choice for the launcher discovery setting, with its one-line effect.</summary>
public sealed class LauncherDiscoveryOption : ReactiveObject
{
    private readonly Action<LauncherDiscoverySetting> _choose;
    private bool _isChecked;

    internal LauncherDiscoveryOption(
        LauncherDiscoverySetting setting,
        string title,
        string effect,
        bool isChecked,
        Action<LauncherDiscoverySetting> choose)
    {
        Setting = setting;
        Title = title;
        Effect = effect;
        _isChecked = isChecked;
        _choose = choose;
    }

    public LauncherDiscoverySetting Setting { get; }

    public string Title { get; }

    /// <summary>One line describing what this setting reads.</summary>
    public string Effect { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
                return;

            _isChecked = value;
            OnPropertyChanged();
            if (value)
                _choose(Setting);
        }
    }

    /// <summary>Sets the displayed state without invoking the choice callback.</summary>
    internal void SetChecked(bool value)
    {
        if (_isChecked == value)
            return;

        _isChecked = value;
        OnPropertyChanged(nameof(IsChecked));
    }
}

/// <summary>One checkable launcher in the Selected setting checklist.</summary>
public sealed class LauncherSelectionOption : ReactiveObject
{
    private readonly Action<GameSourceKind, bool> _changed;
    private bool _isSelected;

    internal LauncherSelectionOption(
        GameSourceKind kind,
        bool isSelected,
        Action<GameSourceKind, bool> changed)
    {
        Kind = kind;
        _isSelected = isSelected;
        _changed = changed;
    }

    public GameSourceKind Kind { get; }

    public string DisplayName => GameSourceParser.ToDisplayName(Kind);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
            _changed(Kind, value);
        }
    }

    /// <summary>Sets the displayed state without persisting it.</summary>
    internal void SetSelected(bool value)
    {
        if (_isSelected == value)
            return;

        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));
    }
}

/// <summary>A launcher without a locator in this build, shown as information, not a checkbox.</summary>
public sealed record LauncherInfo(string DisplayName, string StatusText)
{
    public string Summary => $"{DisplayName}: {StatusText}";
}
