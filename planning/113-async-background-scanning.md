# Plan 113: Async Background Scanning

**Created:** 2026-07-26
**Priority:** P1
**Status:** COMPLETE
**Defers from:** Plan 112 Step 5 (deferred items)

---

## 1. Problem Statement

F5 rescan blocks the UI thread. For a library with hundreds of games across multiple roots, the window freezes for seconds (sometimes 10+ seconds for large roots). The user cannot navigate, cancel, or see progress. This is the single biggest UX gap remaining.

Current behavior:
- F5 at root level: `_libraryManager.Refresh()` loops ALL roots synchronously on UI thread
- F5 inside a root: `_libraryManager.SelectScannerAndScan()` runs synchronously on UI thread
- No cancellation support in the scan pipeline
- No per-root progress indicator
- No ability to navigate away during scan

---

## 2. Goals

| Goal | Description |
|------|-------------|
| **Non-blocking UI** | F5 triggers scan on background thread; UI remains responsive |
| **Per-root badge** | Left pane shows "⏳ Scanning..." on the root being scanned |
| **Navigation during scan** | User can F9 to roots, drill into other roots, or F2 setup while scan runs |
| **Status bar progress** | Status bar shows current folder being scanned (updated per-root) |
| **Cancellation** | Pressing F5 again during scan cancels the in-progress scan |
| **Thread safety** | `ObservableCollection` updates marshaled to UI thread; no data races |

---

## 3. Non-Goals

- Incremental/streaming game results (partial lists during scan) — future enhancement
- Parallel multi-root scanning — sequential is simpler, sufficient for now
- Progress bars or percentage indicators — badge + status text is enough

---

## 4. Architecture Changes

### 4.1 Add CancellationToken to Scan Pipeline

**Files:**
- `ILibraryManager.cs` — add `CancellationToken` to `Refresh()`, `SelectScannerAndScan()`
- `LibraryManager.cs` — pass token through, check `token.ThrowIfCancellationRequested()` per root
- `FolderScanner.cs` — add `CancellationToken` parameter to `Scan()`, check per subdirectory
- `SteamLibraryScanner.cs` — add `CancellationToken` parameter to `Scan()`, `ScanAll()`

**Pattern:**
```csharp
// ILibraryManager.cs
IReadOnlyList<GameEntry> SelectScannerAndScan(
    string rootPath, GameSourceKind defaultType,
    CancellationToken ct = default);

void Refresh(CancellationToken ct = default);
```

**Cancellation points:**
- `LibraryManager.Refresh()`: check `ct.ThrowIfCancellationRequested()` at start of each root iteration
- `FolderScanner.Scan()`: check `ct.ThrowIfCancellationRequested()` in the `foreach (DirectoryInfo subDir)` loop
- `SteamLibraryScanner.Scan()`: check `ct.ThrowIfCancellationRequested()` in the common/ scan loop
- `ContainerScanner`: no change needed (called per-directory, short-lived)

### 4.2 Scan State on ShellViewModel

**File:** `ShellViewModel.cs`

Add properties:
```csharp
/// <summary>True when a scan is in progress on any root.</summary>
public bool IsScanning
{
    get => _isScanning;
    private set => SetProperty(ref _isScanning, value);
}
private bool _isScanning;

/// <summary>Root path currently being scanned (for badge display).</summary>
public string? ScanningRootPath
{
    get => _scanningRootPath;
    private set => SetProperty(ref _scanningRootPath, value);
}
private string? _scanningRootPath;
```

Add methods:
```csharp
/// <summary>Mark a root as currently being scanned (sets badge).</summary>
public void SetScanning(string rootPath) { ... }

/// <summary>Clear scanning state (scan complete or cancelled).</summary>
public void ClearScanning() { ... }
```

### 4.3 Scanning Badge on Root Entries

**File:** `ShellPaneItemViewModel.cs`

Add property:
```csharp
/// <summary>Suffix appended to LaunchTarget for scanning state.
/// "⏳ Scanning..." when this root is being scanned.</summary>
public string ScanningBadge { get; init; } = string.Empty;
```

**File:** `ShellViewModel.cs` — `JumpToLibraryRoots()` and `LoadGamesForRoot()`

When building root items, check if each root is the current scanning root and set `ScanningBadge` accordingly.

### 4.4 Async F5 Handler

**File:** `MainWindow.axaml.cs` — `RefreshCurrentRootAsync()`

