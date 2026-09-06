# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** Builder. Read before implementing.  
**Updated:** 2026-08-29

---

## Status

User live-smoke-test on `d:\games` reported 9 detection/identity failures. Plan **`planning/123-detection-bugfixes.md`** rewritten per review: **analysis-first, Python-first**. No speculative filter removal, no hardcoded store-folder/path rules (scanner must work on any system). Phase 1 = realign `tools/detect.py` with the C# scanner and measure both against a scenario corpus before designing any fix.

## Next task

Execute Plan 123 **Phase 1 — Realignment & baseline (analysis only, zero behavior change)**:

1. Parity audit both directions: `detect.py` (`_scan`, `_is_non_game_folder`, `_find_game_executables`, `_pick_primary_executable`, `_read_pe_metadata`, `_build_name_candidates`, `_pcgw_lookup`) vs C# (`FolderScanner`, `ContainerScanner`, `ExecutableDiscovery`, `StoreSignalDetector`, `FallbackSignalDetector`, PE enrichment, `TitleText`, `PcgwTitleFilter`); refresh `planning/103-detect-py-port-status.md`.
2. Add `--probe <folder>` decision-chain diagnostics to `detect.py`.
3. Build scenario corpus: `testdata/samples/d-games.txt` (user-provided sample EXE list — harness input only, no manual reads) + `testdata/mock/` + new fixtures as needed.
4. **Sample-list structure analysis (scripted, from the list alone):** per-path exe counts, nesting-depth histogram, exe-in-subfolder patterns (redist/system/bin/Binaries), acronym/locale stems, blacklist-noise ratio. Output = scenario pattern catalog for E1–E7 + selective-visit list (no "scan every exe").
5. Run Python vs C# over the corpus; produce divergence report (missed/extra entries, wrong exe picks, wrong titles/types) for S1–S9.
6. Write findings; **gate — no fixes designed until report is reviewed.**

## Standing hard rule

Documented format/research wins. Ask before changing a documented field source or `.item` shape. Verify with `tools/decode_manifest.py` before more C# Epic writer changes. No hardcoded path/store-name detection rules. No blacklist/filter changes without corpus evidence. DisplayName via `UpdateGameEntry` is the one deliberate metadata VFS-write exception (identity-guarded, `TitleSource`) — Plan 123 §4.