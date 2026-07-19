# Task T28: Extract ShellDetailsViewModel from ShellViewModel

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~30 min
**Risk:** Low
**Status:** ⏭️ skipped — high risk, low value

---

## Evaluation Notes (2026-07-19)

**Reason for skipping:** ShellViewModel is 384 lines (under 500-line limit). The 15 detail properties are simple pass-throughs to `SelectedItem` — no complex logic to extract. The XAML binding update (16+ changes from `{Binding DetailsName}` to `{Binding Details.DetailsName}`) is mechanical but error-prone — a missed binding would cause silent UI breakage. Additionally:
- Task lists 15 properties but claims 17 — missing `HasGameSelected`
- Future features (categories, search, metadata) will add more properties, but that's when the split should happen
**Prerequisites:** None

---

## Objective

`ShellViewModel.cs` (362 lines) manages three concerns: navigation (library roots → games → details), details panel state (17 properties for game metadata display), and status bar text. Extract the details panel logic to a dedicated ViewModel. This is a proactive split — details will grow with categories, search results, and metadata display.

## What Needs to Change

### 1. New file: `src/GamingCommander.UI/ViewModels/ShellDetailsViewModel.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `ShellDetailsViewModel.cs` with namespace `GamingCommander.UI.ViewModels`
- [ ] Add `/// <summary>` to class: "Manages the details panel state for the selected game. Provides bound properties for game metadata display."
- [ ] Move the following from `ShellViewModel.cs`:
  - `DetailsName` property (line 81)
  - `DetailsPath` property (line 82)
  - `DetailsType` property (line 83)
  - `DetailsExecutable` property (line 84)
  - `DetailsLastModified` property (line 85)
  - `DetailsResolvedType` property (line 86)
  - `DetailsPlatformId` property (line 87)
  - `HasPlatformId` property (line 88)
  - `DetailsPlatformStatus` property (line 89)
  - `HasPlatformStatus` property (line 90)
  - `DetailsPlatformStatusColor` property (line 91)
  - `DetailsPlatformStatusDetail` property (line 92)
  - `HasPlatformStatusDetail` property (line 93)
  - `HasSelection` property (line 94)
  - `HasOverride` property (line 97)
  - `UpdateDetailsForSelection()` method (lines 329-348)
- [ ] Add `/// <summary>` XML docs to all members
- [ ] Add a method to update all details from a `ShellPaneItemViewModel`:
  ```csharp
  /// <summary>
  /// Updates all detail panel properties from the selected item.
  /// Call this when the selection changes.
  /// </summary>
  public void UpdateFromSelection(ShellPaneItemViewModel? item)
  {
      // ... extracted logic from UpdateDetailsForSelection
  }
  ```

### 2. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

**Current state:** Lines 81-97, 329-348 contain details panel properties and update logic
**Actions:**
- [ ] Add `ShellDetailsViewModel` as a property:
  ```csharp
  /// <summary>
  /// Details panel state for the currently selected game.
  /// </summary>
  public ShellDetailsViewModel Details { get; } = new();
  ```
- [ ] Remove all 17 detail properties (lines 81-97)
- [ ] Remove `UpdateDetailsForSelection()` method (lines 329-348)
- [ ] Update `SelectedIndex` setter to call `Details.UpdateFromSelection(SelectedItem)`
- [ ] Reduce ShellViewModel from ~362 to ~280 lines

### 3. XAML Binding Updates (if needed)

**Current state:** `MainWindow.axaml` binds to `DetailsName`, `DetailsPath`, etc. directly on ShellViewModel
**Actions:**
- [ ] Update bindings to go through `Details` property:
  - `{Binding DetailsName}` → `{Binding Details.DetailsName}`
  - `{Binding DetailsPath}` → `{Binding Details.DetailsPath}`
  - ... etc for all 17 properties
- [ ] Verify all bindings resolve correctly

## Context

- The details panel has 17 bound properties — more than the navigation logic
- Future features (categories, search, metadata) will add more detail properties
- `ShellPaneItemViewModel` is the data source — `UpdateFromSelection` takes it as input
- The `Details` property is a public sub-object — XAML bindings use dot notation
- No behavior change — same properties, same update logic, just relocated

## Requirements

- [ ] `ShellDetailsViewModel.cs` created with all 17 properties + `UpdateFromSelection`
- [ ] All properties have `/// <summary>` XML docs
- [ ] `ShellViewModel.cs` no longer contains detail properties or `UpdateDetailsForSelection`
- [ ] `ShellViewModel.Details` property exposes the sub-ViewModel
- [ ] XAML bindings updated to use `Details.*` prefix
- [ ] No behavior change — same displayed values

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "DetailsName\|DetailsPath\|DetailsType" src/GamingCommander.UI/ViewModels/ShellViewModel.cs` returns 0 (moved)
- [ ] `grep -c "Details\." src/GamingCommander.App/MainWindow.axaml` returns 17+ (bindings updated)
- [ ] `wc -l src/GamingCommander.UI/ViewModels/ShellViewModel.cs` shows < 300 lines

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
