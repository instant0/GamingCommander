# Plan 118 — Documentation Audit Refactor

**Nature:** Planning. Addresses findings from the 2026-08-07 librarian audit (refined 2026-08-10).  
**Audience:** Builder / Librarian.  
**Status:** ✅ COMPLETE — executed 2026-08-10.  
**Priority:** Was first gate before product work; done. Next product: Plan 117.  
**Scope:** Documentation only — no code, no `dotnet build` / `dotnet test`.

---

## 0. Goal

Restore a single trustworthy governance surface:

- TECH_DEBT matches what was actually fixed
- Session files match post-MVP reality (and list Plans 117–118)
- `planning/README.md` indexes all plans with correct status
- Path casing `planning/` is consistent on Linux
- Historical / bootstrap docs are archived, not mixed with live docs

---

## 1. Problems (verified)

### P1 — TECH_DEBT lies about Bugs 23–33 (HIGH)

CURRENT.md and NEXT.md state Plan 114 fixed Bugs 23–33. TECH_DEBT still has every one as `Status: Open` (Bug 31 is a duplicate of 26). Agents that prefer TECH_DEBT will re-implement solved fixes.

Also:

| Item | TECH_DEBT today | Truth |
|------|-----------------|--------|
| Bug 8 (F6 crash) | ✅ Fixed | Keep fixed — **do not** list as remaining Open |
| Bugs 17–19 (Epic) | ✅ Fixed (Plan 109) | Keep fixed |
| Bug 11 + Bug 15 | Both Open (same issue) | Merge; verify vs Plan 108 (status messages ✅ in session docs) |
| Bug 33 (tags not displayed) | Open | **Partially fixed** — right-pane colored tags shipped (Plan 102 Phase 4). Close for “tags missing”; left-pane layout residual → **Plan 117** |

### P2 — `PLANNING/` capitalization (LOW, narrow)

**Not** “nine files.” Verified hits:

| File | Occurrences | Action |
|------|-------------|--------|
| `AGENTS.md` | lines ~7, 78, 99 | `PLANNING/` → `planning/` |
| `META/BACKLOG/TECH_DEBT.md` | line 3 (header) | `PLANNING/` → `planning/` |
| `META/RULES.md` | — | **Already correct** — do not edit for this |

On Linux, uppercase `PLANNING/` is a broken path. Directory on disk is `planning/`.

### P3 — CURRENT.md is a 639-line session log (HIGH)

Contract: scratch, overwritten each handoff. Reality: ~400+ lines of completed session dumps. Belongs in `META/COMPLETED/`.

### P4 — Duplicates and Epic doc body lag (MEDIUM)

- **Bug 11 ≡ Bug 15** — Orphaned vs Missing UI confusion; keep one entry.
- **Epic:** TECH_DEBT 17–19 already ✅ Fixed; plan file 109 and Epic **header** say COMPLETE. **Body tables** in `docs/EPIC-MANIFEST-ENRICHMENT.md` still say “not implemented.” Fix the **stale body** (or add a short “Implemented” banner above the old gap tables). Do **not** re-open or re-edit 17–19 status in TECH_DEBT unless wrong.

### P5 — planning/README.md index is frozen pre-101 (HIGH)

- Plans **101–118** missing from the index
- `100-mvp-next-steps.md` still shown as **ACTIVE**
- Dead stubs `05-phase-3.md`, `06-phase-4.md` listed as future work
- `90-sdk-upgrade.md` still **PLANNED** (actually deferred indefinitely)
- Plan **117** / **118** absent

### P6 — NEXT.md stale (HIGH) — missing from original plan

Last updated ~2026-07-27 03:20, **before** Plan 117 (left-pane layout) and this plan. Still ranks Steam SyncMove as first incomplete P2 and never mentions 117/118. Timestamp cluster after tags work points at **Plan 117** as the live UI thread; SyncMove plan file is cold (2026-07-09). WP-C must rewrite NEXT, not only CURRENT.

---

## 2. Execution sequence

Work packages are ordered for risk and dependency. **A is mandatory first.** B and D are parallel-safe after A. C depends on A+D. E last.

