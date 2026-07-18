# Plan 96: VFS Display Enhancements — Game Health Visibility

**Date:** 2026-07-17
**Status:** Completed
**Priority:** P1 (user-facing visibility)
**Depends on:** Plan 95 (theme extraction), Plan 04-phase-2-syncmove.md (manifest repair)

---

## Context

The Steam status UI (Installed/Moved/Orphaned) is currently shown only in the right-side details panel. The user requested:

1. **Orphaned games from ACF perspective** — Show games that an ACF "expects" to find at a location, but whose game files are actually missing. This is the inverse of the current orphan detection: instead of "game folder exists but no ACF", it's "ACF exists but game folder is gone."
2. **Cross-library mismatches** — When an ACF sits in library A but game files are detected in library B, show this mismatch with a repair action (move ACF to correct library).
3. **Left-pane list coloring** — Color-code orphaned/misplaced games directly in the game list (not just the details panel), so users can scan the list visually.

---

## Current State

### What We Have

| Component | Status | File |
|-----------|--------|------|
| Steam scanner detects Installed/Moved/Orphaned | ✅ Done | `SteamLibraryScanner.cs` |
| Status stored in `GameEntry.Extra["SteamStatus"]` | ✅ Done | `GameEntry.Extra` dict |
| `ShellViewModel` extracts status → `ShellPaneItemViewModel` | ✅ Done | `ShellViewModel.cs` L246-258 |
| Details panel shows colored status | ✅ Done | `MainWindow.axaml` |
| `HexToBrushConverter` for runtime hex → brush | ✅ Done | `HexToBrushConverter.cs` |
| Left-pane list coloring | ❌ Missing | — |
| ACF-expects-but-missing detection | ❌ Missing | — |
| Cross-library mismatch repair action | ❌ Depends on SyncMove | — |

### What's Missing

**Detection gap:** `SteamLibraryScanner.Scan()` iterates over `common/` directories and matches against ACF data. But it does NOT iterate ACFs to find "missing game folders" — the reverse lookup. An ACF with no matching `common/<installdir>/` is silently skipped.

**Display gap:** `ShellPaneItemViewModel` has `PlatformStatusColor` but nothing in the left-pane `ListBox` template uses it for foreground/background coloring. The status only appears in the right panel.

---

## Changes

### Bundle A: ACF-Expects-But-Missing Detection

**Goal:** When scanning a Steam library, detect ACFs whose game files are missing from ALL known libraries.

#### A1. Reverse ACF lookup in `SteamLibraryScanner`

**File:** `SteamLibraryScanner.cs`

After the current scan loop (which iterates `common/` dirs), add a second pass that iterates ALL collected ACFs and checks if each ACF's `installdir` exists in any library's `common/`:

```
For each ACF in acfMap:
    if acf.installdir does NOT exist in any library's common/:
        → Create a "Missing" GameEntry
        → Extra["SteamStatus"] = "Missing"
        → Extra["AcfLibraryPath"] = acf.LibraryPath
        → Extra["AcfFilePath"] = acf.AcfFilePath
```

This catches the case where a user deleted game files (or moved them outside Steam libraries) but left the ACF behind.

**New status value:** `"Missing"` — game files are absent from all known libraries, but an ACF still registers the game.

#### A2. Add `Missing` to status color mapping

**File:** `ShellViewModel.cs`

Extend the `platformStatusColor` switch:
```csharp
"Missing" => "#E87070",  // Same red as Orphaned (both indicate broken state)
```

**File:** `Themes/NortonCommander.axaml` + `App.axaml`

Add:
```xml
<SolidColorBrush x:Key="StatusMissing" Color="#E87070" />
```

#### A3. Extend `GameEntry.Extra` fields for Missing games

