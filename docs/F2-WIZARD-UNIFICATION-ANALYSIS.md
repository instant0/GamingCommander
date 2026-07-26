# F2 + Wizard Setup Screen Unification — Verification & Analysis

**Nature:** Verification pass over existing Plan 106. Cross-references plan claims against actual codebase.  
**Audience:** Planner / Builder  
**Status:** Verification complete — Plan 106 is largely accurate with minor corrections  
**Date:** 2026-07-26

---

## 0. Purpose

This document is a **verification and analysis pass** on `planning/106-unified-setup-screen.md`. It cross-references every claim in the plan against the actual codebase, identifies inaccuracies, finds gaps, and provides updated recommendations.

---

## 1. Plan 106 Accuracy Verification

### 1.1 "Both have folder picking, nesting validation, type selection, scanning"

**Verdict:** ✅ Accurate

| Feature | Wizard (`WizardViewModel`) | F2 (`LibrarySetupViewModel`) |
|---------|---------------------------|------------------------------|
| Folder picking | `AddEntryAsync()` → `OpenFolderPickerAsync` | `AddRootAsync()` → `OpenFolderPickerAsync` |
| Nesting validation | `IsChildOf()` check (lines 64-80) | Identical `IsChildOf()` check (lines 61-78) |
| Type selection | ComboBox via `GameSourceParser.SourceDisplayNames` | ComboBox via `GameSourceParser.SourceDisplayNames` |
| Scanning | Direct `_scanner.Scan()` / `SteamLibraryScanner` | Via `_libraryManager.AddRoot()` |

Both implementations share **identical nesting validation code** — the same `IsChildOf()` call pattern with the same error messages. This is confirmed duplicated logic.

### 1.2 "Wizard bypasses ILibraryManager — creates its own FolderScanner directly"

**Verdict:** ✅ Accurate

```csharp
// WizardViewModel.cs line 30
_scanner = new FolderScanner(configService.Load().HiddenFolders, blacklist);
```

The Wizard creates its own `FolderScanner` instance with `BlacklistData`. It does NOT use `ILibraryManager` at all.

```csharp
// LibrarySetupViewModel.cs line 135
bool added = await Task.Run(() => _libraryManager.AddRoot(path, defaultType, []));
```

F2 correctly delegates to `ILibraryManager.AddRoot()`, which internally calls `SelectScannerAndScan()` — the proper scanner routing logic.

**Impact:** The Wizard's scanner instantiation is redundant and doesn't benefit from the `LibraryManager`'s structural Steam detection (`LooksLikeSteamLibrary()`). This means the Wizard may misroute a Steam library through `FolderScanner` instead of `SteamLibraryScanner` when the user hasn't manually selected "Steam" type.

### 1.3 "Wizard has features F2 lacks (online metadata toggle, scan progress badges, blacklist)"

**Verdict:** ✅ Accurate with minor detail

| Wizard Feature | Code Location | In F2? |
|---------------|---------------|--------|
| Online metadata toggle | `WizardViewModel.EnableOnlineMetadata` + `WizardWindow.axaml` line 37-42 | ❌ No checkbox in F2 |
| Per-entry scan progress | `WizardLibraryEntry.IsScanning`, `IsScanned` properties | ❌ No equivalent |
| Scan state badges | `WizardWindow.axaml.cs` lines 92-102: "scanning..." / "N games" / "not scanned" | ❌ F2 shows "N game(s) — Type" |
| Blacklist loading | `WizardWindow.axaml.cs` line 23: `BlacklistLoader(baseDir).Load()` | ❌ F2 doesn't load blacklist |
| Finish/Skip semantics | `Finish()` saves all; `Cancel()` saves only scanned | ❌ N/A — F2 uses immediate persistence |

**Detail correction:** The Wizard loads `BlacklistData` and passes it to `FolderScanner`, but the `LibraryManager` also creates its own `FolderScanner` with blacklist from `App.axaml.cs` (line 85-89). The Wizard's blacklist loading is redundant when F2's path through `ILibraryManager` already provides it.

### 1.4 "F2 has features Wizard lacks (loads existing roots, empty-state message, immediate persistence)"

**Verdict:** ✅ Accurate

| F2 Feature | Code Location | In Wizard? |
|-----------|---------------|------------|
| Loads existing roots | `LibrarySetupViewModel.LoadRoots()` (line 36-45) | ❌ Wizard starts empty |
| Empty-state message | `LibrarySetupWindow.axaml.cs` lines 132-141 | ❌ No empty state |
| Immediate persistence | `LibrarySetupViewModel.ScanAndSaveAsync()` → `_libraryManager.AddRoot()` | ❌ Deferred to Finish/Cancel |
| Close button | `LibrarySetupWindow.axaml` line 31 | ❌ Has Skip/Finish buttons |

