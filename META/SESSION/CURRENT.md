# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** All agents. Read every session.  
**Updated:** 2026-09-06 — Plan 123 reframed: thinking phase (paper spec + corpus classification) inserted before any detection code.

---

## This session

1. **Phase 0 complete** (T1 Steam LOCKED declaration, T2 Epic Missing-Manifest spec) — earlier this session.
2. **T3 parity audit complete** — `planning/103-detect-py-port-status.md` refreshed: C# is a superset for product features but missing two Python analysis layers (`_is_non_game_folder` child-all-non-game + file-type; deep 4-level signal walk).
3. **Plan reframed** to put the "thinking phase" first: §2.1 decision model, §2.2 exclusion taxonomy, §2.3 scenario catalog (S-A..S-J), §2.4 probe contract. Phases renumbered (Phase 1 = paper spec + corpus analysis; probe/diagnostics moved to Phase 2; experiments Phase 3; port Phase 4; tests Phase 5).
4. **Phase 1 started — corpus inventory complete.** User provided full listing `/mnt/r/full-d.txt` (12,193 file lines; superset of the 719-line exe sample). Copied as harness fixture `testdata/samples/full-d.txt`. Aggregate analysis (no paths published):
   - 106 third-level folders, 103 with exe/store signal; 95 have >1 exe; only ~39% of exes sit directly in the game folder.
   - 9 container parents (Blizzard, Origin, Epic Games, SSI, Stardock, …) vs ~15 real deep-nested games → confirms S1/S3 (parent promotion is the failure).
   - ~48 of ~90 store signals sit **one level below** the game folder → store probes must go to depth ≤ 2.
   - **S4 CONFIRMED (corrected):** penumbra's only real exe (`PENUMBRA.EXE`) is in `redist/`. The earlier "S4 refuted" claim used parent-dir-only noise analysis and missed the root `oalinst` installer. Corrected method: exe-name + parent-dir noise both checked. E5 must test redist-subfolder fallback admission.
   - **Design answer:** detection is staged collect→decide→register/escalate (structural → store → container → exe → title → enrich), now written into §0.3.
   - Decision model + taxonomy + scenario catalog updated with these corrections (§2.1/§2.2/§2.3/§0.3).
5. **Phase 2 — `--probe` implemented (additive, 554 lines).** `python tools/detect.py --probe <folder>` renders the full §0.3 decision chain (rule → evidence → kept/rejected candidates with reasons → next → tier) per §2.4 contract. Verified on Steam (Locked), Epic (Secure), standalone (Candidate), container (Unknown, parent not promoted), penumbra/ELEX fixtures. **Exposed a real ordering flaw:** container analysis must run BEFORE the non-game filter, or every container is rejected (S1/S2). §0.3/§2.1 corrected; `scan_directory` regression-checked (4 games, unchanged); `test_detection_patterns.py` unchanged (29 pre-existing issues).
6. **Phase 2 — §2.4.7 validation matrix DONE.** `tools/validate_probe_matrix.py`: one fixture per scenario S-A..S-J (realistic names in `testdata/mock/probe/`) asserting tier + primary exe. **Result: 9 PASS + 1 DIVERG.**
   - Container-before-filter corrected (S1/S2).
   - Platform-child rule: `system/`/`bin/`/`win64/` children belong to the parent game — `Elex` → Candidate, `system/ELEX.exe` primary (S3/E4). This is a probe-model fix (not production code).
   - S-F/S4 recovery: `Penumbra` → Candidate with `redist/PENUMBRA.EXE` primary (E5 target).
   - S-G DIVERG: bare `"tool"` not penalized in Python `_TOOL_NAMES` OR C# `tier_10_dev_editor_tools` → nondeterministic tie (E6 finding).

**Plan 123 status:** Phase 0 COMPLETE; Phase 1 (paper spec + corpus analysis) complete except the **review gate**; Phase 2 probe + validation matrix DONE. **No production detection code changed — probe is additive diagnostics only; model corrections recorded as experiment inputs (E4/E5/E6).**

---

**Next session: Read `META/SESSION/NEXT.md`.**