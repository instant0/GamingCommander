# Task T05: Clean ILauncher Ghost References

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~20 min
**Risk:** Low
**Status:** completed

---

## Objective

`ILauncher` interface was retired in plan 95 and replaced by the two-tier scanner architecture (`LibraryManager` → `FolderScanner`/`SteamLibraryScanner`). However, 4 documentation files still reference it without noting its retirement. Clean up these ghost references.

## What Needs to Change

### Files to Update

1. **`META/ARCHITECTURE.md` line 93**
   - Current: `Each launcher detector (Steam, Epic, GOG, EA, Ubisoft, Battle.net, Xbox) must be isolated behind \`ILauncher\` interface.`
   - Change to: Add a note that ILauncher was retired. Update the "Provider Detection Pipeline" section to reflect the current `LibraryManager → FolderScanner/SteamLibraryScanner` architecture.
   - Keep ADR-008 as-is (it's a historical record), but add a note at the bottom of the section.

2. **`META/COMPLETED/phase-1-core-ui.md` line 12**
   - Current: `Core interfaces: IGame, ILauncher, ILibraryManager, IConfigService, IGamesDatabaseService`
   - Change to: Note that `ILauncher` was retired and removed. Keep the entry as historical fact but add "(retired)" annotation.

3. **`META/ROADMAP.md` line 55**
   - Current: `Phase 1.0 | Dual-pane UI, config engine, IGame/ILauncher/ILibraryManager interfaces`
   - Change to: `Phase 1.0 | Dual-pane UI, config engine, IGame/ILibraryManager interfaces (ILauncher later retired)`

4. **`META/ADR/008-isolated-launcher-integrations.md` line 13**
   - Current: `Launcher-specific logic is isolated behind interfaces (\`ILauncher\`).`
   - Change to: Add a "Superseded" note. The ADR's intent (isolation) is preserved in the current architecture, but the specific `ILauncher` interface was replaced.
   - Do NOT modify the body — append a "Superseded" section at the end.

5. **`docs/FEATURES.md` line 39** — Handled by T04 (delete or update)

## Context

- `META/SESSION/CURRENT.md` line 149 already documents: "ILauncher retired — ADR-008 described ILauncher but the pragmatic two-tier scanner architecture replaced it"
- The concept of isolation is preserved — `FolderScanner` and `SteamLibraryScanner` are still isolated behind `ILibraryManager`
- ADR-008 should remain as a historical record; we only add a superseded note

## Requirements

- [ ] Add "(retired)" or "Superseded" annotations to all 4 files
- [ ] Do NOT delete ADR-008 or remove historical content
- [ ] Ensure the current architecture (LibraryManager → scanners) is correctly described
- [ ] Update `docs/FEATURES.md` only if T04 doesn't delete it

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -r "ILauncher" META/ --include="*.md"` shows only historical/annotated references
- [ ] No file claims ILauncher is "Complete" or actively used

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Updated 4 files with ILauncher retirement annotations:
  1. `META/ARCHITECTURE.md` — Replaced Provider Detection Pipeline section with current scanner architecture + retirement note
  2. `META/COMPLETED/phase-1-core-ui.md` — Added "(ILauncher later retired in plan 95)" annotation
  3. `META/ROADMAP.md` — Added "(ILauncher later retired)" to Phase 1.0 milestone
  4. `META/ADR/008-isolated-launcher-integrations.md` — Appended "Superseded" section preserving original body
- **Verification:** Build clean, 17 tests passing, all ILauncher references in META/ are now historical/annotated
- **No issues encountered.**
