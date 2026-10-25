# Task T27: Extract HelpDialogBuilder from MainWindow

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Low
**Status:** ✅ completed (updated per evaluation)

---

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created `HelpDialogBuilder.cs` with `ShowHelpAsync(Window owner)` static method. Moved all 107 lines of UI construction logic from MainWindow. Updated MainWindow F1 handlers (OnKeyDown + CommandButtonPressed) to call `HelpDialogBuilder.ShowHelpAsync(this)`. Removed `ShowHelpAsync` from MainWindow. Cleaned up unused `using` statements.
- **Verification:** Build clean (0 errors), 17 tests passing.
- **Issues encountered:** Had to add `using Avalonia;` for `Thickness` type.
**Prerequisites:** None

---

## Objective

`MainWindow.axaml.cs` contains `ShowHelpAsync()` (lines 380-487) — a 107-line method that programmatically builds a help dialog with keybinding descriptions. This is pure UI construction with no dependency on MainWindow state. Extract it to a dedicated builder class. This is a proactive split — help content will grow as more features are added.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/HelpDialogBuilder.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `HelpDialogBuilder.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Builds the programmatic help dialog showing keyboard shortcuts and feature descriptions."
- [ ] Move `ShowHelpAsync()` logic from MainWindow.axaml.cs (lines 380-487):
  ```csharp
  /// <summary>
  /// Creates and shows the help dialog window with all keyboard shortcuts.
  /// Returns a Task that completes when the dialog is closed.
  /// </summary>
  public static async Task ShowHelpAsync(Window owner)
  {
      // ... all the UI construction logic from ShowHelpAsync
  }
  ```
- [ ] The method takes `Window owner` parameter (for dialog positioning) instead of accessing `this`
- [ ] All UI construction (Grid, TextBlock, Row definitions) stays in this class
- [ ] Add `/// <summary>` XML doc

### 2. `src/GamingCommander.App/MainWindow.axaml.cs`

**Current state:** Lines 380-487 contain `ShowHelpAsync()`
**Actions:**
- [ ] Delete `ShowHelpAsync()` method (lines 380-487)
- [ ] Replace call site (in `OnKeyDown` F1 handler) with:
  ```csharp
  case Key.F1:
      _ = HelpDialogBuilder.ShowHelpAsync(this);
      break;
  ```
- [ ] Reduce MainWindow from ~541 to ~430 lines (before T26)

## Context

- `ShowHelpAsync` is 107 lines of pure UI construction — Grid rows, TextBlocks, styling
- It only reads `AppTheme` for colors/fonts — no MainWindow state needed
- The method is already `private` — making it a static method on a builder class is a clean extraction
- Help content will grow as F3 (view), F8 (categories), S (search) are implemented
- Dialog positioning uses `owner` window as parent — passed as parameter

## Requirements

- [ ] `HelpDialogBuilder.cs` created with `ShowHelpAsync(Window owner)` static method
- [ ] Method has `/// <summary>` XML doc
- [ ] MainWindow.axaml.cs no longer contains `ShowHelpAsync()`
- [ ] F1 handler calls `HelpDialogBuilder.ShowHelpAsync(this)`
- [ ] No behavior change — same dialog content, same positioning
- [ ] `HelpDialogBuilder` class is `public static`

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "ShowHelpAsync" src/GamingCommander.App/MainWindow.axaml.cs` returns 0 (removed)
- [ ] `grep -c "HelpDialogBuilder" src/GamingCommander.App/Services/HelpDialogBuilder.cs` returns 1 (class definition)
- [ ] `wc -l src/GamingCommander.App/Services/HelpDialogBuilder.cs` shows < 130 lines

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
