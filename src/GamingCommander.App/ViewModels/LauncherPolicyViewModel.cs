using System.Collections.ObjectModel;
using System.Threading.Tasks;
using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App.ViewModels;

/// <summary>
/// Launcher discovery choice, selected-launcher checklist, preview rows, and per-store policy
/// rows. It probes only when <see cref="Detect"/> is called; construction performs no I/O.
/// The setting lives in <see cref="LauncherDiscoveryContext"/> and is persisted through the
/// injected config service, not a second settings file.
/// </summary>
public sealed class LauncherPolicyViewModel : ReactiveObject
{
    private readonly LauncherDiscoveryService _discovery;
    private readonly IStorePolicyProvider _policies;
    private readonly Func<GameSourceKind, bool> _hasReader;
    private readonly LauncherDiscoveryContext _context;
    private readonly IConfigService? _configService;
    private string _statusText = "Click Detect launchers to probe installed launchers.";

    public LauncherPolicyViewModel(
        LauncherDiscoveryService discovery,
        IStorePolicyProvider policies,
        Func<GameSourceKind, bool>? hasReader = null,
        LauncherDiscoveryContext? context = null,
        IConfigService? configService = null)
    {
        _discovery = discovery;
        _policies = policies;
        _hasReader = hasReader ?? (_ => false);
        _context = context ?? new LauncherDiscoveryContext(LauncherDiscoverySetting.Unset);
        _configService = configService;

        SettingChoices =
        [
            new LauncherDiscoveryOption(
                LauncherDiscoverySetting.Automatic,
                "Automatic",
                "Detects every installed launcher on the actions you start; nothing is enabled automatically.",
                _context.Setting == LauncherDiscoverySetting.Automatic,
                ChooseSetting),
            new LauncherDiscoveryOption(
                LauncherDiscoverySetting.Selected,
                "Selected",
                "Detects only the launchers you check; nothing is enabled automatically.",
                _context.Setting == LauncherDiscoverySetting.Selected,
                ChooseSetting),
            new LauncherDiscoveryOption(
                LauncherDiscoverySetting.FileOnly,
                "FileOnly",
                "Reads no launcher registry or ProgramData; scans only the game folders you add. Existing games stay.",
                _context.Setting == LauncherDiscoverySetting.FileOnly,
                ChooseSetting),
        ];

        foreach (GameSourceKind kind in LauncherKinds.All)
        {
            if (_discovery.Supports(kind))
            {
                SelectionOptions.Add(new LauncherSelectionOption(
                    kind,
                    _context.SelectedLaunchers.Contains(kind),
                    SelectionChanged));
            }
            else
            {
                InformationalLaunchers.Add(new LauncherInfo(
                    GameSourceParser.ToDisplayName(kind),
                    "Unsupported — detection only; no locator in this build."));
            }
        }
    }

    public ObservableCollection<LauncherPolicyRow> Rows { get; } = [];

    /// <summary>The three launcher discovery settings and their one-line effects.</summary>
    public IReadOnlyList<LauncherDiscoveryOption> SettingChoices { get; }

    /// <summary>Launchers with a registered locator; only these can be selected.</summary>
    public ObservableCollection<LauncherSelectionOption> SelectionOptions { get; } = [];

    /// <summary>Launchers without a locator in this build, shown as information.</summary>
    public ObservableCollection<LauncherInfo> InformationalLaunchers { get; } = [];

    /// <summary>The current Setup choice. Setting it persists immediately and never probes.</summary>
    public LauncherDiscoverySetting Setting
    {
        get => _context.Setting;
        set => ChooseSetting(value);
    }

    /// <summary>True when the checklist applies.</summary>
    public bool IsSelectedSetting => Setting == LauncherDiscoverySetting.Selected;

    /// <summary>True when discovery and store controls are actionable (Automatic/Selected).</summary>
    public bool ShowsLauncherSections =>
        Setting is LauncherDiscoverySetting.Automatic or LauncherDiscoverySetting.Selected;

    /// <summary>A snapshot of the current policy; consumers gate their reads on it.</summary>
    public LauncherDiscoveryPolicy Policy => _context.Policy;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Raised after a setting change so the window can run one explicit discovery. Setup is the
    /// only caller: the view-model itself stays synchronous and performs no I/O here.
    /// </summary>
    public event Action? SettingChosen;

