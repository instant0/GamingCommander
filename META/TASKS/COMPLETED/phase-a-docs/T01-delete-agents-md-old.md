# Task T01: Delete AGENTS.md.old

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~5 min
**Risk:** Minimal
**Status:** completed

---

## Objective

Remove the stale `AGENTS.md.old` backup file. It references a deprecated `.sisyphus/plans/` workflow and the "early planning/bootstrap stage" — both obsolete. `AGENTS.md` is the canonical agent guidance file and `AGENT.md` properly redirects tools that look for it.

## What Needs to Change

- Delete `/home/malware/projects/gamingCommander/AGENTS.md.old`

## Context

- `AGENTS.md.old` (178 lines) is a stale copy of an earlier AGENTS.md version.
- It references `.sisyphus/plans/` workflow (superseded by `planning/` + `META/`).
- It says "repository is in early planning/bootstrap stage" (now in Phase 2).
- `grep -r "AGENTS.md.old"` across the repo returns zero hits — nothing references this file.
- `AGENTS.md` (287 lines) is the current canonical file. `AGENT.md` (9 lines) is a pointer to it.

## Requirements

- [ ] Verify no file in the repo references `AGENTS.md.old` (already confirmed: grep returns empty)
- [ ] Verify `AGENTS.md` is intact and canonical
- [ ] Delete `AGENTS.md.old`

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests: 5 Core + 1 Migration + 11 App)
- [ ] `grep -r "AGENTS.md.old" .` returns no hits
- [ ] `AGENTS.md` exists and is unchanged

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Deleted `AGENTS.md.old` (178 lines, stale backup from earlier AGENTS.md version)
- **Verification:** Build clean, 17 tests passing (5 Core + 1 Migration + 11 App)
- **No issues encountered.**
