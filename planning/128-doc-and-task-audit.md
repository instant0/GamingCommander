# Plan 128: Documentation & Task-Backlog Audit

**Created:** 2026-10-25
**Priority:** P0 (state correctness) / P1 (detection)
**Status:** COMPLETE (2026-10-25) — audit, hygiene wave, `CURRENT.md` reset, and O9 title canonicalization done. SteamEmu decision remains blocked.
**Source:** four read-only sub-agent audits covering `planning/`, `META/TASKS/`, `docs/`, and META governance, plus `docs/detection/09-blueprint.md` §14.

---

## 0. Outcome

- **Completed:** audit, documentation/state hygiene, `CURRENT.md` reset, and O9 title canonicalization.
- **Next unblocked work:** Epic engine detection (P2); author its task doc first.
- **Blocked on user decisions:** SteamEmu semantics, the phase-i navigation/filter question, the `Extensions{}` / emulator decision, and the EA caveat.
- **Root cause of drift:** plans and trackers recorded intent at creation time and were not updated when the C# detection port, typed persistence, and the detector review shipped.

## 1. Verified findings (by area)

### 1.1 Task trackers (`META/TASKS/`)
- Several phase-h and phase-g deliverables shipped, but their task headers and tracker rows disagree; one phase-g item still targets a removed path helper.
- A phase-f item's own condition is already met, but the doc says pending.
- Phase-i has a tracker row with no task doc, a superseded filter item, and a mode-switcher doc describing removed F9, already-shipped engine work, and the old tier scheme.
- Phase-j: a deferred SyncMove item's header says pending; the detection-closeout doc still instructs the deleted `PlatformMetadataKeys`.
- Phase-d/e/f/h are effectively closed but remain in the active tree (repo convention: move whole phases to `COMPLETED/`).
- `TEMPLATE/task-template.md` still mandates effort estimates (forbidden by `AGENTS.md`) and stops at Tier 3.

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
- `126-facts-eosh-re` / `121`: stale "in progress"; `121` omits the EOSH limitation.
- `README.md`: stale statuses; omits phase-j/k/l trackers; `nn-` vs `nnn-` inconsistency.

### 1.3 Product docs (`docs/`)
- Epic write status is contradictory (README/ONLINE say writes; research says deferred; code still has `EpicItemWriter`).
- `ONLINE-AND-DATA.md`: `settings.json` wrongly owns library roots; omits `libraries.json`/`collections.json`/`ScanDepth`.
- `DATA-FORMAT.md`: still says "target/intended"; omits `settings.json`.
- `GAME-DETECTION-LOGIC.md` (1073): duplicates `detection/01`–`06`; uses removed `PlatformMetadata`.
- `EPIC-MANIFEST-ENRICHMENT.md` (661): self-contradictory and predates the shipped local-manifest work.
- `research/epic_item_format.md`: mixes read-only format with deferred regen recipes.
- `detection/05`,`07`: stale test counts/scoring deltas; `02`: wrong tier range; `03`: Steam bootstrap marked "planned"; `09`: self-contradictions.
- `docs/README.md` has no entry for `detection/README.md`.

### 1.4 Governance (`META/`)
- `NEXT.md` mixes completed work and directives; its only pending tracker row is P2.
- Plan 123 review-gate status differs across `CURRENT`, `NEXT`, the detection-closeout doc, and blueprint §15.
- `CURRENT`/`NEXT` label Epic engine detection P1; the blueprint says P2.
- `CODE_MAP` missing `DetectionNoise`/`ICollectionsService`; stale `Extra` fields; 217-test snapshot.
- `ROADMAP` missing typed-persistence milestones; `ARCHITECTURE` missing the 2026-10 decisions/ADRs.
- `TECH_DEBT` last verified 2026-08-22; Bug 12 is fixed; per-tag colour shipped.
- `IDEAS` has shipped/dead entries without annotations.

## 2. Backlog (one deliverable per work item)

### Documentation and state hygiene
- Reconcile task statuses and fix the task template.
- Archive closed phase directories.
- Refresh `META/CODE_MAP.md` and record the typed-persistence milestone in `META/ROADMAP.md`.
- Refresh `TECH_DEBT` and `IDEAS`.
- Update the `planning/README.md` status/tracker table.
- Correct detection-plan parity and residual text; align superseded/partial plan statuses; align the anchor-library, EOSH, and Epic-catalog plan statuses.
- Split or rewrite oversized plans (game persistence, tags, detection logic); dedupe duplicated analyses.
- Rewrite the SyncMove plan and split its registration-repair scope.
- Truth pass on Epic write status; refresh persistence docs (`ONLINE`, `DATA-FORMAT`, `FAQ`); shrink the monolithic detection logic doc; archive the Epic manifest doc and split the `.item` format research.
- Correct stale detection docs (parity, executable discovery, tiers, Steam bootstrap, blueprint, detection README).

### Detection and code
- Title canonicalization (O9) — next product work.
- SteamEmu ADR — blocked on the user decision.
- Epic engine detection; Epic signal check and dead-code removal.
- Store-signal dedup; path normalizer; Epic cheap-exe improvement.
- Structured `DetectionLogger`; bounded 4-level store-marker walk spike.
- EA caveat verification — blocked on user data.

### Game model follow-ups
- `Extensions{}` / emulator model decision — blocked on the user.
- Launch-profile picker UI; `Relations.Kind` scanner wiring; collections UI; play-time tracking.

### Navigation
- Rewrite or retire the phase-i mode-switcher, flatten-view, and filter items now that filtering shipped.

Task docs are authored when their wave starts, so each doc stays current and small. Un-authored items remain specified here; do not invent task docs ahead of execution.

## 3. Execution order

```
Hygiene: reconcile statuses → archive closed phases → refresh reference docs → planning README
Product: title canonicalization → SteamEmu decision (blocked) → Epic engine detection
Docs:    Epic write truth → persistence docs → detection docs → shrink/archive monoliths
Cleanup: split/rewrite plans → phase-i navigation decision → spike/EA verification
Later:   detector cleanups → game-model follow-ups
```

Completed: hygiene → O9. Next: SteamEmu decision (blocked) → Epic engine detection.

## 4. Decisions needed
1. **SteamEmu:** (a) keep `steam_api*.dll` → `SteamEmu`; (b) require `steam_emu.ini`/`steam_settings/`; (c) add a distinct `Steamworks` kind.
2. **Phase-i navigation/filter:** keep, defer, or cancel now that filtering shipped.
3. **`Extensions{}` / emulator model:** build, defer, or drop.
4. **EA caveat:** provide a complete EA install for verification, or mark it blocked.

## 5. Success criteria
- Every active task doc's `**Status:**` matches its tracker row.
- No active doc describes removed types (`PlatformMetadata`, `GameRoot`, `GameEntry.Extra`).
- `planning/README.md` and `META/CODE_MAP.md` resolve every path they list.
- The detector P1 backlog exists as task docs with objective verification.
- `NEXT.md` is an ordered queue with one clear next task.
