# Task T70: Windows Smoke Gate

**Tier:** 3 — Validation
**Phase:** H — MVP
**Effort:** ~30 min
**Risk:** Minimal
**Status:** Pending
**Prerequisites:** T61–T69 all Complete
**WP:** WP-5

---

## Objective

Final validation gate for MVP. Run the application on a Windows machine with real or mock game libraries and verify all 10 acceptance criteria from Plan 100 §1. Record results in `META/SESSION/CURRENT.md`. Do NOT record private paths — use generic references.

## Validation Checklist

### Build & Publish
- [ ] `dotnet build` passes (0 errors, 0 warnings)
- [ ] `dotnet test` passes (all tests green)
- [ ] `dotnet publish src/GamingCommander.App/GamingCommander.App.csproj -c Release -r win-x64 --self-contained false -o ./publish` succeeds

### Startup & Wizard
- [ ] App starts on Windows without crashes
- [ ] First run: wizard appears (or F2 Library Setup is available)
- [ ] Add a Steam library root → games list populates
- [ ] Add a standalone games folder → multi-store games appear

### Launch
- [ ] Select a Steam game → Enter/F5 launches via `steam://rungameid/{appid}` (Steam client reacts, overlay appears)
- [ ] Select a standalone game → Enter/F5 launches the primary `.exe` with correct working directory
- [ ] Standalone game with args → args are passed to the process

### Edit & Rescan
- [ ] F4 on a game → edit display name / source / exe path / args → save → relaunch uses new values
- [ ] F6 rescan after folder change → game list refreshes

### Visual Feedback
- [ ] Steam Installed games → green status color
- [ ] Steam Moved games → yellow status color + detail text
- [ ] Steam Missing/Orphaned games → red status color + detail text

### Stability
- [ ] No unhandled exceptions on startup
- [ ] No crash on missing folders, empty roots, or games with no exe (status message only)
- [ ] Status bar messages are readable and actionable

## Instructions

1. On a Windows machine, run the published app
2. Walk through each checklist item above
3. Mark `[x]` for pass, `[ ]` for fail
4. Record results in the Completion Notes section below
5. Update `META/SESSION/CURRENT.md` with MVP status (READY or blockers)
6. If any P0 item fails, create a new task to fix it before declaring MVP READY

## Context

- This is a human validation step — no code changes unless a failure is found
- If a failure is found, log it in `META/BACKLOG/TECH_DEBT.md` and create a follow-up task
- Do not record private library paths in any documentation
- The checklist mirrors Plan 100 §3 WP-5 and §1 acceptance criteria

## Requirements

- [ ] All checklist items pass
- [ ] Results recorded in Completion Notes
- [ ] `META/SESSION/CURRENT.md` updated with MVP status

## Verification

- [ ] All 15 checklist items marked `[x]`
- [ ] `META/SESSION/CURRENT.md` says "MVP READY" (or lists specific blockers)

## Completion Notes

- **Completed:**
- **MVP Status:** (READY / BLOCKED)
- **Results:**
  - Build & Publish:
  - Startup & Wizard:
  - Launch:
  - Edit & Rescan:
  - Visual Feedback:
  - Stability:
- **Issues encountered:**
