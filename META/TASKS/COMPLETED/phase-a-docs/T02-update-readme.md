# Task T02: Update README.md

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~15 min
**Risk:** Low
**Status:** completed

---

## Objective

Bring `README.md` up to date with the actual project state. The current file says "Phase 0 bootstrap" (the project is in Phase 2), references `.sisyphus/plans/` (superseded), and lacks build/test instructions.

## What Needs to Change

- **File:** `/home/malware/projects/gamingCommander/README.md`

### Specific Changes

1. **Line 9** — Replace "The repository is in Phase 0 bootstrap." with the current phase: "The repository is in Phase 2: Steam & Standalone Games." (per `META/SESSION/CURRENT.md`)

2. **Lines 11–16** — Update "Current work focuses on" list. Replace with actual current work:
   - Standalone game detection and metadata collection
   - Steam library scanning and ACF cross-referencing
   - Executable scoring and noise filtering
   - UI polish and theme system

3. **Lines 34–40** — Update "Planned Solution Layout" to match actual structure:
   ```
   src/
     GamingCommander.Core/
     GamingCommander.Detection/
     GamingCommander.Migration/
     GamingCommander.UI/
     GamingCommander.App/
   tests/
   tools/
   docs/
   data/
   ```

4. **Lines 42–46** — Update "Key Documents" section:
   - Keep `AGENTS.md` and `CONTRIBUTING.md`
   - Replace `.sisyphus/plans/` with `META/SESSION/CURRENT.md` and `planning/`
   - Add `META/ARCHITECTURE.md`
   - Add `META/ROADMAP.md`

5. **Add** a "Build & Test" section:
   ```markdown
   ## Build & Test

   ```bash
   dotnet build
   dotnet test
   ```

   Requirements: .NET 8 SDK. See `docs/development-environment.md` for details.
   ```

## Context

- `META/SESSION/CURRENT.md` says phase is "Stabilization & Feature Completion"
- `META/ROADMAP.md` shows Phase 2 active
- `.sisyphus/plans/` is deprecated (being archived in T03)
- Tests confirmed: 17 passing (5 Core + 1 Migration + 11 App)
- `docs/development-environment.md` exists with Linux dev setup details

## Requirements

- [ ] Fix phase reference (Phase 0 → Phase 2)
- [ ] Update current work list
- [ ] Update solution layout to match actual structure
- [ ] Replace `.sisyphus/plans/` reference with `META/` and `planning/`
- [ ] Add build & test commands
- [ ] Keep file concise (target: <60 lines)

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests)
- [ ] README.md no longer references "Phase 0" or `.sisyphus/plans/`
- [ ] All links in README resolve to existing files

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Updated README.md with 5 changes:
  1. Phase reference: "Phase 0 bootstrap" → "Phase 2: Steam & Standalone Games"
  2. Current work list: replaced bootstrap items with detection/scanning/UI work
  3. Solution layout: added project names and descriptions
  4. Key Documents: replaced `.sisyphus/plans/` with META/ and planning/ references
  5. Added Build & Test section with `dotnet build` / `dotnet test` commands
- **Verification:** Build clean, 17 tests passing, all 7 linked files/directories exist
- **No issues encountered.**
