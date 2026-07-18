# Task T18: Extract Shared AvailableTypes Constant

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~15 min
**Risk:** Minimal
**Status:** pending
**Prerequisites:** None

---

## Objective

The same 10-element string array `AvailableTypes` is defined identically in three places: `GameSetupWindow.axaml.cs` (lines 25-29), `LibrarySetupViewModel.cs` (lines 36-48), and `WizardViewModel.cs` (lines 37-49). This is a maintenance hazard — adding a new game source requires updating three files. Define it once and reference it everywhere.

## What Needs to Change

### 1. `src/GamingCommander.Core/Models/GameSourceParser.cs`

**Current state:** Contains `InferFromPath()` and `ParseFromString()` static methods. No `AvailableTypes` property.
**Actions:**
- [ ] Add a new static property:
  ```csharp
  /// <summary>
  /// Human-readable display names for all supported game source types.
  /// Used by UI dropdowns and combo boxes.
  /// </summary>
  public static readonly string[] AvailableTypes =
  [
      "Standalone", "Steam", "GOG", "Epic", "EA App",
      "Ubisoft Connect", "Battle.net", "Xbox", "Rockstar", "Steam Emulator"
  ];
  ```
- [ ] This array is already in the Core assembly, accessible to all other projects

### 2. `src/GamingCommander.App/GameSetupWindow.axaml.cs`

**Current state:** Lines 25-29 define `public string[] AvailableTypes { get; } = [...]`
**Actions:**
- [ ] Delete the property definition (lines 25-29)
- [ ] Replace all references to `AvailableTypes` with `GameSourceParser.AvailableTypes`
  - Used in `RenderFields()` at line ~78 and `MakeComboRow()` at line ~175

### 3. `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs`

**Current state:** Lines 36-48 define `public string[] AvailableTypes { get; } = [...]`
**Actions:**
- [ ] Delete the property definition (lines 36-48)
- [ ] Replace all references to `AvailableTypes` with `GameSourceParser.AvailableTypes`
  - Used in the class to populate the source type dropdown

### 4. `src/GamingCommander.App/ViewModels/WizardViewModel.cs`

**Current state:** Lines 37-49 define `public string[] AvailableTypes { get; } = [...]`
**Actions:**
- [ ] Delete the property definition (lines 37-49)
- [ ] Replace all references to `AvailableTypes` with `GameSourceParser.AvailableTypes`
  - Used in the class to populate the source type dropdown

## Context

- All three definitions are byte-for-byte identical
- `GameSourceParser` is already the right home — it's the shared model for game source types
- The array is `public static readonly` — no instantiation needed
- All three consumer files already have `using GamingCommander.Core.Models;`

## Requirements

- [ ] `GameSourceParser.AvailableTypes` defined once in `GameSourceParser.cs`
- [ ] `GameSetupWindow.axaml.cs` no longer defines `AvailableTypes`
- [ ] `LibrarySetupViewModel.cs` no longer defines `AvailableTypes`
- [ ] `WizardViewModel.cs` no longer defines `AvailableTypes`
- [ ] All three files reference `GameSourceParser.AvailableTypes`
- [ ] `GameSourceParser.AvailableTypes` has `/// <summary>` XML doc
- [ ] No behavior change — same array values

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -rn "AvailableTypes" src/` shows exactly 1 definition (in GameSourceParser.cs) and 3+ references
- [ ] `grep -c "AvailableTypes = \[" src/` returns 1 (only one definition)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
