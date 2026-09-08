using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Type-to-search half of <see cref="ShellViewModel"/> (Plan 122, Plan 125 final
/// stage): the typed-character buffer, live wildcard filtering, and search teardown.
/// </summary>
public sealed partial class ShellViewModel
{
    private string _searchBuffer = string.Empty;

    /// <summary>True while a type-to-search buffer is active (Plan 122).</summary>
    public bool IsSearching => _searchBuffer.Length > 0;

    /// <summary>Current typed search text.</summary>
    public string SearchText => _searchBuffer;

    /// <summary>
    /// Appends a typed character to the live-search buffer. From the threshold up,
    /// applies a cross-root wildcard filter (names + tags) on every keystroke.
    /// </summary>
    public void AppendSearchChar(char c)
    {
        SetSearchBuffer(_searchBuffer + c);
        if (_searchBuffer.Length >= SearchThreshold)
            ApplyFilter(new GameFilter(GameFilterKind.Wildcard, _searchBuffer));
        else
            StatusText = "Keep typing to search…";
    }

    /// <summary>
    /// Removes the last typed character. Re-filters while at/above the threshold;
    /// dropping below it ends the search and returns to library roots.
    /// Returns false when there is nothing to erase.
    /// </summary>
    public bool SearchBackspace()
    {
        if (_searchBuffer.Length == 0) return false;

        string remaining = _searchBuffer[..^1];
        if (remaining.Length >= SearchThreshold)
        {
            SetSearchBuffer(remaining);
            ApplyFilter(new GameFilter(GameFilterKind.Wildcard, remaining));
        }
        else
        {
            EndSearch();
        }
        return true;
    }

    /// <summary>
    /// Cancels an active search: clears buffer and filter, back to library roots.
    /// Returns false when no search is in progress (caller should navigate up).
    /// </summary>
    public bool CancelSearch()
    {
        if (_searchBuffer.Length == 0) return false;
        EndSearch();
        return true;
    }

    private void EndSearch()
    {
        SetSearchBuffer(string.Empty);
        JumpToLibraryRoots();
        StatusText = string.Empty;
    }

    private void SetSearchBuffer(string value)
    {
        _searchBuffer = value;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(IsSearching));
        OnPropertyChanged(nameof(LeftPaneTitle));
    }
}