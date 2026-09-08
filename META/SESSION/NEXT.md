# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** Builder. Read before implementing.
**Updated:** 2026-09-08

---

## Status

Plan **`planning/124-anchor-library-game-db.md`** — Anchor-based Library + Game Database (two-file split).

- App implementation COMPLETE (`libraries.json` anchors + flat `games.json`; Steam single-anchor bootstrap; scan-context + re-anchoring).
- Test alignment COMPLETE (all old path-anchored APIs removed from tests; rewritten to anchor API).
- **Full solution build: 0 errors.** `dotnet test`: App 394 + Core 156 + Migration 1 = **562 pass / 0 fail**.
- Plan 123 (detection) closed previously: Python matrix 22/22, baseline 13 clean.

## Next task

1. **Review gate for Plan 124** — Reviewer: verify app code against `docs/DATA-FORMAT.md` schema (libraries.json + games.json), anchor linkage, single-`Steam`-anchor semantics, and the pruned/updated test suite. Log any tech debt in `META/BACKLOG/TECH_DEBT.md`.
2. **Plan completion** — Planner: on review pass, move `planning/124-anchor-library-game-db.md` to `META/COMPLETED/`, update `META/ROADMAP.md`, and set the next backlog item.

## Standing hard rules (unchanged)

- No regex / line-based VDF parsing — Steam bootstrap is structural (libraryfolders.vdf = path locator only; ACF+folder scan = content authority).
- No hardcoded path/store-name detection rules; no filter changes without corpus evidence.
- `DisplayName` via `UpdateGameEntry` is the one deliberate metadata VFS-write exception (identity-guarded, `TitleSource`).
- Windows paths are opaque strings in C# code; no Linux-specific filesystem logic.
- Do not reintroduce `LibraryRoot`-style path anchors; games link by anchor name, physical path lives in `GameEntry.FolderPath`.