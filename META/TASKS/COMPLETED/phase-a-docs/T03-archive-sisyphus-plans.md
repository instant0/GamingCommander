# Task T03: Archive .sisyphus/plans/

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~10 min
**Risk:** Low
**Status:** completed

---

## Objective

Archive the deprecated `.sisyphus/plans/` directory. This is a legacy planning system superseded by `planning/` + `META/`. Two parallel planning systems create confusion.

## What Needs to Change

- **Move:** `/home/malware/projects/gamingCommander/.sisyphus/plans/` → `/home/malware/projects/gamingCommander/META/COMPLETED/.sisyphus-plans-archive/`
- **Update:** Any references to `.sisyphus/plans/` (T02 handles the README reference)

## Context

- `.sisyphus/plans/` contains 14 files (overview.md, phase-0.md through phase-4.md, sdk-upgrade.md, README.md)
- `planning/README.md` explicitly says "Project memory, session state, roadmap, and architecture have moved to `META/`"
- `README.md` line 46 references `.sisyphus/plans/` (T02 will fix this)
- No code files reference `.sisyphus/plans/`
- These are historical artifacts — preserve, don't delete

## Requirements

- [ ] Verify no code files reference `.sisyphus/plans/`
- [ ] Move `.sisyphus/plans/` to `META/COMPLETED/.sisyphus-plans-archive/`
- [ ] Verify `planning/` has equivalent or newer content for all plans
- [ ] Ensure `README.md` no longer references `.sisyphus/plans/` (depends on T02)

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests)
- [ ] `.sisyphus/plans/` no longer exists at original location
- [ ] `META/COMPLETED/.sisyphus-plans-archive/` contains all 14 files
- [ ] `grep -r "\.sisyphus/plans" . --include="*.md"` returns no hits (or only the archive itself)

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Moved 14 files from `.sisyphus/plans/` to `META/COMPLETED/.sisyphus-plans-archive/`. Removed `.sisyphus/` directory entirely.
- **Verification:** Build clean, 17 tests passing, no live references to `.sisyphus/plans/` remain
- **No issues encountered.**