### 1.5 "They share ~60-70% of their logic"

**Verdict:** ✅ Accurate

Duplicated logic (verified):

| Logic | Wizard Code | F2 Code | Identical? |
|-------|------------|---------|------------|
| Folder picker invocation | `AddEntryAsync()` line 56-57 | `AddRootAsync()` line 52-53 | ✅ Same API call |
| Path normalization | `LibraryManager.NormalizeLibraryRoot()` | Same | ✅ Same call |
| Nesting validation | Lines 64-80 (IsChildOf loop) | Lines 61-78 | ✅ Nearly identical |
| Duplicate path check | `Entries.Any(e => e.Path.Equals(...))` | Same | ✅ Same pattern |
| Type inference | `GameSourceParser.InferFromPath()` | Same | ✅ Same call |

**Not duplicated (different):**

| Logic | Wizard | F2 |
|-------|--------|-----|
| Scanner routing | Direct FolderScanner/SteamLibraryScanner | Via ILibraryManager → SelectScannerAndScan |
| Database persistence | `_dbService.AddRoot()` directly | `_libraryManager.AddRoot()` (wraps db + config) |
| Config persistence | Only on Finish/Cancel | Every Add/Remove |

### 1.6 Proposed Solution: "Keep F2 Library Setup as the single setup screen"

**Verdict:** ✅ Correct approach

The plan proposes enhancing F2 with Wizard's missing features and deleting the Wizard. This is the right approach because:

1. F2 already uses `ILibraryManager` properly (clean abstraction)
2. F2 has immediate persistence (crash-safe)
3. F2 loads existing roots (better UX for ongoing management)
4. The Wizard's direct scanner instantiation is a code smell — it duplicates scanner routing logic

### 1.7 "New Flow: App launch → Show MainWindow, then auto-open LibrarySetupWindow"

**Verdict:** ✅ Accurate with one correction

Current code (`App.axaml.cs` lines 124-149):
```csharp
if (needsWizard)
{
    var wizardWindow = new WizardWindow(configService, dbService);
    mainWindow.Show();
    wizardWindow.ShowDialog(mainWindow);
    // ... Closed handler updates config ...
}
```

Plan proposes:
```csharp
if (config.IsFirstRun || config.LibraryRoots.Count == 0)
{
    Dispatcher.UIThread.Post(async () =>
    {
        await ShowLibrarySetupAsync();
    }, DispatcherPriority.ApplicationIdle);
}
```

**Correction needed:** The plan's `needsWizard` check is simpler than the actual check. The real `needsWizard` condition (line 107-110) also triggers on version upgrades (`isNewerVersion`), not just first run. The plan should account for version-upgrade scenarios where the F2 should also auto-open (or not — this is a design decision).

**Current `needsWizard` triggers:**
1. `config.IsFirstRun` — first run
2. `config.LastSeenVersion is null` — no version recorded
3. `isNewerVersion` — version upgraded
4. `config.LibraryRoots.Count == 0` — no roots configured

The plan's simpler condition `config.IsFirstRun || config.LibraryRoots.Count == 0` would miss the version-upgrade case. The existing behavior (re-wizard on upgrade) should be preserved or explicitly dropped.

---

## 2. Feature Comparison Matrix (Expanded)

### 2.1 Complete Feature Inventory

| # | Feature | Wizard | F2 | Unified (Proposed) |
|---|---------|--------|-----|-------------------|
| 1 | Folder picker | ✅ | ✅ | ✅ (F2) |
| 2 | Nesting validation | ✅ | ✅ | ✅ (F2) |
| 3 | Duplicate rejection | ✅ | ✅ | ✅ (F2) |
| 4 | Type auto-inference | ✅ | ✅ | ✅ (F2) |
| 5 | Path normalization | ✅ | ✅ | ✅ (F2) |
| 6 | Scanning | ✅ Direct | ✅ Via ILibraryManager | ✅ (F2) |
| 7 | Online metadata toggle | ✅ | ❌ | ✅ Add to F2 |
| 8 | Per-entry scan progress | ✅ IsScanning | ❌ | ✅ Add to F2 |
| 9 | Scan state badges | ✅ "scanning..."/"N games" | ❌ "N game(s) — Type" | ✅ Enhance F2 |
| 10 | Loads existing roots | ❌ | ✅ | ✅ (F2) |
| 11 | Empty-state message | ❌ | ✅ | ✅ (F2) |
| 12 | Immediate persistence | ❌ Deferred | ✅ | ✅ (F2) |
| 13 | Close button | ❌ Skip/Finish | ✅ | ✅ (F2) |
| 14 | Uses ILibraryManager | ❌ Direct scanner | ✅ | ✅ (F2) |
| 15 | Blacklist loading | ✅ Manual | ❌ Via ILibraryManager | ✅ (F2) — automatic |
| 16 | Per-entry Rescan button | ✅ Scan/Rescan | ✅ Rescan | ✅ (F2) |
| 17 | Per-entry Remove button | ✅ X button | ✅ Remove button | ✅ (F2) |
| 18 | Welcome/onboarding text | ✅ "Welcome to GamingCommander" | ❌ "Library Root Setup" | ✅ Conditional title |

