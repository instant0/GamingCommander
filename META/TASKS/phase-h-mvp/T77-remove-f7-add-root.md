# Task T77: Remove F7 (Add Root) — Redundant With F2

**Tier:** 2 — Cleanup
**Phase:** H — MVP
**Effort:** ~15 min
**Risk:** Minimal
**Status:** Complete
**Prerequisites:** T75 Complete
**WP:** WP-5

---

## Objective

Remove the F7 keyboard shortcut and command bar button ("Add Root") because it is a strict subset of F2 (Library Setup) and has a worse type-detection heuristic.

## Analysis

### F7 vs F2 — What Each Does

| Aspect | F7 (AddRootAsync) | F2 (LibrarySetupWindow) |
|--------|-------------------|------------------------|
| **Scope** | Add one folder only | Full management: list, add, remove, rescan |
| **Type detection** | Binary: `LooksLikeSteamLibrary()` → Steam or Standalone only | Rich: `GameSourceParser.InferFromPath()` → Steam, GOG, Epic, EA, Ubisoft, Battle.net, Xbox, Rockstar |
| **Type override** | None — user cannot change the type | ComboBox per root with all 10 source types |
| **Remove root** | Not available | Yes |
| **Rescan existing root** | Not available (F6 does this) | Yes — per-root Rescan button |
| **Duplicate check** | None — relies on `LibraryManager.AddRoot()` internal dedup | Explicit before adding |
| **UI feedback** | Single status bar message | Visual list with game counts, type labels |

### Why F7 Should Be Removed

1. **Worse type detection** — F7 only detects Steam vs Standalone. F2 detects 10 store types via path heuristics. Users who add a GOG or Epic folder via F7 get it tagged as "Standalone" instead of the correct type.

2. **No type override** — F7 locks the user into the auto-detected type. F2 lets them override with a dropdown.

3. **No management** — F7 can only add. F2 can list, rescan, and remove roots.

4. **Duplicated code** — Both call `LibraryManager.AddRoot()`. F7 is 30 lines of MainWindow.axaml.cs that duplicate what F2 does better.

5. **Confusing UX** — Two buttons doing the same thing with different quality creates confusion. "What's the difference between F2 and F7?" is a question that shouldn't exist.

6. **Reduces command bar clutter** — 9 buttons → 8 buttons. Cleaner footer.

### What Changes

| Item | Change |
|------|--------|
| `MainWindow.axaml.cs` | Removed `Key.F7` case from `OnKeyDown()` |
| `MainWindow.axaml.cs` | Removed `case "F7":` from `CommandButtonPressed()` |
| `MainWindow.axaml.cs` | Removed `AddRootAsync()` method (33 lines) |
| `MainWindow.axaml.cs` | Removed unused `using Avalonia.Platform.Storage;` import |
| `MainWindow.axaml.cs` | Updated F6 empty-roots message: "F2 or F7" → "F2" |
| `ShellViewModel.cs` | Removed F7 from `Commands` list (9 → 8 buttons) |
| `HelpDialogBuilder.cs` | Removed F7 from help dialog |

**Note:** `LibraryManager` itself is still needed — F2 uses it, and F6 (RefreshCurrentRootAsync) uses it. Only the MainWindow's direct instantiation of scanners for F7 can be removed if F6 also gets its scanner from LibraryManager. Need to verify F6 dependency.

### F6 (Refresh) Dependency Check

F6 calls `_libraryManager.Refresh()` and `_libraryManager.SelectScannerAndScan()`. These use the `_scanner` and `_steamScanner` fields on MainWindow. These fields are also used by `_libraryManager` which is constructed with them in the MainWindow constructor. So `_libraryManager` and its dependencies must stay — only the F7-specific UI code is removed.

### Backward Compatibility

- **F7 key binding removed** — Users accustomed to F7 for quick-add must use F2 instead.
- **Command bar** — 8 buttons instead of 9. No gap in functionality.
- **Help dialog** — F7 entry removed.

## Tests

Existing tests don't cover F7 directly (it's a UI-only path). No test changes needed.

## Verification

- [x] `dotnet build` passes
- [x] `dotnet test` passes (no regressions) — 209 tests
- [x] F7 key no longer handled (falls through to default handler)
- [x] F2 still works: list roots, add root, rescan, remove
- [x] Command bar shows 8 buttons, no F7
- [x] Help dialog no longer shows F7
- [x] F6 empty-roots message updated to say "Press F2" (not "F2 or F7")
