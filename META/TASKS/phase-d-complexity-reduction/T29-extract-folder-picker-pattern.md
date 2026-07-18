# Task T29: Extract Folder-Picker-and-Add Pattern

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Low
**Status:** pending
**Prerequisites:** T18 (AvailableTypes extracted)

---

## Objective

Three files contain identical folder-picker-and-add logic: open folder picker, get path, normalize it, check for duplicates, add to library roots. Extract this to a shared helper method.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/LibraryRootHelper.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `LibraryRootHelper.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Shared operations for adding library roots: folder picking, path normalization, and duplicate checking."
- [ ] Add `NormalizeLibraryRoot(string path)` static method:
  ```csharp
  /// <summary>
  /// Normalizes a library root path for consistent comparison.
  /// Trims trailing separators and ensures consistent casing.
  /// </summary>
  internal static string NormalizeLibraryRoot(string path)
  {
      return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
  }
  ```
  - Note: This delegates to `LibraryManager.NormalizeLibraryRoot()` if it exists, or replaces it
- [ ] Add `IsDuplicateRoot(string path, IEnumerable<LibraryRoot> existingRoots)` static method:
  ```csharp
  /// <summary>
  /// Checks if a path already exists in the configured library roots.
  /// Uses case-insensitive comparison.
  /// </summary>
  internal static bool IsDuplicateRoot(string path, IEnumerable<LibraryRoot> existingRoots)
  {
      string normalized = NormalizeLibraryRoot(path);
      return existingRoots.Any(r =>
          NormalizeLibraryRoot(r.Path).Equals(normalized, StringComparison.OrdinalIgnoreCase));
  }
  ```

### 2. `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs`

**Current state:** Lines 62-76 contain folder picker + normalize + duplicate check logic
**Actions:**
- [ ] Replace the normalize/duplicate logic with calls to `LibraryRootHelper.NormalizeLibraryRoot()` and `LibraryRootHelper.IsDuplicateRoot()`
- [ ] Keep the folder picker UI code (that's View-specific)

### 3. `src/GamingCommander.App/ViewModels/WizardViewModel.cs`

**Current state:** Lines 68-82 contain folder picker + normalize + duplicate check logic
**Actions:**
- [ ] Replace the normalize/duplicate logic with calls to `LibraryRootHelper.NormalizeLibraryRoot()` and `LibraryRootHelper.IsDuplicateRoot()`
- [ ] Keep the folder picker UI code

### 4. `src/GamingCommander.App/MainWindow.axaml.cs`

**Current state:** Lines 352-378 contain folder picker + normalize + duplicate check logic in `AddLibraryRootAsync()`
**Actions:**
- [ ] Replace the normalize/duplicate logic with calls to `LibraryRootHelper.NormalizeLibraryRoot()` and `LibraryRootHelper.IsDuplicateRoot()`
- [ ] Keep the folder picker UI code

## Context

- The folder picker UI code is different in each file (different parent containers)
- The normalize + duplicate check logic is identical across all three
- `LibraryManager.NormalizeLibraryRoot()` already exists — we're just making it accessible without a LibraryManager instance
- The `LibraryRoot` record is already in `GamingCommander.Core.Models`

## Requirements

- [ ] `LibraryRootHelper.cs` created with `NormalizeLibraryRoot` and `IsDuplicateRoot`
- [ ] Both methods have `/// <summary>` XML docs
- [ ] All three consumer files use the helper instead of inline logic
- [ ] No behavior change — same normalization and duplicate detection

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "LibraryRootHelper" src/` shows 4+ references (definition + 3 call sites)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
