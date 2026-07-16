# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.

---

## Phase
**Theme Extraction & VFS Display Enhancements** — Centralized theme system complete; VFS display enhancements planned

## Objectives Achieved
1. ✅ Game detection overhaul — Phase 1+2 complete
2. ✅ Build versioning + re-wizard system (0.3.0)
3. ✅ In-memory VFS cache for GamesDatabaseService
4. ✅ Keyboard layout overhaul (10 F-keys, Enter=launch, Esc=up, double-tap)
5. ✅ Bug fixes & cleanup — Plan 95 complete

## Completed This Session

### Theme Extraction (Complete)
All hardcoded colors and font sizes centralized to `App.axaml` Application.Resources:
- **App.axaml** — 23 `SolidColorBrush` resources + 8 font size resources with semantic names (WindowBg, PaneBg, TextPrimary, etc.)
- **AppTheme.cs** — static accessor class for code-behind files (resolves resources at runtime via `TryFindResource`)
- **MainWindow.axaml** — all hardcoded values replaced with `{DynamicResource ...}` bindings
- **WizardWindow.axaml** — fully converted
- **GameSetupWindow.axaml** — fully converted
- **LibrarySetupWindow.axaml** — fully converted
- **MainWindow.axaml.cs** — `ShowHelpAsync()` uses `AppTheme.*` for all colors/fonts
- **WizardWindow.axaml.cs** — `RenderEntries()` uses `AppTheme.*`
- **GameSetupWindow.axaml.cs** — `RenderFields()`, `MakeFieldRow()`, `MakeComboRow()` use `AppTheme.*`
- **LibrarySetupWindow.axaml.cs** — `RenderRoots()` uses `AppTheme.*`
- **HexToBrushConverter.cs** — fallback uses `AppTheme.TextSecondary`
- **NortonCommander.axaml** — standalone theme file (kept as reference but App.axaml has direct resources for runtime access)

## Test Status
**17 tests passing** (5 Core + 1 Migration + 11 App). Build clean, 0 errors. 4 Avalonia AVLN3001 warnings (cosmetic).

## Key Architecture Decisions
- **Theme centralized in App.axaml** — All 23 color brushes and 8 font sizes live as Application.Resources with semantic names (e.g. `TextAccent`, `ButtonBgAction`). `AppTheme.cs` provides static accessor for code-behind. To re-theme: swap the resources in App.axaml. `NortonCommander.axaml` retained as reference.
- **AppTheme name (not Theme)** — Avoids collision with `Avalonia.Controls.Theme` which is a type in Avalonia's namespace.
- **SolidColorBrush in resources (not Color)** — `DynamicResource` bindings for `Background`/`Foreground` require brush types, not plain `Color`.
- **ILauncher retired** — ADR-008 described `ILauncher` but the pragmatic two-tier scanner architecture (`LibraryManager` → `FolderScanner`/`SteamLibraryScanner`) replaced it.
- **GameSourceParser** in Core — shared by all ViewModels that need to convert between display strings and `GameSourceKind` enum values.
- **GameEntryId** in Core — single source of truth for deterministic game entry IDs.
- **MainWindow stores LibraryManager** — eliminates repeated construction of temporary LibraryManager instances.

---

**Next session: Read META/SESSION/NEXT.md before starting.**
