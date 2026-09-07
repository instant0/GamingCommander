# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** Builder. Read before implementing.  
**Updated:** 2026-09-06

---

## Status

Plan **`planning/123-detection-bugfixes.md`** — Detection Tightening (confidence tiers, safe-identifications-first).

- **Phase 0 COMPLETE** (steam LOCKED declaration + Epic Missing-Manifest spec).
- **Phase 1 thinking phase IN PROGRESS.** Reframed 2026-09-06: **no detection code changes until the detection/exclusion model is written down and the real corpus is classified on paper, then reviewed.**
- T3 parity audit already complete (`planning/103-detect-py-port-status.md`). The next step is **not** code — it is the paper specification + corpus classification.

## Next task

Execute Plan 123 **Phase 2 — Diagnostics & baseline (tooling only; no production behavior change)**.
Status 2026-09-06:

- **`--probe` IMPLEMENTED and VERIFIED** (`tools/detect.py`, additive). Renders the §0.3 decision
  chain (rule → evidence → kept/rejected with reason → next → tier) per §2.4 contract.
- Probe **exposed a real ordering flaw**: container analysis must run BEFORE the non-game filter
  (else every container is rejected — S1/S2). §0.3/§2.1 corrected (container = stage 6, filter =
  stage 7); verified against PublisherCollection, Steam/Epic/standalone/penumbra/ELEX fixtures.
- `scan_directory` regression-checked (4 games, unchanged); `test_detection_patterns.py` unchanged.

Remaining in Phase 2:

1. **§2.4.7 validation matrix — DONE** (10/10 PASS after E4/E5; findings recorded in plan Phase 2).
2. **Selective folder visits — DONE** (14 symptom-driven visits, `tools/selective_visits.py`,
   JSON at `testdata/samples/selective-visits.json`; executed on a Windows machine).
3. **Baseline runs — DONE (Python side, 10/10 clean).** `tools/baseline_compare.py`; report:
   `testdata/samples/baseline-report.json`. C# side deferred to Phase 4 (Python-first method).
4. **E4/E5 production fixes — APPLIED in Python** (parent-bound children; base-name scoring;
   dash-backup penalty). Python is now the correct reference for detection logic.
5. **Findings + gate review — NEXT.** Present the decision model + taxonomy + scenario catalog +
   probe contract + baseline divergence for review.

Then: Phase 3 remaining experiments (E1/E2/E3/E6/E7/E8) → Phase 4 (C# port) → Phase 5 (tests).

**Experiment status:**
- **DONE in Python:** E4 (platform-child parent rule), E5 (redist fallback), deterministic tie-break, store-coverage fixtures (matrix 17/17), collection fixes (stray root files; deep-nested games via proof/path-divergence model).
- **E1** (collection/nesting): partial — stray-files + deep-nested collection handled; the **path-divergence/aggregate** signal ("many sibling game folders diverging from one parent ⇒ collection") noted as optimization candidate (avoid re-parsing confirmed collection parent).
- **E2** (store typing via child signals + global manifests): pending fixture work for child-of-container `.item`/ACF cross-ref.
- **E3** (nesting depth / 3-level fallback): the `_find_exe_in_subdirs` deep search now covers the deep-nested collection; UE-layout (2-level) still a gap.
- **E6a** (bare `"tool"` penalty): deferred pending corpus justification.
- **E6** (PE-title guard, S5/S9): C# title-selection logic; needs Windows PE data (P2.2).
- **E7/E8** (identity pipeline, title persistence): pending.

**Path-handling note (2026-09-06):** Windows `\` path normalization belongs ONLY to the Python
analysis tools (`selective_visits.py`, fixture parsing). C#/Windows code is untouched; Linux
fixture dirs are real directories scanned natively.

## Standing hard rule

Documented format/research wins. Ask before changing a documented field source or `.item` shape. Verify with `tools/decode_manifest.py` before more C# Epic writer changes. No hardcoded path/store-name detection rules. No blacklist/filter changes without corpus evidence. DisplayName via `UpdateGameEntry` is the one deliberate metadata VFS-write exception (identity-guarded, `TitleSource`) — Plan 123 §4.