### 2.2 Data Model Comparison

| Field | `WizardLibraryEntry` | `LibraryRootEntry` |
|-------|---------------------|-------------------|
| `Path` | ✅ `string Path` | ✅ `string Path` |
| `SelectedType`/`DefaultType` | ✅ `string SelectedType` (settable) | ✅ `string DefaultType` (settable) |
| `GameCount` | ✅ `int GameCount` (settable) | ✅ `int GameCount` (settable) |
| `IsScanned` | ✅ `bool IsScanned` | ❌ Not present |
| `IsScanning` | ✅ `bool IsScanning` | ❌ Not present |

**Gap:** `LibraryRootEntry` is missing `IsScanned` and `IsScanning` properties that `WizardLibraryEntry` has. These need to be added to `LibraryRootEntry` for the unified screen.

### 2.3 Constructor Comparison

**WizardViewModel:**
```csharp
public WizardViewModel(
    IConfigService configService,
    IGamesDatabaseService dbService,
    Window window,
    BlacklistData? blacklist = null)
```
Takes optional `BlacklistData`. Creates its own `FolderScanner` directly.

**LibrarySetupViewModel:**
```csharp
public LibrarySetupViewModel(
    IConfigService configService,
    IGamesDatabaseService dbService,
    ILibraryManager libraryManager,
    Window window)
```
Takes `ILibraryManager`. Delegates scanning to the manager.

