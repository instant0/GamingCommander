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
- **E1** (collection/nesting): **COMPLETE in Python (2026-09-07).** Exact-match terminating rule (Tier 1.5 / probe stage 13) + working-set pruning + catalog/sibling signal + **P3c-2 DROPPED** (corpus disproved the single-child/proof-counter rules — production already handles all real single-child chains). Corpus-grounded fixes applied: **GAP A** whole-name match (`Diablo III`↔`Diablo III.exe`) with backup-exclusion guard; **GAP B** deep container check reuses `_find_exe_in_subdirs` (UE `Ashen/Binaries/Win64/` wrappers now recognized — were missed); **GAP C** `PublisherWrapper/` fixture (real SquareEnix/Stardock/qfg5/COD). Matrix **20/20 PASS**; baseline **12 scenarios 0 missed / 0 wrong-folder / 0 FP**.
- **E2 COMPLETE (2026-09-07):** BattleNet `.build.info`/`.product.db` typing (`_scan_root`), tier-preservation fix (store-Secure not downgraded by terminating rule), `StoreBlizzardBnet/` fixture. Matrix 22/22.
- **E3 COMPLETE (2026-09-07):** `epicgames` noise refinement (only launcher/bootstrap variants — real `IndianaEpicGameStore-Win64-Shipping.exe` no longer wrongly filtered); probe `_platform_child_has_game_exe` descends 2 levels (UE `Binaries/Win64/`); `install` substring audited safe; Neverwinter re-verified. `UEShipping/Indiana` fixture (S-D4). Matrix 22/22, baseline 13 clean.
- **DOCUMENTATION (2026-09-07):** detection logic now documented in `docs/detection/` (01-overview, 02-noise-filtering, 03-store-signals, 04-containers, 05-executable-discovery, 06-deep-scan-fallbacks, 07-csharp-parity, 08-validation, README). CODE_MAP updated. Use these instead of re-deriving from txt/py files.
- **E2** (store typing via child signals + global manifests): **amended 2026-09-07 — Steam ACF OUT OF SCOPE** (Steam is a separate locked mechanism). Covers Epic `.mancpn`/`.item`, BattleNet `.build.info`.
- **E3** (nesting depth): **rewritten 2026-09-07** — premise overturned by depth analysis (real game exes at rel 1-4; deeper = noise/emulators). Do NOT raise WALK_MAX_DEPTH. Scope: validate depth-1-4 coverage, confirm UE-shipping rel-5 fast-path, verify `install` substring impact, ensure Neverwinter → rel-2 `Neverwinter.exe` (not nested `GameClient.exe`).
- **E6a** (bare `"tool"` penalty): deferred pending corpus justification.
- **E6** (PE-title guard, S5/S9): C# title-selection logic; needs Windows PE data (P2.2).
- **E7/E8** (identity pipeline, title persistence): pending.

**Phase 4 reordered 2026-09-07:** port the proven Python fixes FIRST (E4/E5/tie-break/collection rules), then remaining accepted experiments.

**SECOND CORPUS — full-p.txt (`P:\Program Files (x86)`, 18,106 lines):**
- **SEPARATE SCENARIO, do NOT mix into Plan 123 experiments.** Plan 123 stays scoped to the messy standalone `d:\games` corpus; fix it fully first.
- Store-client folders are **KNOWN FIXTURES** (Steam-style structural contracts): `GOG Galaxy\Games\<X>\` (13 games, `goggame-*.info` = Secure), `Ubisoft\Ubisoft Game Launcher\games\<X>\` (2 games), `Steam\steamapps\common\` (already locked), `EA\` (registry-driven, empty in snapshot).
- **Steam bootstrap:** `Steam\steamapps\libraryfolders.vcf` = master index of ALL Steam libraries on ALL disks → parse once, enumerate every `common\` + ACF; subsumes §8.1 ACF work.
- Non-fixture top-level folders (Microsoft SDKs, Visual Studio, Windows Kits, uTorrent) = tooling, excluded; generic scanner must NOT deep-dive them.
- **Next:** create a dedicated plan for §8.3 fixture-path handling AFTER Plan 123 completes (Steam libraryfolders.vcf bootstrap = step 2, highest value).

**Path-handling note (2026-09-06):** Windows `\` path normalization belongs ONLY to the Python
analysis tools (`selective_visits.py`, fixture parsing). C#/Windows code is untouched; Linux
fixture dirs are real directories scanned natively.

## Standing hard rule

Documented format/research wins. Ask before changing a documented field source or `.item` shape. Verify with `tools/decode_manifest.py` before more C# Epic writer changes. No hardcoded path/store-name detection rules. No blacklist/filter changes without corpus evidence. DisplayName via `UpdateGameEntry` is the one deliberate metadata VFS-write exception (identity-guarded, `TitleSource`) — Plan 123 §4.