# Task T69: Launch UX Polish

**Tier:** 1 — Verification/Polish
**Phase:** H — MVP
**Effort:** ~15 min
**Risk:** Minimal
**Status:** Pending
**Prerequisites:** T61, T62
**WP:** WP-4

---

## Objective

Verify that the command bar buttons are clickable and the help dialog matches actual key bindings. The codebase already has both keyboard and mouse-click handlers for all F-key buttons. This task is a verification pass to confirm correctness and fix any discrepancies found.

## What Needs to Change

### 1. `src/GamingCommander.App/MainWindow.axaml` — Command bar

**Current state:** All 10 F-key buttons have `PointerPressed="CommandButtonPressed"` handlers. Both `OnKeyDown` and `CommandButtonPressed` dispatch to the same methods.

**Actions:**
- [ ] Verify each button in the XAML has `PointerPressed="CommandButtonPressed"` and a `Tag` matching its hotkey
- [ ] Verify `IsHitTestVisible` is not set to `false` on any button
- [ ] Verify F3 and F8 stubs show clear "coming soon" status messages (not silent no-ops)

### 2. `src/GamingCommander.App/HelpDialogBuilder.cs` — Help text

**Current state:** Lines 28-43. Help text matches actual bindings (confirmed by analysis).

**Actions:**
- [ ] Verify help text matches `OnKeyDown` handler behavior for all keys
- [ ] Verify F4 description says "Edit game" (not "Edit game type / tags" if that's inaccurate)
- [ ] Add `S` key documentation if search is implemented; omit if still a stub

### 3. Launch failure handling

**Actions:**
- [ ] Verify `LaunchSelectedGameAsync()` catch block shows user-readable message (not stack trace)
- [ ] Verify status bar text is readable (not too long, not truncated)
- [ ] Verify selection is preserved after launch failure (no navigation change)

## Context

- Analysis confirmed all buttons are fully interactive (no decorative-only)
- Help text is accurate for all documented bindings
- Minor omissions: `S` (search stub) and `T` (legacy retag) not in help — acceptable for MVP
- F3/F8 stubs display "coming in a future update" — acceptable for MVP
- This task is primarily a verification pass, not a code change

## Requirements

- [ ] All command bar buttons are clickable (not decorative)
- [ ] Help text matches actual key binding behavior
- [ ] F3/F8 stubs show clear status message
- [ ] Launch failure shows user-readable error in status bar
- [ ] Selection preserved after launch failure

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Manual: click each command bar button, verify correct action
- [ ] Manual: F1 opens help, read each line, verify accuracy
- [ ] Manual: launch a game, verify status bar message

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
