# Task T69: Launch UX Polish

**Tier:** 1 — Verification/Polish
**Phase:** H — MVP
**Effort:** ~10 min
**Risk:** Minimal
**Status:** Pending
**Prerequisites:** T71 (Remove F5 first to avoid verifying a removed keybind)
**WP:** WP-4

---

## Objective

Fix two help text discrepancies and verify the remaining key bindings are correct. The command bar, keyboard handlers, and launch failure handling are already verified correct.

## What Needs to Change

### 1. `HelpDialogBuilder.cs` — Fix F4 description

**Current (line 33):** `"Edit game type / tags"`
**Problem:** F4 opens `GameSetupWindow` which edits 6 fields: DisplayName, GameSource, ExecutablePath, LauncherPath, CommandLineArguments, ManifestPath. "Edit game type / tags" is inaccurate.
**Fix:** Change to `"Configure game — name, type, exe, args"` (or similar)

### 2. `HelpDialogBuilder.cs` — Fix F9 label consistency

Three different labels exist for the same action:
- Command bar: `"Drives"` (ShellViewModel.Commands)
- Help dialog: `"Jump to library roots"` (HelpDialogBuilder)
- Interaction hint: `"F9: roots"` (ShellViewModel.InteractionHint)

**Fix:** Unify to `"Library Roots"` in all three locations.

### 3. Verification (already confirmed correct)

These items were verified during task evaluation and require no changes:
- All 10 command bar buttons have `PointerPressed="CommandButtonPressed"` and correct `Tag`
- No `IsHitTestVisible="False"` on any button
- F3/F8 stubs show clear "coming in a future update" messages
- `LaunchSelectedGameAsync()` catch shows `ex.Message` (no stack trace)
- Selection preserved after launch failure (no `SelectedIndex` mutation)
- `Enter` correctly handles both launch and drill-in

## Context

- Code evaluation confirmed all buttons are fully interactive
- F4 description is the only inaccurate help text
- F9 label inconsistency is cosmetic but worth fixing for polish
- `S` (search stub) and `T` (legacy retag) not in help — acceptable for MVP

## Requirements

- [ ] F4 help text updated to "Configure game" (or accurate description)
- [ ] F9 label unified across command bar, help, and hint
- [ ] Build clean, existing tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Manual: F1 opens help, verify F4 and F9 descriptions are accurate

## Completion Notes

- **Completed:** 2026-07-26
- **What was done:** Fixed F4 help text ("Configure game — name, type, exe, args"), unified F9 label to "Library Roots" across command bar/help/hint, updated InteractionHint to remove F5 and reflect new wording, fixed right-pane F4 hint in MainWindow.axaml.
- **Verification:** Build clean (0 errors), 206 tests passing. grep confirms no stale "Drives", "edit tags", or "type/tags" in user-facing text.
- **Issues encountered:** None
