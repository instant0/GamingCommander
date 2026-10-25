# Plan 128: Documentation & Task-Backlog Audit

**Created:** 2026-10-25
**Priority:** P0 (state correctness) / P1 (detection)
**Status:** IN PROGRESS — audit complete; first wave authored (`T81`–`T88`, `T102`, `T103`).
**Source:** four read-only sub-agent audits covering `planning/`, `META/TASKS/`, `docs/`, and META governance, plus `docs/detection/09-blueprint.md` §14.

---

## 0. Outcome

- **Next product/code task:** `T102` — O9 title canonicalization (detector P1).
- **Before it:** a short documentation/state hygiene wave, `T81`–`T88` (docs-only).
- **Blocked on user decisions:** `T103` SteamEmu semantics, `T99` T72/T73, `T110` Plan 127 C, `T101` EA caveat.
- **Root cause of drift:** plans and trackers recorded intent at creation time and were not updated when the C# detection port, Plan 127, and the detector review shipped.

## 1. Verified findings (by area)

### 1.1 Task trackers (`META/TASKS/`)
- Phase-h T61/T69/T70/T71/T75: tracker says Complete, task headers say Pending (completion notes present).
- Phase-g T48/T50/T51: shipped, but docs say pending; T51 still targets the removed `FileSystemHelper` path helper.
- Phase-f T42: its own condition is already met, but the doc says pending.
- Phase-i: T73 row has no task doc; T74 is superseded; T72 describes removed `F9`, already-shipped engine work, and the old tier scheme.
- Phase-j: T80 header says Pending while the tracker says Deferred; T79 still instructs the deleted `PlatformMetadataKeys`.
- Phase-d/e/f/h are effectively closed but remain in the active tree (repo convention: move whole phases to `COMPLETED/`).
- `TEMPLATE/task-template.md` still mandates `**Effort:**` (forbidden by `AGENTS.md`) and stops at Tier 3.

### 1.2 Planning docs (`planning/`)
- `123`: status still lists tier marking / E6a / 3-level fallback as open (all shipped); `IdentificationTier` marked "NOT implemented".
- `103`: name-only / not-ported claims are false; superseded by `docs/detection/07`.
- `04-phase-2-syncmove`: uses deleted `PlatformMetadata`/`PlatformMetadataKeys` and back-compat defaults.
- `124`: says "Active" although COMPLETE; folder-derived `Id` deviation unrecorded.
- `110`: says "DRAFT" although Phases 1–5 shipped; still uses `List<string> Tags`.
- `102`: Phase 2 marked pending; old tag model; non-existent files; 1140 lines.
- `111`: no status; Tasks 2–3 shipped; references `PlatformMetadata`.
- `05-phase-3-category-browse`: no status; obsolete F8/S/F9, `GameEntry.Extra`, `ILibraryManager`.
- `04-phase-2-metadata-lookup`: superseded, but 518 lines of obsolete design remain.
- `114`: duplicated analyses (768 lines).
- `126-facts-eosh-re` / `121`: stale "in progress"; 121 omits the T78/EOSH limitation.
- `README.md`: stale statuses; omits phase-j/k/l trackers; `nn-` vs `nnn-` inconsistency.

### 1.3 Product docs (`docs/`)
- Epic write status is contradictory (README/ONLINE say writes; research says deferred; code still has `EpicItemWriter`).
- `ONLINE-AND-DATA.md`: `settings.json` wrongly owns library roots; omits `libraries.json`/`collections.json`/`ScanDepth`.
- `DATA-FORMAT.md`: still says "target/intended"; omits `settings.json`.
- `GAME-DETECTION-LOGIC.md` (1073): duplicates `detection/01`–`06`; uses removed `PlatformMetadata`.
- `EPIC-MANIFEST-ENRICHMENT.md` (661): self-contradictory and predates shipped Plan 109.
- `research/epic_item_format.md`: mixes read-only format with deferred regen recipes.
- `detection/05`,`07`: stale test counts/scoring deltas; `02`: wrong tier range; `03`: Steam bootstrap marked "planned"; `09`: self-contradictions.
- `docs/README.md` has no entry for `detection/README.md`.

