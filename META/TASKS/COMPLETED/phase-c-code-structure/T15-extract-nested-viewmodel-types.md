# Task T15: Extract Nested Classes from ViewModels

**Tier:** 2 — Code Structure
**Phase:** C — Code Structure
**Effort:** ~20 min
**Risk:** Low
**Status:** completed

---

## Objective

Two ViewModels contain nested classes that serve as data models for UI items. Extract these to their own files for clarity.

## What Needs to Change

### 1. `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs`

**Current state:** Contains `LibrarySetupViewModel` class + nested `LibraryRootEntry` class.

**Actions:**
- Extract `LibraryRootEntry` to new file `LibraryRootEntry.cs` in the same directory
- Keep `LibrarySetupViewModel` in `LibrarySetupViewModel.cs`
- Both stay in `GamingCommander.App.ViewModels` namespace

### 2. `src/GamingCommander.App/ViewModels/WizardViewModel.cs`

**Current state:** Contains `WizardViewModel` class + nested `WizardLibraryEntry` class.

**Actions:**
- Extract `WizardLibraryEntry` to new file `WizardLibraryEntry.cs` in the same directory
- Keep `WizardViewModel` in `WizardViewModel.cs`
- Both stay in `GamingCommander.App.ViewModels` namespace

## Context

- Nested classes are a common C# pattern but reduce discoverability
- `LibraryRootEntry` and `WizardLibraryEntry` are simple data holders (properties only, no logic)
- Extracting them makes the codebase easier to navigate for junior developers and AI agents
- The `App/ViewModels/` directory currently has 2 files — it will grow to 4

## Requirements

- [ ] Extract `LibraryRootEntry` to `LibraryRootEntry.cs`
- [ ] Extract `WizardLibraryEntry` to `WizardLibraryEntry.cs`
- [ ] Remove nested class declarations from parent files
- [ ] Add `/// <summary>` to extracted classes (if not present)
- [ ] No logic changes

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `App/ViewModels/` contains 4 files: LibrarySetupViewModel.cs, LibraryRootEntry.cs, WizardViewModel.cs, WizardLibraryEntry.cs
- [ ] No nested class declarations remain in parent files

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Extracted 2 co-located classes to their own files:
  1. `LibraryRootEntry` from `LibrarySetupViewModel.cs` → `LibraryRootEntry.cs`
  2. `WizardLibraryEntry` from `WizardViewModel.cs` → `WizardLibraryEntry.cs`
- Both classes were already at namespace level (not truly nested), just co-located in the same file
- Both retained their existing XML docs
- **Verification:** Build clean, 17 tests passing, ViewModels directory has 4 files as expected
- **No issues encountered.**
