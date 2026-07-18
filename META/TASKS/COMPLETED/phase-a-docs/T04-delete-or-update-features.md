# Task T04: Delete or Redirect docs/FEATURES.md

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~30 min
**Risk:** Low
**Status:** completed

---

## Objective

`docs/FEATURES.md` is 1340 lines and severely stale (last updated 2026-05-31, 6+ weeks behind). It contains 10+ factually wrong entries. The recommended approach is to delete it and redirect to the canonical sources that are already maintained.

## What Needs to Change

- **Delete:** `/home/malware/projects/gamingCommander/docs/FEATURES.md`
- **Create:** A short redirect stub at the same path pointing to canonical docs

## Known Errors in Current FEATURES.md

| Line | Error | Correct Value |
|------|-------|---------------|
| 39 | `ILauncher Interface` marked "Complete" | Interface retired in plan 95; replaced by LibraryManager → FolderScanner/SteamLibraryScanner |
| 74 | `F1 Help` — "button exists, no handler" | F1 Help is implemented (ShowHelpAsync in MainWindow.axaml.cs) |
| 77 | `F4 Move` — "Move game folder with re-registration" | F4 is Edit/Retag (GameSetupWindow), not Move |
| 79 | `F6 Details` — "Show game details + PCGamingWiki lookup" | F6 is Refresh/Rescan, not Details |
| 83 | `F10 Quit` — "no handler" | F10 calls Close() — fully implemented |
| 44 | `IGameDiscoveryService Interface` — marked Complete | Deprecated stub, file contains only a comment |
| 359–361 | Steam ACF/VDF/Library listed as "Research" | Fully implemented in SteamLibraryScanner |
| 569–570 | Core.Tests: 1 test, Detection.Tests: 1 test | Core.Tests: 5, App.Tests: 11 (Detection.Tests is deprecated) |

## Recommended Approach

Replace the 1340-line file with a 10-line redirect:

```markdown
# GamingCommander — Feature Status

This file is deprecated. Feature status is now tracked in:

- **Current state:** [`META/SESSION/CURRENT.md`](../META/SESSION/CURRENT.md)
- **Roadmap:** [`META/ROADMAP.md`](../META/ROADMAP.md)
- **Architecture:** [`META/ARCHITECTURE.md`](../META/ARCHITECTURE.md)
- **Code structure:** [`META/CODE_MAP.md`](../META/CODE_MAP.md)
- **Completed work:** [`META/COMPLETED/`](../META/COMPLETED/)

Last updated: 2026-07-18
```

## Context

- `META/SESSION/CURRENT.md` (156 lines) is the living feature status document
- `META/ROADMAP.md` tracks phase progress
- `META/COMPLETED/` has detailed completion records per phase
- Keeping FEATURES.md in sync with 4 other documents is unsustainable

## Requirements

- [ ] Decide: delete-and-replace with redirect (recommended) or comprehensive update
- [ ] If redirect: create short stub pointing to canonical sources
- [ ] If update: fix all 10+ errors listed above
- [ ] Verify no other file links to `docs/FEATURES.md` (check README, planning docs, AGENTS.md)

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests)
- [ ] `docs/FEATURES.md` either contains correct info or is a clean redirect
- [ ] No broken links to FEATURES.md from other docs

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Replaced 1340-line stale `docs/FEATURES.md` with a 12-line redirect stub pointing to canonical sources (CURRENT.md, ROADMAP.md, ARCHITECTURE.md, CODE_MAP.md, COMPLETED/).
- **Verification:** Build clean, 17 tests passing, all 5 linked targets exist
- **No issues encountered.**