    /// <summary>Runs the locators allowed by the current policy once and rebuilds the rows.</summary>
    public void Detect()
    {
        if (!Policy.AllowsStorePolicies)
        {
            StatusText = "Launcher discovery is off — file and folder signals only.";
            return;
        }

        if (Setting == LauncherDiscoverySetting.Selected && Policy.SelectedLaunchers.Count == 0)
        {
            Rows.Clear();
            StatusText = "No launchers selected. Check a launcher first.";
            return;
        }

        Apply(_discovery.DiscoverForPolicy(Policy));
    }

    /// <summary>UI wrapper: probing runs off the UI thread; rows are applied on the caller thread.</summary>
    public async Task DetectAsync()
    {
        if (!Policy.AllowsStorePolicies)
        {
            Detect();
            return;
        }

        if (Setting == LauncherDiscoverySetting.Selected && Policy.SelectedLaunchers.Count == 0)
        {
            Detect();
            return;
        }

        LauncherDiscoveryPolicy policy = Policy;
        IReadOnlyList<LauncherDiscoveryResult> results =
            await Task.Run(() => _discovery.DiscoverForPolicy(policy));
        Apply(results);
    }

    /// <summary>Applies the setting: context and settings.json first, then the UI state.</summary>
    private void ChooseSetting(LauncherDiscoverySetting setting)
    {
        if (!Enum.IsDefined(setting) || setting == LauncherDiscoverySetting.Unset)
            return;

        if (setting == _context.Setting)
        {
            SyncCheckState();
            return;
        }

        // The selection set is independent of the setting and is not cleared on a change.
        // FileOnly disables the store policies (rows stay); returning never re-enables them.
        if (setting == LauncherDiscoverySetting.FileOnly)
            DisableStorePolicies();

        _context.Update(setting, _context.SelectedLaunchers);
        Persist();
        SyncCheckState();
        RaiseSettingProperties();
        UpdateRowGate();
        SettingChosen?.Invoke();
    }

    private void SelectionChanged(GameSourceKind kind, bool selected)
    {
        List<GameSourceKind> next = _context.SelectedLaunchers.ToList();
        if (selected)
        {
            if (!next.Contains(kind))
                next.Add(kind);
        }
        else
        {
            next.Remove(kind);
        }

        _context.Update(_context.Setting, next);
        Persist();
        OnPropertyChanged(nameof(Policy));
    }

    private void Persist()
    {
        if (_configService is null)
            return;

        AppConfig config = _configService.Load();
        _configService.Save(config with
        {
            LauncherDiscovery = _context.Setting,
            SelectedLaunchers = _context.SelectedLaunchers,
        });
    }

    /// <summary>FileOnly disables every store policy but keeps its row/override.</summary>
    private void DisableStorePolicies()
    {
        foreach (GameSourceKind kind in LauncherKinds.All)
        {
            StorePolicy existing = _policies.Get(kind);
            if (!existing.IndexEnabled && !existing.CheckOnLaunch && !existing.EnrichNewGames)
                continue;
            _policies.Set(existing with
            {
                IndexEnabled = false,
                CheckOnLaunch = false,
                EnrichNewGames = false,
            });
        }
    }

    private void SyncCheckState()
    {
        foreach (LauncherDiscoveryOption option in SettingChoices)
            option.SetChecked(option.Setting == Setting);

        foreach (LauncherSelectionOption option in SelectionOptions)
            option.SetSelected(_context.SelectedLaunchers.Contains(option.Kind));
    }

    private void RaiseSettingProperties()
    {
        OnPropertyChanged(nameof(Setting));
        OnPropertyChanged(nameof(IsSelectedSetting));
        OnPropertyChanged(nameof(ShowsLauncherSections));
        OnPropertyChanged(nameof(Policy));
    }

    private void UpdateRowGate()
    {
        bool allowed = Policy.AllowsStorePolicies;
        foreach (LauncherPolicyRow row in Rows)
            row.SetSettingAllowed(allowed);
    }

    private void Apply(IReadOnlyList<LauncherDiscoveryResult> results)
    {
        bool allowed = Policy.AllowsStorePolicies;
        Rows.Clear();
        foreach (LauncherDiscoveryResult result in results)
            Rows.Add(new LauncherPolicyRow(result, _policies, _hasReader, allowed));

        int found = results.Count(r => r.Found);
        StatusText = found == 0
            ? "No launchers found."
            : $"{found} launcher{(found == 1 ? "" : "s")} found.";
    }
}