Convert from synchronous to async:
```csharp
private async Task RefreshCurrentRootAsync()
{
    if (_viewModel is null || _libraryManager is null) return;

    // If already scanning, cancel it
    if (_scanCts is not null)
    {
        _scanCts.Cancel();
        _scanCts.Dispose();
        _scanCts = null;
        _viewModel.ClearScanning();
        SetStatusWithAutoClear("Scan cancelled.");
        return;
    }

    _scanCts = new CancellationTokenSource();
    var ct = _scanCts.Token;

    try
    {
        _viewModel.IsScanning = true;

        if (_viewModel.IsAtRootLevel)
        {
            // Scan each root on background thread
            var config = GetConfigService().Load();
            SetStatusWithAutoClear("Scanning all roots...", 0);

            foreach (var root in config.LibraryRoots)
            {
                ct.ThrowIfCancellationRequested();
                _viewModel.SetScanning(root.RootPath);
                SetStatusWithAutoClear($"Scanning {Path.GetFileName(root.RootPath)}...", 0);

                await Task.Run(() =>
                {
                    var games = _libraryManager.SelectScannerAndScan(
                        root.RootPath, root.DefaultType, ct);
                    _libraryManager.RescanRoot(root.RootPath, games);
                }, ct);
            }

            Dispatcher.UIThread.Post(() => _viewModel.Reload());
        }
        else
        {
            // Scan single root on background thread
            string rootPath = _viewModel.CurrentRootPath;
            var cfg = GetConfigService().Load();
            var matchedRoot = cfg.LibraryRoots.FirstOrDefault(r =>
                r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase));
            if (matchedRoot is null) return;

            _viewModel.SetScanning(rootPath);
            SetStatusWithAutoClear($"Scanning {Path.GetFileName(rootPath)}...", 0);

            var scannedGames = await Task.Run(() =>
                _libraryManager.SelectScannerAndScan(
                    rootPath, matchedRoot.DefaultType, ct), ct);

            Dispatcher.UIThread.Post(() =>
            {
                _viewModel.ApplyRescannedGames(scannedGames);
                SetStatusWithAutoClear(
                    scannedGames.Count == 0
                        ? "Rescan complete — no games found."
                        : $"Rescan complete — found {scannedGames.Count} game(s).");
            });
        }
    }
    catch (OperationCanceledException)
    {
        SetStatusWithAutoClear("Scan cancelled.");
    }
    catch (Exception ex)
    {
        SetStatusWithAutoClear($"Rescan failed: {ex.Message}");
    }
    finally
    {
        _viewModel?.ClearScanning();
        _viewModel.IsScanning = false;
        _scanCts?.Dispose();
        _scanCts = null;
    }
}
```

**Key field:** Add `private CancellationTokenSource? _scanCts;` to `MainWindow`.

### 4.5 Status Bar: Cancel vs Clear

The existing `SetStatusWithAutoClear` auto-clears after 5 seconds. During scanning, status should NOT auto-clear (current behavior with `autoClearMs: 0`). After scan completes, normal auto-clear resumes.

### 4.6 Thread Safety for ObservableCollection

The `Items` collection is an `ObservableCollection<ShellPaneItemViewModel>`. It's accessed from the UI thread only (via `Dispatcher.UIThread.Post()`). The scan runs on `Task.Run()` and only touches the database (which is thread-safe for reads). After scan completes, the UI thread repopulates `Items` from the database. No concurrent collection access.

---

## 5. Step-by-Step Implementation

### Step 1: Add CancellationToken to Scan Pipeline

**Files:**
- `src/GamingCommander.Core/ILibraryManager.cs` — add `CancellationToken` to `Refresh()` and `SelectScannerAndScan()` signatures
- `src/GamingCommander.App/Services/LibraryManager.cs` — pass token, check per-root
- `src/GamingCommander.App/Services/FolderScanner.cs` — add token to `Scan()`, check per-subdirectory
- `src/GamingCommander.App/Services/SteamLibraryScanner.cs` — add token to `Scan()` and `ScanAll()`

**Tests:** Update existing `LibraryManagerTests` — add test for cancellation propagation.

### Step 2: Add Scan State to ShellViewModel

**Files:**
- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — add `IsScanning`, `ScanningRootPath`, `SetScanning()`, `ClearScanning()` properties/methods
- `src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs` — add `ScanningBadge` property

**Tests:** New `ShellViewModelTests` — test scanning state transitions.

### Step 3: Add Scanning Badge to Left Pane UI

**Files:**
- `src/GamingCommander.App/MainWindow.axaml` — update root-level item template to show `ScanningBadge` suffix
- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — populate `ScanningBadge` in `JumpToLibraryRoots()` and `LoadGamesForRoot()`

**Tests:** Visual verification (manual).

### Step 4: Async F5 Handler + Cancellation Toggle

**Files:**
- `src/GamingCommander.App/MainWindow.axaml.cs` — convert `RefreshCurrentRootAsync()` to async with `Task.Run()`, add `_scanCts` field, F5 toggle behavior (start/cancel)

**Tests:** Build + existing tests pass. No new automated tests (UI-layer async is hard to unit test without Avalonia runtime).

### Step 5: Documentation

**Files:**
- `docs/GAME-DETECTION-LOGIC.md` — update pipeline flowchart (add CancellationToken)
- `META/SESSION/CURRENT.md` — update scan architecture notes
- `META/SESSION/NEXT.md` — mark Plan 113 items as complete

---

## 6. Success Criteria

- [x] F5 rescan runs on background thread — UI remains responsive during scan
- [x] Left pane shows "⏳ Scanning..." badge on root being scanned
- [x] User can navigate to other roots while scan is in progress
- [x] Status bar shows current folder being scanned
- [x] Pressing F5 again during scan cancels the in-progress scan
- [x] CancellationToken propagates to FolderScanner.Scan() and SteamLibraryScanner.Scan()
- [x] Build clean, all existing tests pass
- [x] No thread-safety issues (no concurrent ObservableCollection access)

---

## 7. Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Database reads during scan may conflict with writes | `GamesDatabaseService` already serializes to JSON with locks; reads are from in-memory cache |
| Avalonia UI freeze if Task.Run blocks on I/O | All filesystem I/O is in Task.Run; UI thread only touches ViewModel properties |
| Cancellation mid-scan leaves partial database state | `RescanRoot` is called after scan completes; cancelled scan simply doesn't update database |
| Double F5 press race condition | `_scanCts` field is only accessed from UI thread (event handlers); no race |

---

## 8. Deferred Items

- Incremental game result streaming (show games as they're found) — future plan
- Parallel multi-root scanning — sequential is sufficient for now
- Progress bar / percentage indicator — badge + status text is enough
- Cancel button in left pane (beyond F5 toggle) — future UX enhancement
