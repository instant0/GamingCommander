# META/BACKLOG/TECH_DEBT.md — Technical Debt & Known Issues

**Nature:** Mutable. Entries appended by Builder/Reviewer, moved to PLANNING/ when prioritized.

---

## C# Detection Bugs (Found During Phase 1.2 Research)

### Bug 1: GOG detection checks exact filename
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** Checks for `goggame.info` (exact filename match). Real GOG files are `goggame-<id>.info` — prefix match needed.
- **Impact:** GOG games not detected.
- **Suggested fix:** Change to prefix match: `Path.GetFileName(f).StartsWith("goggame-")`
- **Status:** Open

### Bug 2: EA detection needs __Installer/ directory check
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** Checks for `eaapp_` prefix / `.ea.web` / folder name. Real EA installs have a `__Installer/` directory.
- **Impact:** EA games not detected correctly.
- **Suggested fix:** Check for `__Installer/` subdirectory as primary EA signal.
- **Status:** Open

### Bug 3: Ubisoft detection needs uplay_install.manifest check
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** Checks for `ubisoft game launcher url` / folder name. Real detection needs `uplay_install.manifest` or `uplay_r*_loader*.dll`.
- **Impact:** Ubisoft games not detected correctly.
- **Suggested fix:** Check for `uplay_install.manifest` file or `uplay_r*_loader*.dll` pattern.
- **Status:** Open

### Bug 4: Recursive Directory.GetFiles performance issue
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** `Directory.GetFiles("*", SearchOption.AllDirectories)` is recursive — should use root-level scan.
- **Impact:** Slow scanning on large game folders.
- **Suggested fix:** Use `SearchOption.TopDirectoryOnly` for initial scan.
- **Status:** Open

---

## Phase 1.1 Known Issues

### UI Command Buttons Are Decorative
- **Discovered:** 2026-04-17 (Phase 1.1 completion)
- **Where:** MainWindow command bar
- **Issue:** All command buttons have `IsHitTestVisible="False"` — cannot be clicked. Only 6 of 10 F-key buttons exist (F1, F2, F3, F5, F9, F10).
- **Status:** Open
- **Note:** Keyboard handlers exist for F2, F9, T, Enter, Backspace, arrows. Missing: F1, F3, F4, F5, F6, F7, F8, F10 handlers.

### Default settings/games files not created alongside exe
- **Discovered:** 2026-04-17
- **Where:** App startup
- **Issue:** Default `settings.json` and `games.json` should be created alongside exe for clean installs.
- **Status:** Open

---

## EA Format Caveat
- **Discovered:** 2026-Q1
- **Where:** docs/research/ea_format.md
- **Issue:** EA format doc based on staged install only — needs verification against a complete EA game install.
- **Status:** Open

---

## SDK Upgrade
- **Discovered:** 2026
- **Where:** Project-wide
- **Issue:** Currently on .NET 8. Plan to upgrade to .NET 9 exists at planning/90-sdk-upgrade.md.
- **Status:** Open
