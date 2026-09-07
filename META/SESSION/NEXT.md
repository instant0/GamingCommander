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

1. **§2.4.7 validation matrix — DONE** (9 PASS + 1 DIVERG; findings recorded in plan Phase 2).
2. **Selective folder visits** — use the Phase 1 inventory to choose which corpus folders need
   physical probing (PE reads, store-signal checks). Next step.
3. **Baseline runs** — Python vs C# over the scenario corpus; emit divergence report per scenario
   and per symptom (S1–S9).

Then: Phase 3 (experiments, Python) → Phase 4 (C# port) → Phase 5 (tests).

**Pending model corrections to carry into Phase 3 experiments:** E4 (platform-child parent rule —
probe fixed the model, port to container/exe discovery), E5 (redist fallback admission — penumbra
proves it), E6 (bare `"tool"` penalty — Python+C# both missing it).

## Standing hard rule

Documented format/research wins. Ask before changing a documented field source or `.item` shape. Verify with `tools/decode_manifest.py` before more C# Epic writer changes. No hardcoded path/store-name detection rules. No blacklist/filter changes without corpus evidence. DisplayName via `UpdateGameEntry` is the one deliberate metadata VFS-write exception (identity-guarded, `TitleSource`) — Plan 123 §4.