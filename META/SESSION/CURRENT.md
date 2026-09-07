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
7. **Phase 2 — selective folder visits DONE.** `tools/selective_visits.py` → 14 symptom-driven visits covering S1–S6/S8/S9 (S7 is app-level), JSON at `testdata/samples/selective-visits.json`. Executed on a Windows machine (PE reads/manifest contents).
8. **Phase 2 — baseline divergence DONE (Python side).** `tools/baseline_compare.py` runs production `scan_directory` vs the corrected model (`--probe`) over the 10 scenario fixtures → **2 wrong-folder promotions (S3 Elex/system, S4 Penumbra/redist), 0 missed games, 0 false positives**. S1 (container recursion) and S2 (deep-nested) already work in Python. S-G confirmed: both production and probe pick `MultiRunnerTool.exe` (E6 tie). Report: `testdata/samples/baseline-report.json`. C# side deferred to Phase 4.
9. **E4/E5 PRODUCTION FIXES APPLIED (Python first).** After user explanation of the `Elex/system` flow-order bug:
   - `_PARENT_BOUND_CHILD_DIRS`: platform/build/redist children belong to the parent; no container trigger; exes collected as parent candidates → `Elex` → `system/ELEX.exe` (no "SYSTEM" game), `Penumbra` → `redist/PENUMBRA.EXE`.
   - `_pick_best_root_exe`: base-filename token matching + leading-dash backup penalty (`-15`).
   - Probe gained exact-stem `+15` to match production (S-G deterministic: `MultiRunner.exe`).
   - **Deterministic tie-break added to all three scorers** (`-score, base_name`): score ties no longer resolve by filesystem order — alphabetically-first wins reproducibly (generic, no hardcoded paths). Verified on a tie fixture (both input orders → `Aaa.exe`).
   - **Baseline now 10/10 clean; validation matrix now 17/17 PASS.** User-pick fallback (candidate list + UserOverrides) remains for ambiguous installs.
10. **Store-coverage gap CLOSED.** The matrix previously had only Epic as a store fixture; the corpus has 9 store types. Added GOG/EA/Ubisoft/Blizzard/SteamEmu/Xbox/Rockstar fixtures to `testdata/mock/probe/` + matrix → all Secure with correct store. Regression guard: detection fixes can no longer silently break store typing.
11. **E6 vs E6a clarified.** Corrected a mislabel: the S-G "bare tool" gap is **E6a** (scoring penalty), separate from **E6** (PE-title guard relaxation for acronym titles S5/S9). E6 is C# title-selection logic (`SharesNameToken` guard); Python has no equivalent (title = ExeStem/manifest). E6 needs PE metadata = Windows selective-visit data.
12. **Collection-folder fix (user model, 2026-09-06).** User explained the layered-library model: `D:\Games\` = base library, `epicgames\` = store collection folder, `snuffbox\binaries\snuffbox.exe` = first deep finding that proves `epicgames` is a collection (parent never an entry; children are the games). **Found & fixed a real bug:** the collection check was gated on `not has_files_at_root`, so a collection with stray root files (`EpicGamesLauncher.url`, `readme.txt`) was misread as one "Unknown" game and its children missed. Removed the gate — collection is now signaled by game-shaped children (child with deeper exe) regardless of stray files. Verified: stray-collection → both children found; clean 200-game collection (93ms); all existing fixtures unchanged. Added `testdata/mock/probe/CollectionWithStray/` + baseline regression check.
13. **Deep-nested collection fix (user model, 2026-09-06).** User's "1 proof / 2 proof" + "path divergence" model: the first game-shaped finding marks a collection tentatively, a second confirms it, and subsequent children are handled as game-folder bases. **Found & fixed:** a collection game with a DEEP exe (`monsterhunter/win64/binaries/monsterhunter.exe`) was missed (container data-only check only looked at direct children → wrongly skipped as "data-only"; then `win64` wrongly promoted). Fixed: (a) container data-only check now searches deep exes via `_find_exe_in_subdirs`; (b) deep container check skips parent-bound children; (c) parent-bound exe collection reaches 2 levels. Result: `monsterhunter` → `win64/binaries/monsterhunter.exe` (parent wins). Fixture `CollectionDeepNest/` + baseline check added. The path-divergence/aggregate collection signal noted as E1 optimization candidate.

**Plan 123 status:** Phase 0 COMPLETE; Phase 1 complete except the **review gate**; Phase 2 (probe + matrix 17/17 + selective visits + baseline) DONE; E4/E5 + collection fixes (stray files + deep-nested) applied in Python. **C# still untouched** — Python is the reference for correct detection logic first. Remaining: E1 (divergence/aggregate collection optimization), E2/E3, E6a, E6 (C# + Windows PE data), E7/E8, C# port (Phase 4).

---

**Next session: Read `META/SESSION/NEXT.md`.**