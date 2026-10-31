using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App.ViewModels;

/// <summary>One row in the setup launcher preview/policy list.</summary>
public sealed class LauncherPolicyRow : ReactiveObject
{
    private readonly IStorePolicyProvider _policies;
    private readonly Func<GameSourceKind, bool> _hasReader;
    private bool _settingAllowsPolicy;
    private bool _indexEnabled;
    private bool _checkOnLaunch;
    private bool _enrichNewGames;

    public LauncherPolicyRow(
        LauncherDiscoveryResult result,
        IStorePolicyProvider policies,
        Func<GameSourceKind, bool> hasReader,
        bool settingAllowsPolicy = true)
    {
        Result = result;
        _policies = policies;
        _hasReader = hasReader;
        _settingAllowsPolicy = settingAllowsPolicy;
        StorePolicy policy = policies.Get(result.Kind);
        bool canIndex = CanIndex;
        _indexEnabled = canIndex && policy.IndexEnabled;
        _checkOnLaunch = canIndex && _indexEnabled && policy.CheckOnLaunch;
        _enrichNewGames = canIndex && _indexEnabled && policy.EnrichNewGames;
    }

    public LauncherDiscoveryResult Result { get; }

    public GameSourceKind Kind => Result.Kind;

    public string DisplayName => GameSourceParser.ToDisplayName(Kind);

    public string StatusText =>
        Result.Found ? "Found"
        : Result.Safety == LauncherReadOnlySafety.Unsupported ? "Unsupported"
        : "Not found";

    public string Detail =>
        Result.Found
            ? (Result.InventorySource ?? Result.InstallPath ?? Result.Reason)
            : Result.Reason;

    /// <summary>Indexing is possible only when the setting allows policies, a reader exists, and the source is not unsupported.</summary>
    public bool CanIndex => _settingAllowsPolicy && _hasReader(Kind) && Result.Found
        && Result.Safety != LauncherReadOnlySafety.Unsupported;

    public bool CanCheckOnLaunch => CanIndex && IndexEnabled;

    public bool CanEnrich => CanIndex && IndexEnabled;

    /// <summary>
    /// FileOnly (and Unset) make the row non-actionable and clear its checked state. The
    /// underlying policy rows stay, but nothing is left enabled when the setting returns.
    /// </summary>
    internal void SetSettingAllowed(bool allowed)
    {
        if (_settingAllowsPolicy == allowed)
            return;

        _settingAllowsPolicy = allowed;
        if (!allowed)
        {
            _indexEnabled = false;
            _checkOnLaunch = false;
            _enrichNewGames = false;
        }

        OnPropertyChanged(nameof(CanIndex));
        OnPropertyChanged(nameof(CanCheckOnLaunch));
        OnPropertyChanged(nameof(CanEnrich));
        OnPropertyChanged(nameof(IndexEnabled));
        OnPropertyChanged(nameof(CheckOnLaunch));
        OnPropertyChanged(nameof(EnrichNewGames));
    }

    public bool IndexEnabled
    {
        get => _indexEnabled;
        set
        {
            bool next = value && CanIndex;
            if (_indexEnabled == next)
                return;

            _indexEnabled = next;
            if (!next)
            {
                _checkOnLaunch = false;
                _enrichNewGames = false;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(CheckOnLaunch));
            OnPropertyChanged(nameof(EnrichNewGames));
            OnPropertyChanged(nameof(CanCheckOnLaunch));
            OnPropertyChanged(nameof(CanEnrich));
            Save();
        }
    }

    public bool CheckOnLaunch
    {
        get => _checkOnLaunch;
        set
        {
            bool next = value && CanCheckOnLaunch;
            if (_checkOnLaunch == next)
                return;
            _checkOnLaunch = next;
            OnPropertyChanged();
            Save();
        }
    }

    public bool EnrichNewGames
    {
        get => _enrichNewGames;
        set
        {
            bool next = value && CanEnrich;
            if (_enrichNewGames == next)
                return;
            _enrichNewGames = next;
            OnPropertyChanged();
            Save();
        }
    }

    private void Save()
    {
        if (!_settingAllowsPolicy)
            return;

        StorePolicy existing = _policies.Get(Kind);
        _policies.Set(existing with
        {
            IndexEnabled = _indexEnabled,
            CheckOnLaunch = _checkOnLaunch,
            EnrichNewGames = _enrichNewGames,
        });
    }
}