For "Missing" entries, populate:
- `SteamAppId` — from ACF (for potential store lookup)
- `AcfLibraryPath` — where the ACF lives
- `AcfFilePath` — full path to the ACF
- `AcfName` — game name from ACF (since there's no folder to read)
- `ManifestPath` — set to ACF path (enables future SyncMove repair)

The `ExecutablePath` will be empty (no files exist), `FolderName` = `installdir` from ACF.

---

### Bundle B: Cross-Library Mismatch Detection

**Goal:** Detect when an ACF is in library A but game files exist only in library B.

#### B1. Cross-library mismatch flag

**File:** `SteamLibraryScanner.cs`

During the `common/` scan loop, when a game is found with status `"Moved"` (ACF is in a different library), store additional context:
```csharp
Extra["AcfExpectedPath"] = Path.Combine(acf.LibraryPath, "steamapps", "common", folderName);
```

This gives the UI enough information to show: "ACF expects files at D:\..., files found at E:\..."

#### B2. Surface mismatch in `ShellViewModel`

**File:** `ShellViewModel.cs`

For games with status `"Moved"`, compute a more descriptive display:
```csharp
string platformStatusDetail = platformStatus switch
{
    "Moved" => $"Moved (ACF in {Path.GetFileName(acfLibraryPath)})",
    "Missing" => "Missing — ACF exists but game files not found",
    _ => platformStatus,
};
```

Add `PlatformStatusDetail` to `ShellPaneItemViewModel` for richer tooltip/detail text.

---

### Bundle C: Left-Pane List Coloring

**Goal:** Color-code game entries in the left-pane ListBox based on their platform status.

#### C1. Add `ItemStatusColor` to `ShellPaneItemViewModel`

**File:** `ShellPaneItemViewModel.cs`

Add a new field:
```csharp
/// <summary>
/// Foreground color for the game title in the left pane list.
/// Set when game has a non-normal status (Moved, Orphaned, Missing).
/// Empty for normal (Installed) or non-platform games.
/// </summary>
public string ItemStatusColor { get; init; } = string.Empty;
```

#### C2. Populate `ItemStatusColor` in `ShellViewModel.LoadGamesForRoot`

**File:** `ShellViewModel.cs`

When building `ShellPaneItemViewModel` entries, set `ItemStatusColor` from the same status mapping:
```csharp
string itemStatusColor = platformStatus switch
{
    "Installed" => string.Empty,  // No special color — use default
    "Moved" => "#E8C547",         // Yellow
    "Orphaned" => "#E87070",      // Red
    "Missing" => "#E87070",       // Red
    _ => string.Empty,
};
```

#### C3. Bind left-pane ListBox item foreground to `ItemStatusColor`

**File:** `MainWindow.axaml`

Add a `DataTemplate` for game entries in the left-pane `ListBox`. Use `HexToBrushConverter` to convert the status color:

```xml
<ListBox.ItemTemplate>
    <DataTemplate>
        <TextBlock Text="{Binding Title}"
                   Foreground="{Binding ItemStatusColor,
                               Converter={StaticResource HexToBrushConverter}}"
                   FontSize="{DynamicResource FontSizeItem}" />
    </DataTemplate>
</ListBox.ItemTemplate>
```

For non-game items (directories, parent directories), `ItemStatusColor` is empty, so the converter returns the default text color.

#### C4. Update `HexToBrushConverter` for empty string fallback

**File:** `HexToBrushConverter.cs`

When `ItemStatusColor` is empty, return the primary text brush instead of the fallback gray:
```csharp
if (string.IsNullOrWhiteSpace(hex) || hex == "#00000000")
    return AppTheme.TextPrimary;
```

---

## Data Flow Summary

```
SteamLibraryScanner.Scan()
  ├── common/ iteration → Installed / Moved / Orphaned
  └── ACF iteration    → Missing (ACF exists, no files)

GameEntry.Extra["SteamStatus"] = "Installed" | "Moved" | "Orphaned" | "Missing"

ShellViewModel.LoadGamesForRoot()
  ├── Extracts SteamStatus
  ├── Maps to PlatformStatusColor (for details panel)
  └── Maps to ItemStatusColor (for left-pane list)

MainWindow.axaml
  ├── Left pane: ListBox items use ItemStatusColor for foreground
  └── Right pane: Status row uses PlatformStatusColor
```

---

## Tasks

### Bundle A: ACF-Expects-But-Missing
- [ ] A1. Add reverse ACF lookup pass in `SteamLibraryScanner.Scan()` and `ScanAll()`
- [ ] A2. Create `CreateMissingAcfEntry()` helper in `SteamLibraryScanner.cs`
- [ ] A3. Add `StatusMissing` to theme resources (`App.axaml` + `NortonCommander.axaml`)
- [ ] A4. Add `"Missing"` case to `ShellViewModel` status color mapping
- [ ] A5. Write tests for missing-game detection (mock steamapps with ACF but no common/ folder)

### Bundle B: Cross-Library Mismatch
- [ ] B1. Store `AcfExpectedPath` in `Extra` for Moved games
- [ ] B2. Add `PlatformStatusDetail` to `ShellPaneItemViewModel`
- [ ] B3. Populate detail text in `ShellViewModel.LoadGamesForRoot()`
- [ ] B4. Surface `PlatformStatusDetail` in `MainWindow.axaml` details panel (tooltip or extra row)

### Bundle C: Left-Pane List Coloring
- [ ] C1. Add `ItemStatusColor` field to `ShellPaneItemViewModel`
- [ ] C2. Populate `ItemStatusColor` in `ShellViewModel.LoadGamesForRoot()`
- [ ] C3. Update `MainWindow.axaml` left-pane ListBox to use `ItemStatusColor` binding
- [ ] C4. Update `HexToBrushConverter` empty-string fallback to use `TextPrimary`

### Bundle D: Documentation
- [ ] D1. Update `META/CODE_MAP.md` with new Extra fields and ShellPaneItemViewModel properties
- [ ] D2. Update `META/SESSION/NEXT.md` and `META/SESSION/CURRENT.md`

---

## Execution Order

1. **Bundle A** — ACF-Expects-But-Missing detection (scanner + status mapping)
2. **Bundle B** — Cross-library mismatch display (extra context in details)
3. **Bundle C** — Left-pane list coloring (visual feedback)
4. **Bundle D** — Documentation updates
5. Build + test after each bundle

---

## Risk Assessment

| Bundle | Risk | Rationale |
|--------|------|-----------|
| A | Low | Adding a second pass to existing scan; new Extra fields don't break existing code |
| B | Low | Read-only display enhancement; no mutation |
| C | Low | Adding a binding + converter; existing layout unchanged |
| D | Zero | Documentation only |

---

## Constraints

- **No file movement.** This plan only adds detection and display. Actual repair (ACF relocation) belongs to SyncMove (Plan 04-phase-2-syncmove.md).
- **No new external dependencies.** Uses existing `HexToBrushConverter` and `DynamicResource` system.
- **Backward compatible.** New Extra fields are optional; existing code ignores unknown keys.

---

## Exit Criteria

Phase is complete when:
- Scanning a Steam library shows "Missing" games (ACF exists, no game files) in the game list with red coloring
- Moved games show cross-library context ("ACF in D:\SteamLibrary") in the details panel
- Left-pane list items are color-coded: green (Installed), yellow (Moved), red (Orphaned/Missing)
- Non-platform games and directories retain default text color
- All 17+ tests pass, build clean

---

## Out of Scope (Deferred to SyncMove)

- ACF repair / move action (F6 SyncMove dialog)
- Epic .item cross-library detection (Epic doesn't have multi-library in the same way)
- GOG, EA, Ubisoft mismatch detection (not yet implemented scanners)