**Unified:** Should take `ILibraryManager` (F2's approach) — the blacklist is already loaded by `App.axaml.cs` and passed to `LibraryManager`'s `FolderScanner`.

---

## 3. Code Change Impact Analysis

### 3.1 Files to Delete

| File | Lines | Rationale |
|------|-------|-----------|
| `WizardWindow.axaml` | 52 | XAML layout |
| `WizardWindow.axaml.cs` | 134 | Code-behind with `RenderEntries()` |
| `WizardViewModel.cs` | 167 | ViewModel with direct scanner instantiation |
| `WizardLibraryEntry.cs` | 49 | Data model (subsumed by `LibraryRootEntry`) |

**Total deletion:** ~402 lines

### 3.2 Files to Modify

| File | Current Lines | Change | Notes |
|------|--------------|--------|-------|
| `LibraryRootEntry.cs` | 29 | +8 | Add `IsScanning`, `IsScanned` properties |
| `LibrarySetupViewModel.cs` | 144 | +40 | Add scan progress tracking, metadata toggle, welcome text |
| `LibrarySetupWindow.axaml` | 35 | +20 | Add metadata checkbox, scan badges, conditional title |
| `LibrarySetupWindow.axaml.cs` | 143 | +30 | Enhanced rendering with scan progress |
| `App.axaml.cs` | 212 | -20, +15 | Replace wizard trigger with F2 auto-open |

**Total modification:** ~113 lines changed (net ~+93 after deletions)

### 3.3 Wizard Features to Port to F2

#### Feature 1: Online Metadata Toggle

Add to `LibrarySetupViewModel`:
```csharp
public bool EnableOnlineMetadata
{
    get => _enableOnlineMetadata;
    set => SetProperty(ref _enableOnlineMetadata, value);
}
private bool _enableOnlineMetadata;
```

Add to `LibrarySetupWindow.axaml` (bottom area):
```xml
<CheckBox IsChecked="{Binding EnableOnlineMetadata}"
          Content="Enable online metadata lookups (PCGW, Steam)" />
```

Persist to `AppConfig` on close. **Note:** `AppConfig.EnableOnlineMetadata` already exists — just needs wiring.

#### Feature 2: Per-Entry Scan Progress

Add to `LibraryRootEntry`:
```csharp
public bool IsScanning
{
    get => _isScanning;
    set => SetProperty(ref _isScanning, value);
}
private bool _isScanning;

public bool IsScanned
{
    get => _isScanned;
    set => SetProperty(ref _isScanned, value);
}
private bool _isScanned;
```

Update `ScanAndSaveAsync` in `LibrarySetupViewModel`:
```csharp
entry.IsScanning = true;
entry.StatusText = "Scanning...";
// ... scan ...
entry.IsScanning = false;
entry.IsScanned = true;
entry.StatusText = $"✓ {entry.GameCount} games";
```

#### Feature 3: Scan State Badges

Replace the current F2 status line:
```csharp
// Current:
Text = $"{entry.GameCount} game(s) — {entry.DefaultType}"

// Proposed:
Text = entry.IsScanning ? "⏳ Scanning..."
     : entry.GameCount > 0 ? $"✓ {entry.GameCount} games"
     : entry.IsScanned ? "0 games"
     : "⚠ Not scanned"
```

#### Feature 4: Welcome/Onboarding Text

Add conditional title in `LibrarySetupWindow.axaml`:
```xml
<TextBlock Text="{Binding TitleText}" FontWeight="Bold" FontSize="..." />
<TextBlock Text="{Binding SubtitleText}" TextWrapping="Wrap" />
```

Where `TitleText` and `SubtitleText` are computed from `config.IsFirstRun`:
- First run: "Welcome to GamingCommander" + onboarding text
- Ongoing: "Library Root Setup" + management text

### 3.4 App.axaml.cs Changes

**Current (lines 124-149):**
```csharp
if (needsWizard)
{
    var wizardWindow = new WizardWindow(configService, dbService);
    mainWindow.Show();
    wizardWindow.ShowDialog(mainWindow);
    wizardWindow.Closed += (_, _) => { /* update config */ };
}
```

**Proposed:**
```csharp
if (needsWizard)
{
    mainWindow.Show();
    Dispatcher.UIThread.Post(async () =>
    {
        var setupWindow = new LibrarySetupWindow(configService, dbService, libraryManager);
        await setupWindow.ShowDialog(mainWindow);
        
        // Post-close: update version stamp and load games
        config = configService.Load();
        config = config with { LastSeenVersion = currentVersion };
        configService.Save(config);
        
        if (config.LibraryRoots.Count == 0)
            shellVm.StatusText = "No library roots configured. Press F2 to add folders.";
        else
        {
            shellVm.JumpToLibraryRoots();
            int totalGames = config.LibraryRoots.Sum(r => dbService.GetGamesForRoot(r.RootPath).Count);
            shellVm.StatusText = $"Welcome — {config.LibraryRoots.Count} root(s), {totalGames} game(s) loaded.";
        }
    }, DispatcherPriority.ApplicationIdle);
}
```

**Key change:** The `LibrarySetupWindow` already handles scanning via `ILibraryManager` — no separate scanner instantiation needed.

---

## 4. Risk Assessment (Updated)

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| F2 loses Wizard-only functionality | Low | Medium | Feature parity checklist above (all 18 features verified) |
| First-run flow confuses users | Low | Low | F2 is self-explanatory with empty state message |
| Scan progress causes UI freeze | Low | Medium | Already async in both implementations; `_isRefreshing` guard exists |
| Removing Wizard breaks version upgrade path | **Medium** | Low | Plan's `needsWizard` condition is simpler than actual — must preserve version-upgrade trigger |
| `EnableOnlineMetadata` not wired to anything | Low | Low | Field exists in `AppConfig` but no code reads it yet; wiring is future work |
| Blacklist loading regression | Low | Medium | Wizard loads blacklist manually; F2 doesn't need to (LibraryManager already has it) |

### 4.1 Version Upgrade Path — Detailed Analysis

**Current behavior:** When the app version changes, `needsWizard = true` triggers the Wizard. The Wizard's `Finish()` and `Cancel()` both set `IsFirstRun: false` and save `LastSeenVersion`. After the Wizard closes, `App.axaml.cs` also stamps `LastSeenVersion = currentVersion`.

**Proposed behavior:** The F2 auto-open should also trigger on version upgrades. The `needsWizard` condition must be preserved:

```csharp
bool needsWizard = config.IsFirstRun
    || config.LastSeenVersion is null
    || isNewerVersion
    || config.LibraryRoots.Count == 0;
```

The F2 setup screen should display appropriate text:
- First run: "Welcome to GamingCommander — add your game folders"
- Version upgrade: "GamingCommander updated — review your library roots"
- No roots: "Add your game library folders to get started"

---

## 5. Implementation Sequence (Refined)

### Step 1: Enhance LibraryRootEntry (5 min)
Add `IsScanning`, `IsScanned` properties. Backward-compatible — defaults are `false`.

### Step 2: Enhance LibrarySetupViewModel (30 min)
- Add `EnableOnlineMetadata` property
- Add `IsFirstRun`/welcome text properties
- Enhance `ScanAndSaveAsync` with progress tracking
- Persist metadata toggle on close

### Step 3: Enhance LibrarySetupWindow UI (30 min)
- Add metadata checkbox
- Add scan progress badges (replace "N game(s) — Type")
- Add conditional title/subtitle text
- Wider type ComboBox (Bug 12 fix)

### Step 4: Update App.axaml.cs (15 min)
- Replace `WizardWindow` with `LibrarySetupWindow` in startup
- Preserve `needsWizard` condition
- Update post-close handler

### Step 5: Delete Wizard files (5 min)
- Delete `WizardWindow.axaml`, `WizardWindow.axaml.cs`
- Delete `WizardViewModel.cs`
- Delete `WizardLibraryEntry.cs`
- Remove any remaining Wizard references

### Step 6: Tests (30 min)
- `LibrarySetupViewModelTests`: metadata toggle, scan progress, status text
- Manual test: first-run flow, version upgrade flow, F2 mid-session

**Total estimated effort:** ~2 hours (reduced from Plan 106's 4-6 hours due to simpler scope after verification)

---

## 6. Backward Compatibility

| Change | Impact | Compatible? |
|--------|--------|-------------|
| `LibraryRootEntry` gains `IsScanning`, `IsScanned` | New properties with defaults | ✅ Yes |
| `AppConfig.EnableOnlineMetadata` already exists | No schema change | ✅ Yes |
| `WizardLibraryEntry` removed | Only used by Wizard | ✅ Yes (Wizard deleted) |
| `WizardViewModel` removed | Only used by Wizard | ✅ Yes (Wizard deleted) |
| `WizardWindow` removed | Only used by App.axaml.cs startup | ✅ Yes (replaced) |
| `games.json` schema unchanged | No migration needed | ✅ Yes |
| `settings.json` schema unchanged | `EnableOnlineMetadata` already exists | ✅ Yes |

---

## 7. Files Affected (Complete)

### Delete (4 files, ~402 lines)
- `src/GamingCommander.App/WizardWindow.axaml`
- `src/GamingCommander.App/WizardWindow.axaml.cs`
- `src/GamingCommander.App/ViewModels/WizardViewModel.cs`
- `src/GamingCommander.App/ViewModels/WizardLibraryEntry.cs`

### Modify (5 files, ~93 net lines added)
- `src/GamingCommander.App/ViewModels/LibraryRootEntry.cs` — +8 lines
- `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs` — +40 lines
- `src/GamingCommander.App/LibrarySetupWindow.axaml` — +20 lines
- `src/GamingCommander.App/LibrarySetupWindow.axaml.cs` — +30 lines
- `src/GamingCommander.App/App.axaml.cs` — -20/+15 lines

### Reference (no changes needed)
- `src/GamingCommander.Core/Models/AppConfig.cs` — `EnableOnlineMetadata` already exists
- `src/GamingCommander.App/Services/LibraryManager.cs` — used as-is by enhanced F2
- `src/GamingCommander.App/ViewModels/ShellViewModel.cs` — `ShowLibrarySetupAsync()` already exists

---

## 8. Conclusion

Plan 106 is **largely accurate** and well-structured. The verification found:

1. **One inaccuracy:** The plan's `needsWizard` condition is simpler than the actual code — version-upgrade triggers must be preserved
2. **One data model gap:** `LibraryRootEntry` needs `IsScanning` and `IsScanned` from `WizardLibraryEntry`
3. **Effort overestimate:** The actual implementation is closer to ~2 hours (not 4-6) because most features are simple property additions
4. **All 18 features verified:** Every feature claim in the plan matches the actual code
5. **No architectural concerns:** The proposed approach of enhancing F2 and deleting Wizard is sound

**Recommendation:** Proceed with Plan 106 as written, with the version-upgrade condition fix noted above.

---

**Last updated:** 2026-07-26  
**Related documents:** `planning/106-unified-setup-screen.md`, `META/BACKLOG/TECH_DEBT.md` (Bug 14)