```
WP-A (TECH_DEBT) ──┐
WP-B (casing)      ├──► WP-C (CURRENT + NEXT) ──► WP-E (archive orphans)
WP-D (README)    ──┘
```

| Order | WP | What | Why this order |
|------|-----|------|----------------|
| **1** | **A** | TECH_DEBT truth | Stops agents re-fixing B23–B33 |
| **2** | **B** | `planning/` casing | Tiny; AGENTS is read every session |
| **2** | **D** | planning/README index | Parallel with B; agents find 101–118 |
| **3** | **C** | CURRENT compact + **NEXT rewrite** | Needs correct debt + index so slim session files don’t re-encode lies |
| **4** | **E** | Archive orphans | Last — avoid breaking mid-edit links |

**Do not** start with CURRENT wipe or mass archive moves.

**Optional fast path:** complete WP-A only, then product work (e.g. Plan 117), then finish B–E later.

---

## 3. Work packages

### WP-A — TECH_DEBT clean-up

| File | Change |
|------|--------|
| `META/BACKLOG/TECH_DEBT.md` | Mark Bugs **23–33** (incl. 31 as dup of 26) `✅ Fixed — Plan 114` (+ verification date from CURRENT/Plan 114). |
| same | **Bug 33:** `✅ Fixed` for right-pane tags (Plan 102 Phase 4); note left-pane redesign residual → `planning/117-left-pane-layout.md`. |
| same | **Merge Bug 11 + Bug 15** into one entry. Verify Plan 108: if status detail strings are enough, mark fixed or “partial — UX polish remains”; do not leave two Open dups. |
| same | **Leave fixed:** Bug 8, 9, 14/16-style fixed entries, 17–19, 20–22 as already recorded. **Do not** flip 17–19. |
| same | Re-verify remaining Open set (see Success Criteria). Likely candidates: Bug 10, 12, 13, UI button hit-test, EA format caveat, and any other still-true items. Drop anything CURRENT proves fixed (e.g. Plan 108 if fully done). |

### WP-B — Path casing

| File | Change |
|------|--------|
| `AGENTS.md` | All `PLANNING/` → `planning/` (≈3 places) |
| `META/BACKLOG/TECH_DEBT.md` | Header “moved to PLANNING/” → `planning/` |
| `META/RULES.md` | **Skip** — already lowercase |

### WP-C — Session compaction + NEXT rewrite

| File | Change |
|------|--------|
| `META/SESSION/CURRENT.md` | Cut to **≤200 lines** (target ~100–150). Keep: Phase, Priority Roadmap (truthful), Known Issues (open only), Test Status, Key Architecture Decisions. |
| `META/COMPLETED/phase-post-mvp-sessions.md` | **NEW** — move bulk “Completed This Session” history from CURRENT (preserve history; do not delete). |
| `META/SESSION/NEXT.md` | **Overwrite** with post-MVP next actions. Must: drop completed MVP WP table noise or collapse to one line; list **Plan 118** if still in progress else mark done; include **Plan 117** (left-pane) as candidate next product work; keep SyncMove / 102 / 103 as backlog without claiming they are the sole “next” if 117 is the live thread; note deferred T48–T57. |

Suggested NEXT priority after 118 completes (product choice — confirm with human if needed):

1. Plan 117 — Left pane layout (freshest code/feedback thread)  
2. Steam SyncMove — `planning/04-phase-2-syncmove.md` (roadmap Phase 2.1)  
3. Plan 102 Phases 2–3 — engine + PCGW metadata  
4. Plan 103 / 104 / 101 — as today  

### WP-D — planning/README.md index

| File | Change |
|------|--------|
| `planning/README.md` | Index **all** plans on disk, including **101–118**. |
| same | Mark `100-mvp-next-steps.md` **COMPLETE** (not ACTIVE). |
| same | `05-phase-3.md`, `06-phase-4.md` → archived stubs / superseded (point at real plans 101, multi-launcher notes). |
| same | `90-sdk-upgrade.md` → **Deferred indefinitely**. |
| same | Active/Planned: 117 (DRAFT), 118 (this plan), SyncMove, 102 remaining phases, 103, 104, 101, etc. |
| same | Completed: 105–116, 109, 106, 114, tags display slice of 102, etc. as session docs already claim. |

