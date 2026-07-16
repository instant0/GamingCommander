# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** Builder. Read before implementing.

---

## ✅ COMPLETED

### Theme Extraction
All hardcoded colors/fonts centralized to `App.axaml` Application.Resources with semantic names. `AppTheme.cs` provides code-behind access. All 4 windows (MainWindow, WizardWindow, GameSetupWindow, LibrarySetupWindow) + their .axaml.cs files fully converted.

### Steam Status UI Display
- `ShellPaneItemViewModel` — `PlatformStatus` and `PlatformStatusColor` fields
- `ShellViewModel.LoadGamesForRoot()` — extracts `SteamStatus`, maps to colors
- `MainWindow.axaml` — status row in details pane
- `HexToBrushConverter` — runtime hex string → brush

---

## Next: VFS Display Enhancements (Plan Only)

The user wants a planning document for improved VFS display to support game relocation awareness:

1. **Orphaned games** — Show games that "should" be in a location based on ACF data but whose game files are missing
2. **Cross-library mismatches** — Show when ACF is on D: but game files are on E: with repair action to move ACF to correct directory
3. **List coloring** — Color-code orphaned/misplaced games in the left pane list (not just details panel)

**Create planning doc:** `planning/vfs-display-enhancements.md`

---

## After That: Multi-Theme System (Plan Only)

Plan (don't implement) multi-theme support:
- "Norton Commander Style" (current — blue/cyan on dark)
- "Windows Commander" (white/yellow on dark)
- "GrayScale" (monochrome)

**Create planning doc:** `planning/multi-theme-system.md`

---

## After That: Phase 2.1 SyncMove Migration

Manifest-aware game relocation for Steam (ACF) and Epic (.item).
**Planning doc:** `planning/04-phase-2-syncmove.md`

---

### SDK Upgrade Note

.NET 9 upgrade (`planning/90-sdk-upgrade.md`) is **lowest priority**. Working application first.