### 1.4 Governance (`META/`)
- `NEXT.md` is prose that mixes completed work and directives; its only pending tracker row (T72) is P2.
- Plan 123 review-gate status differs across `CURRENT`, `NEXT`, `T79`, and blueprint §15.
- `CURRENT`/`NEXT` label Epic engine detection P1; the blueprint says P2.
- `CODE_MAP` missing `DetectionNoise`/`ICollectionsService`; stale `Extra` fields; 217-test snapshot.
- `ROADMAP` missing Plan 127 A/B; `ARCHITECTURE` missing the 2026-10 decisions/ADRs.
- `TECH_DEBT` last verified 2026-08-22; Bug 12 is fixed; per-tag colour shipped.
- `IDEAS` has shipped/dead entries without annotations.

## 2. Backlog (one deliverable per task)

| ID | Task | Priority | Depends |
|----|------|----------|---------|
| T81 | Reconcile task statuses + fix template | P0 | — |
| T82 | Archive closed phase dirs | P0 | T81 |
| T83 | Refresh `META/CODE_MAP.md` | P0 | — |
| T84 | ROADMAP + Plan 127 completion record | P0 | — |
| T85 | Refresh `TECH_DEBT` + `IDEAS` | P1 | — |
| T86 | `planning/README.md` + tracker table | P1 | T81 |
| T87 | Plan 103 parity + Plan 123 residual text | P1 | — |
| T88 | Superseded/partial plan status pass | P1 | — |
| T89 | Plan 124/126/121 status alignment | P1 | — |
| T90 | Split Plan 102 | P2 | — |
| T91 | Rewrite Plan 110 shipped-vs-future | P2 | — |
| T92 | Dedupe Plan 114 | P2 | — |
| T93 | SyncMove plan + split T80 | P2 | — |
| T94 | Epic write-status truth pass | P1 | — |
| T95 | Persistence docs (`ONLINE`, `DATA-FORMAT`, `FAQ`) | P1 | — |
| T96 | Shrink `GAME-DETECTION-LOGIC` | P2 | — |
| T97 | Archive `EPIC-MANIFEST` + split `epic_item_format` | P2 | — |
| T98 | Detection docs corrections (`05/07/02/03/09/README`) | P1 | — |
| T99 | Rewrite T72/T73/T74 (user decision) | P2 | T81 |
| T100 | 4-level walk spike (docs-only) | P3 | — |
| T101 | EA caveat verification (user data) | P3 | user |
| **T102** | **O9 title canonicalization** | **P1** | — |
| T103 | SteamEmu ADR (user decision) | P1 | user |
| T104 | Epic engine detection | P2 | — |
| T105 | O3 Epic signal + dead-code removal | P2 | — |
| T106 | O4/O5 store-signal dedup | P2 | — |
| T107 | O8 path normalizer | P2 | — |
| T108 | O10 Epic cheap-exe | P3 | — |
| T109 | Structured `DetectionLogger` | P3 | — |
| T110 | Plan 127 C decision (user) | P3 | user |
| T111 | Launch-profile picker UI | P2 | — |
| T112 | `Relations.Kind` wiring | P2 | — |
| T113 | Collections UI | P2 | — |
| T114 | Play-time tracking | P3 | — |

Task docs authored in this session: **T81–T88**, **T102**, **T103**. The rest are specified here and will be authored when their wave starts, so each doc stays current and small.

## 3. Execution order

```
Hygiene: T81 → T82 → T83/T84/T85 → T87/T88/T89 → T86
Product: T102 (O9) → T103 (SteamEmu ADR; blocked) → T104 (Epic engine)
Docs:    T94 → T95 → T98 → T96/T97
Cleanup: T90/T91/T92/T93 → T99 → T100/T101
Later:   T105–T109 → T110–T114
```

`T102` may run in parallel with hygiene (it touches code, not those docs), but read its tracker after `T81`.

## 4. Decisions needed
1. **SteamEmu (`T103`):** (a) keep `steam_api*.dll` → `SteamEmu`; (b) require `steam_emu.ini`/`steam_settings/`; (c) add a distinct `Steamworks` kind.
2. **T72/T73 (`T99`):** keep, defer, or cancel now that F8 filtering shipped.
3. **Plan 127 C (`T110`):** build, defer, or drop the `Extensions{}`/emulator model.
4. **EA caveat (`T101`):** provide a complete EA install for verification, or mark it blocked.

## 5. Success criteria
- Every active task doc's `**Status:**` matches its tracker row.
- No active doc describes removed types (`PlatformMetadata`, `GameRoot`, `GameEntry.Extra`).
- `planning/README.md` and `META/CODE_MAP.md` resolve every path they list.
- The detector P1 backlog exists as task docs with objective verification.
- `NEXT.md` is an ordered queue with one clear next task.
