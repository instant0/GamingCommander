# Detection Logic Documentation Index

Fresh reference set (2026-09-07). The Python scanner (`tools/detect.py`) is the ground truth;
behavior is validated here FIRST, then ported to C#.

| Doc | Content |
|-----|---------|
| `01-overview.md` | **START HERE.** The decision pipeline, all gates in order, working-set reduction, confidence tiers. |
| `02-noise-filtering.md` | Noise exe/dir classification, 23-tier blacklist, `epicgames` special case, non-game folder names, parent-bound dirs. |
| `03-store-signals.md` | Store marker detection for all 9 stores, BattleNet `.build.info` signal, tier preservation. |
| `04-containers.md` | Container / collection / publisher-wrapper logic, stray-files fix, deep-nested fix, dropped P3c-2. |
| `05-executable-discovery.md` | Exe discovery order, the terminating rule (token + whole-name + backup guard), scoring signals, parent-bound promotion. |
| `06-deep-scan-fallbacks.md` | Deep signal scan, engine detection, `.lnk`, GOG metadata, non-game filter layers, UE-aware filter, `install` audit. |
| `07-csharp-parity.md` | C# ↔ Python gaps, which 2026-09-07 fixes must be ported, port order. |
| `08-validation.md` | Verification tools, current green results, fixture layout, real corpora, regression rules. |

Related: `planning/123-detection-bugfixes.md` (the plan), `planning/103-detect-py-port-status.md`
(parity matrix), `META/SESSION/CURRENT.md` (session state).