Use a clear table; every `planning/*.md` file should be findable (success criterion is full coverage, not a specific markdown list syntax).

### WP-E — Orphan archive

Move (do not delete) into `META/COMPLETED/` (or a subfolder), and fix any remaining links:

| Source | Dest / note |
|--------|-------------|
| `META/SESSION/DETECTION_ANALYSIS.md` | `META/COMPLETED/detection-analysis-2026-07.md` — superseded by `docs/GAME-DETECTION-LOGIC.md` |
| `docs/input.md` | Archive — bootstrap; next steps done |
| `docs/development-environment.md` | Archive — Phase 0 env |
| `docs/ui-direction.md` | Archive — Phase 0 POC |
| `docs/ui-stack-decision.md` | Archive — Avalonia chosen and shipped |
| `docs/windows-validation-checklist.md` | Archive — pre-MVP checklist |
| `docs/F2-WIZARD-UNIFICATION-ANALYSIS.md` | Archive — Plan 106 verification; plan COMPLETE |

### WP-F — Epic doc body (optional but recommended)

| File | Change |
|------|--------|
| `docs/EPIC-MANIFEST-ENRICHMENT.md` | Align body with header: mark local parse / global cross-ref / FolderScanner integration as **implemented** (Plan 109). Keep API/GraphQL as deferred. Avoid leaving “❌ Not implemented” tables that contradict the header. |

---

## 4. Out of scope

- Any C# / XAML / test changes  
- Implementing Plan 117, SyncMove, or Plan 102  
- Rewriting `docs/GAME-DETECTION-LOGIC.md`  
- Deleting history (archive only)  
- Changing ROADMAP phase structure beyond what’s needed for CURRENT/NEXT accuracy  

---

## 5. Success criteria

1. **TECH_DEBT Open set is truthful**  
   - `Status: Open` only for bugs still unfixed after verify.  
   - Bugs **23–33** are not Open.  
   - Bug **8** remains Fixed (not listed as remaining Open).  
   - Bugs **17–19** remain Fixed.  
   - Single entry for Orphaned/Missing UX (11/15 merged).  
   - Bug **33** not “tags missing”; residual points at Plan 117 if needed.

2. **Casing**  
   - `grep -n 'PLANNING/' AGENTS.md META/BACKLOG/TECH_DEBT.md META/RULES.md` → no uppercase path refs (RULES already clean).

3. **CURRENT**  
   - `wc -l META/SESSION/CURRENT.md` ≤ **200**.  
   - History preserved under `META/COMPLETED/phase-post-mvp-sessions.md`.

4. **NEXT**  
   - Mentions Plan **117** and does not claim MVP is the active implement track.  
   - Does not omit 118 if 118 incomplete; after 118 done, next product candidate is explicit.

5. **planning/README.md**  
   - Every file under `planning/` is listed or explicitly archived/deferred.  
   - Includes **117** and **118**.  
   - MVP plan not marked ACTIVE.

6. **Docs-only**  
   - No source/test project edits; no build/test required (per `META/RULES.md` documentation-only sessions).

7. **Archives**  
   - Listed orphan docs no longer live under active `docs/` / `META/SESSION/` paths (moved to COMPLETED).

---

## 6. Verification commands (docs-only)

```bash
# Open bugs — inspect manually; expect no 23-33
grep -n "Status: Open" META/BACKLOG/TECH_DEBT.md

# Casing
grep -n 'PLANNING/' AGENTS.md META/BACKLOG/TECH_DEBT.md META/RULES.md || true

# CURRENT size
wc -l META/SESSION/CURRENT.md

# NEXT mentions 117
grep -n '117\|118\|SyncMove\|Post-MVP' META/SESSION/NEXT.md

# README coverage (spot-check)
ls planning/*.md | wc -l
grep -E '117|118|100-mvp|90-sdk' planning/README.md
```

---

## 7. Handoff

After completion:

1. Mark this plan **COMPLETE** in its header and in `planning/README.md`.  
2. NEXT.md points at the chosen product plan (default recommendation: **Plan 117**).  
3. Do not append another giant “Completed This Session” block to CURRENT — keep CURRENT short.
