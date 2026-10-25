# Task T116: Store Capability Matrix and Verification

**Tier:** 1 — Documentation
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** completed
**Prerequisites:** None

> Documentation-only. Do not run build/test (`META/RULES.md`).

## Objective

Publish one durable, verified capability matrix for all eight stores so launcher-discovery and
inventory work starts from facts, not assumptions. The former sub-sections (Steam/Epic,
GOG/EA/Ubisoft, Battle.net/Xbox/Rockstar) are folded into this single document.

## What Needs to Change

### 1. New file `docs/detection/10-store-capabilities.md`
- [ ] Purpose, legend (`verified` / `needs verification` / `unsupported`), and a one-line "how to use".
- [ ] Matrix rows: Steam, Epic, GOG, EA, Ubisoft, Battle.net, Xbox, Rockstar.
- [ ] Matrix columns:
  1. Launcher install located by the app
  2. Inventory source the launcher trusts
  3. Inventory read by the app
  4. Per-game metadata read
  5. Executable discovery
  6. Health states
  7. Startup incremental sync
  8. Read-only safety
  9. Evidence
- [ ] Per-store section: launcher mechanism, inventory source, what we read today, metadata, exe, health, gaps, verification status.
- [ ] A gap register per store.
- [ ] A "documentation contradictions reconciled" section (see Context).

### 2. Evidence
- [ ] Use `planning/129-store-capabilities-and-launcher-discovery.md` §2 as the starting point.
- [ ] Verify key claims with `grep` against `src/` and cite `file:line` or a `docs/research/*` doc.
- [ ] Mark anything not implemented or not verified explicitly; never claim "100%" without code.

### 3. Index and link
- [ ] Add it to `docs/detection/README.md`.
- [ ] Add it to the live map in `docs/README.md`.
- [ ] Link it from `docs/detection/09-blueprint.md` §14.

## Context

- Source matrix: `planning/129` §2 and the four read-only store audits.
- Contradictions to reconcile in the new doc:
  - stale "signal only" wording for Epic/EA/Ubisoft vs shipped parser code;
  - Epic `.item` **write** vs **registration**;
  - Battle.net `.battle.net/` vs `.build.info`/`.product.db` markers;
  - the two different `product.db` files (Agent SQLite vs game-root binary);
  - Steam registry-key variants (HKLM vs HKCU/`WOW6432Node`);
  - GOG default-path disagreement;
  - store-count drift and the omitted Xbox row in the roadmap phase list.
- Do not repeat unsupported "fully implemented" claims.

## Requirements

- [ ] All 8 stores present with all 9 capability columns.
- [ ] Every shipped cell cites evidence; unknown cells say `needs verification`.
- [ ] The reconciliation section lists each contradiction and its resolution.
- [ ] No task IDs in the document.

## Verification

- [ ] `test -f docs/detection/10-store-capabilities.md`.
- [ ] Manual check: 8 store rows × 9 capability columns.
- [ ] Every link in the new doc and the two indexes resolves.
- [ ] `grep -nE '\bT[0-9]{2,3}[A-Z]?\b' docs/detection/10-store-capabilities.md` returns no task IDs.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Published `docs/detection/10-store-capabilities.md` with purpose/legend/how-to-use, an 8-store × 9-capability matrix, per-store sections (mechanism, inventory source, what we read, metadata, exe, health, gaps, verification status), per-store gap registers, a seven-item "Documentation contradictions reconciled" section, and cross-store observations. Added the doc to `docs/detection/README.md` and the live map in `docs/README.md`, and linked it from `docs/detection/09-blueprint.md` §14.
- **Verification:** File exists. Matrix has 8 store rows and 9 capability columns (plus the store-name column). All relative links in the new doc and both indexes resolve. No task IDs match `\bT[0-9]{2,3}[A-Z]?\b`. No build/test run (docs-only). 13 matrix cells use `needs verification`.
- **Issues encountered:** The matrix is transposed relative to `planning/129` §2 (stores as rows per the task requirement). GOG has no single default path; three distinct documented values were reconciled rather than picking one. Xbox was missing from the roadmap Phase 3 store list but is shipped in detection; recorded as an eight-store set. No STATUS.md edited.
