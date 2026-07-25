# Task T71: Remove F5 Launch Keybind

**Tier:** 1 — Cleanup
**Phase:** H — MVP (post-UX polish)
**Effort:** ~10 min
**Risk:** Low
**Status:** Pending
**Prerequisites:** None
**WP:** WP-4 (UX polish)

---

## Objective

`Enter` already handles game launch (and drill-in for directories). `F5` is a redundant keybind that does the same thing. Remove it to simplify the keyboard layout and avoid confusion. The Norton Commander convention uses F-keys for file operations, not launch — `Enter` is the natural launch key.

## What Needs to Change

### 1. `src/GamingCommander.App/MainWindow.axaml.cs`

**Lines 170-171:** Remove the `case Key.F5:` handler that calls `LaunchSelectedGameAsync()`.

**Lines 422-424:** Remove the `case "F5":` handler in the command dispatcher.

### 2. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

**Line 42:** Remove `F5: launch  |` from the `InteractionHint` string.

**Line 132:** Remove `new ShellCommandViewModel { Hotkey = "F5", Label = "Launch" }` from the `AvailableCommands` list.

### 3. `src/GamingCommander.App/Services/HelpDialogBuilder.cs`

**Line 34:** Remove `("F5", "Launch selected game")` from the help entries.

## Context

- `Enter` already calls `LaunchSelectedGameAsync()` for files and `NavigateInto()` for directories
- The bottom status bar shows available commands from `ShellViewModel.AvailableCommands` — removing F5 cleans up the display
- This is a pure removal with no behavioral change (Enter still works)
- Low risk — F5 was always a convenience alias

## Requirements

- [ ] `F5` key no longer triggers launch
- [ ] `F5` removed from help dialog
- [ ] `F5` removed from status bar command list
- [ ] `F5` removed from interaction hint string
- [ ] `Enter` still launches games (no regression)
- [ ] Build clean, existing tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] `grep -rn "F5" src/` returns no matches (except comments if any)
- [ ] Manual: press F5 → nothing happens
- [ ] Manual: press Enter on a game → launches

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
