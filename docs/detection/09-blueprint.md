# Detection Blueprint — Full Scope, Overlaps, and Sync Plan

**Status:** Created 2026-10-14.
**Audience:** Anyone changing detection (C# or Python).
**Authority model:** `tools/detect.py` is the **reference** used to test ideas first; the C# scanners
**ship**. Steam is a **separate locked system**. Where the two disagree, this doc records the
intended direction (port Python→C# for correctness, port C#→Python for parity).

This is the single map of the detector. Per-area detail stays in `01`–`08`; this file is the index
plus the overlap/sync backlog.

---

## 0. Implemented in this pass (2026-10-14)

**C# corrections (shipped detector):**
- **Deterministic tie-break** added at all three exe-selection sites (`FindPrimaryExecutable`,
  `FindParentBoundExe`, `FindExactFolderMatch`): score desc, then base filename asc.
- **Python scoring rules ported to C#** (`ScoreExecutable`): leading-dash backup −15; `_copy`/trailing
  `" copy"` −25; numbered underscore backup `N_org_` −15; specific tool binaries −25; `<500KB` −5;
  size bonus aligned to `min(size/10M, 10)`.
- **Container signal parity**: `ContainerScanner.Summarize` now also evaluates the deep fallback
  signals (SteamEmu deep, Ubisoft legacy, root `.lnk`) and promotes accordingly; a deep
  SteamEmu/Ubisoft marker is authoritative over the data-only layer.
- **Dead code removed**: `EpicManifestParser.NormalizePath` (private, unused). `EpicOrphanDiscovery` /
  `EpicStoreSearch` are unused but retained pending the deferred Epic work (see §11).
- **Tests:** +6 (tie-break, leading-dash, `_copy`, tool name, 100–500KB, container deep-SteamEmu).

**Python sync (reference):** C#-only scoring strengths back-ported — whole-name exact +40, classic
main-binary names +18, UE `-shipping` +28, path `\Binaries\win64\` +12 (both scorers). Validation
matrix still **22/22 PASS**.

**Still open (documented, not implemented):** O1/O2 full blacklist→scanner threading; O3–O10 dedup; store-priority alignment (**done**); SteamEmu false-positive refinement; engine detection for Steam/Epic; deep 4-level store walk; structured `DetectionLogger`.

**Added 2026-10-14 (P1 pass):** small detection noise sets centralized in `DetectionNoise`
(`NonGameExe`, `LnkSkip`, `SteamCommonNoise`); the container non-game layer and the terminating-rule
gate now share one `NonGameExe` set; registry store-priority aligned. Remaining P1: title
canonicalization (O9), SteamEmu design call, Epic engine detection.

---

## 1. Pipeline

### 1.1 Routing (`LibraryManager.SelectScannerAndScan`)
1. **Steam** — `LooksLikeSteamLibrary` (`steamapps/common`) or configured type Steam → `SteamLibraryScanner`.
2. **Epic** — configured type Epic or `EpicItemCatalog.LooksLikeManifestsDir` → `EpicLibraryScanner`.
3. **Default** → `FolderScanner`.

### 1.2 `FolderScanner.Scan` pass chain (per immediate child)
Pre-filters: hidden names → `IsNoiseDirectory` (substring, blacklist) → `NoiseSubDirNames` (exact) →
`IsNestedSteamTree`.
1. **Pass 1 — store signal** (`StoreSignalDetector.DetectType`) → register.
2. **Pass 1c — registry fallback** (`RegistryFallbackDetector`) → register.
3. **Pass 1.5 — exact folder-name terminating rule** (`FindExactFolderMatch`) → register Standalone.
4. **Pass 1.6 — parent-bound child promotion** (`FindParentBoundExe`) → register Standalone.
5. **Pass 2 — fallback signals** (`FallbackSignalDetector.DetectFallbackType`) → register.
6. **Pass 3 — container recursion** (`ContainerScanner.ScanContainerChildren`).

### 1.3 `AddGameEntry` enrichment
`FindPrimaryExecutable` → force-exe → `.lnk` fallback → `FindLauncherExecutable` → id →
`StoreMetadataEnricher` (GOG/EA/Epic/BattleNet) → `TitleEnricher` → `EngineDetector` →
`DetectionTierDeriver` → build `GameEntry`.

---

## 2. Identity signals per store

| Store | Signals (root unless noted) | Tier |
|-------|-----------------------------|------|
| Steam | structural `steamapps/common/<Game>` + ACF; `libraryfolders.vdf` = path locator only | Locked |
| Epic | global `.item` matched to install location; local `.mancpn`/`.item`/`.egstore`/`.egsstore` | Locked (global) / Secure (local) |
| GOG | `goggame*`, `gog_*`, `gog.ico`, `Launch *.lnk`; `goggame-*.info` (root + 1 subdir) | Secure |
| EA | `__Installer/`, `Touchup.exe`, `ActivationUI.exe` | Secure |
| Ubisoft Emu | `uplay_loader*`/`uplay_r*` + `.ini` with `Username=`/`AccountId=` | Secure |
| Ubisoft | `uplay_install.manifest`/`.state`, `uplay_r*loader*.dll`, `uplay_download/`, `*_UPP*.exe` | Secure |
| BattleNet | `.battle.net/`, `.build.info`, `.product.db`, `.patch.result` | Secure |
| Xbox | `default-metadata.json` | Secure |
| Rockstar | `title.rgl` | Secure |
| Steam Emu | `steam_api64.dll`/`steam_api.dll` (strong), `steam_appid.txt` (weak), deep `steam_emu.ini`, copied `steamapps/`+`.acf` (Python only) | Secure |
| Standalone | root exe / `.lnk` / UE layout | Candidate |

**Registry fallback:** EA, Ubisoft, GOG, Rockstar (exact-path + name tiering).

---

## 3. Tiers and taxonomy

- **Tiers:** `Unknown=0 < Candidate=1 < Secure=2 < Locked=3` (`IdentificationTier`); provenance in
  `DetectionInfo` (titleSource, signals, manifestKind, candidateExes). Store Secure/Locked is never
  downgraded by a later gate.
- **Exclusion taxonomy (7 buckets):** non-game folders (exclude); data-only (exclude); redist/installer
  (exclude deep + penalize); launchers/tools/editors (penalize); backup/copy/crack (penalize
  strongly); platform dirs (exclude as folders, never as games); ambiguous → Candidate.
- **Principle:** folder name is evidence, never authority.

---

## 4. Containers
Container = folder with no self-signal whose children have game signals. Parent is **never promoted**.
Organization = ≥2 game children → recurse. Max depth 2. Children are classified by their **own**
signals. See `04-containers.md`.

---

## 5. Exe discovery & scoring
Discovery: root → immediate children → UE `Binaries/{platform}` → `bin/` → recursive fallback
(`AppConfig.ScanDepth`, default 3) → `.lnk` fallback → GOG `.info`. Parent-bound promotion (E4/E5) and
the terminating rule (rel ≤4) are separate. Scoring is unified in `ScoreExecutable` (see `05`).

---

## 6. Titles
`TitleSource` order: user override → store manifest (GOG/EA/Epic) → PE FileDescription/ProductName
(guarded) → exe stem → folder. Edition stripping + acronym rules (E6). `PcgwPick`/`UserOverride`
survive rescan.

## 7. Engines
UE/Unity/RAGE/Frostbite (+ C# Source/Godot/CryEngine display tags). **Only `FolderScanner` runs
`EngineDetector`** — Steam/Epic rows get no engine until PCGW.

## 8. Catalogs / VFS
Steam ACF catalog (Installed/Moved/Missing/Orphaned); Epic `.item` catalog + orphan discovery;
virtual catalog per source type; anchors + flat `games[]` (Plan 124). `libraryfolders.vdf` is a
**path locator only** — ACF + folder scan is the content authority.

## 9. Diagnostics
Python `--probe` full chain (JSON) + `DetectionLogger`. C#: status messages only (no structured
chain) — a real parity gap.

## 10. Validation tooling
`validate_probe_matrix.py`, `baseline_compare.py`, `test_detection_patterns.py`,
`train_detection.py`, `selective_visits.py`, `decode_manifest.py`; fixtures `testdata/mock/probe/`;
corpora `full-d.txt`, `full-e.txt`, `full-p.txt`.

---

## 11. Overlaps / duplication (correction backlog)

These are real duplications of the *same rule* across components. They risk divergence.

| # | Duplication | Locations | Action |
|---|-------------|-----------|--------|
| O1 | Noise **exe** lists re-declared (setup/vcredist/dxsetup/oalinst/unins…) | `FolderScanner.DefaultNoiseExePatterns`, `ContainerScanner.s_noiseExePatternsForNonGame`, `ExecutableDiscovery.NoiseSkipForTerminating`, `SteamLibraryScanner.IsSteamNoiseExe`, `EpicLaunchResolver.NoiseExe`, `EpicItemWriter.IsEpicNoiseExe`, `ExecutableDiscovery.IsForbiddenLaunchExe`, `LnkParser.s_skipExeNames`, `data/blacklist.json` | Consolidate into one source (blacklist + a small shared forbidden set) |
| O2 | Noise **dir** lists re-declared | `FileSystemHelper.NoiseSubDirNames`, `blacklist.json directory_patterns`, `ContainerScanner.s_nonGameFolderNames`, `SteamLibraryScanner.s_nonGameCommonFolderNames`, `ExecutableDiscovery.ParentBoundChildNames` | Consolidate; keep parent-bound as its own intentional set |
| O3 | Epic `.egstore` detection | `StoreSignalDetector.HasEpicSignal`, `EpicOrphanDiscovery.HasEgstore`, `EpicManifestParser`, `ExecutableDiscovery.FindEpicManifest` | Single helper |
| O4 | Ubisoft detection in 4 places | `StoreSignalDetector` (×2), `FallbackSignalDetector.HasUbisoftLegacySignal`, `RegistryFallbackDetector`, `TitleEnricher` | Keep signals central; others consume |
| O5 | Rockstar `title.rgl` | `StoreSignalDetector.HasRockstarSignal`, `EngineDetector.HasRage` | Reuse |
| O6 | Blizzard product extraction called twice | `StoreSignalDetector.ExtractBlizzardProduct`, `StoreMetadataEnricher` | Pass value through |
| O7 | UE layout detection duplicated + platform list duplicated | `FallbackSignalDetector.HasUnrealLayoutSignal`/`s_uePlatformNames`, `EngineDetector.HasUnrealEngine`, `ExecutableDiscovery.s_uePlatformNames` | Single UE helper |
| O8 | Path normalization duplicated | `SteamAcfParser.NormalizePath`, `RegistryFallbackDetector.NormalizePath`, `EpicInstallPath.Normalize`, `EpicManifestParser.NormalizePath` (dead) | Single normalizer |
| O9 | Title canonicalization differs | `FileSystemHelper.NormalizeDisplayName` vs `TitleText.StripEdition` | Align edition lists |
| O10 | Cheap exe finder split | `SteamLibraryScanner.FindPrimaryExe`, `EpicItemWriter.FindLaunchExe`, `EpicLaunchResolver.Resolve` | One `CheapExeFinder` |

### Inconsistent rules (must be corrected)
- **Store priority differs:** `StoreSignalDetector` (GOG→EA→Ubi…) vs `RegistryFallbackDetector` (EA→Ubi→GOG…). Align.
- **Container children classified with fewer signals** than top-level: `ContainerScanner.Summarize` omits
  SteamEmu-deep / Ubisoft-legacy / root `.lnk` that Pass 2 uses → a child whose only evidence is those
  is silently dropped. **Corrected in this pass.**
- **SteamEmu false positive:** any `steam_api*.dll` ⇒ `SteamEmu`; legit Steamworks standalones mislabeled.
- **Engine detection only in `FolderScanner`** (Steam/Epic never get an engine).
- **GOG signal root-only** but GOG metadata root+1 subdir.

### Dead code
`EpicOrphanDiscovery`, `EpicStoreSearch`, `EpicManifestParser.NormalizePath` — no callers. Removed.

---

## 12. Two-way sync

### Python → C# (correctness gaps to port)
| Rule | Python | C# |
|------|--------|-----|
| Deterministic tie-break (score desc, then base name asc) | yes | **MISSING → ported** |
| Leading-dash backup `-Name.exe` −15 | yes | **MISSING → ported** |
| `_copy` / trailing `" copy"` −25 | yes | **MISSING → ported** |
| Numbered underscore backup `N_org_` −15 | yes | **MISSING → ported** |
| `_TOOL_NAMES` −25 (specific tool filenames) | yes | **MISSING → ported** |
| Size `<500KB` −5 | yes | **MISSING → ported** |
| Deep 4-level store-marker walk | yes | C# targeted probes only (intentional, LOW) |
| Copied `steamapps/`+`.acf` ⇒ SteamEmu | yes | C# skips nested Steam (intentional) |
| `DetectionLogger` structured chain | yes | C# status messages (LOW) |

### C# → Python (parity strengths to back-port)
| Rule | C# | Python |
|------|----|--------|
| Exact-stem +40 (folder-name match) | yes | +15 → **synced** |
| UE `-Shipping` +28 / path `\Binaries\win64\` +12 | yes | +5 → **synced** |
| Classic main binaries `game`/`start`/`play` +18 | yes | none → **synced** |
| PE noise penalties (desc −25, internalName −20, game-like +10) | yes | tiebreak only → **documented** |
| Tiered blacklist scoring (−30/−20/−10/−5) | yes | flat filter → **documented** |
| Registry fallback, store enrichers | yes | out of Python scope |

---

## 14. Prioritized backlog (evaluated 2026-10-14)

### Priority key
- **P0 — do now:** high impact, low risk, verifiable.
- **P1 — next:** clear value, moderate effort/risk (often needs a design call or fixture work).
- **P2 — later:** consistency/cleanup; do when the area is next touched.
- **P3 — not now:** low value, or an intentional design choice.

| # | Item | Impact | Effort | Risk | Prio | Decision |
|---|------|--------|--------|------|------|----------|
| O7 | UE platform-list dedup | Drift prevention | S | Low | **P0** | **DONE** — single `FileSystemHelper.UePlatformNames` |
| — | Store-priority alignment (registry) | Consistency | S | Low | **P0** | **DONE** — registry reordered to GOG→EA→Ubisoft→Rockstar |
| — | Plan 123 amended review gate | Process closeout | S | Low | **P0** | Close (see §15) |
| O1 | Noise **exe** list consolidation | Consistency | M | Med | **P1** | **PARTIAL DONE** — small sets centralized in `DetectionNoise`; container-L3 + terminating-rule sets unified to one `NonGameExe`. Full blacklist→scanner threading deferred (behavior change). |
| O2 | Noise **dir** list consolidation | Consistency | M | Med | **P1** | **PARTIAL DONE** — `SteamCommonNoise` centralized; `NoiseSubDirNames` already single. Container/parent-bound/blacklist dir sets remain distinct by design. |
| O9 | Title canonicalization (`NormalizeDisplayName` vs `TitleText.StripEdition`) | Title quality | S | Med | **P1** | Next — align edition lists; assert with title tests |
| — | SteamEmu false-positive refinement | Semantics | M | Med | **P1** | Design call required (see note) |
| O3 | Epic `.egstore` detection dedup | Cleanup | S | Low | **P2** | Later |
| O4 | Ubisoft detection dedup | Cleanup | S | Low | **P2** | Later (signals vs registry vs title are distinct concerns) |
| O5 | Rockstar `title.rgl` dedup | Cleanup | S | Low | **P2** | Later |
| O6 | Blizzard product extraction dedup | Perf/cleanup | S | Low | **P2** | Later |
| O8 | Path normalizer consolidation | Cleanup | M | Low | **P2** | Later (one dead copy already removed) |
| O10 | Cheap exe finder unification | Cleanup | M | Med | **P2** | Later (partially unified) |
| — | Engine detection for Epic rows | Feature | S | Low | **P2** | Later (Steam stays excluded for perf) |
| — | Deep 4-level store-marker walk | Recall | M | Med | **P2** | Later — bounded walk for unknowns only; measure perf first |
| — | Engine detection for Steam rows | Feature | S | Med | **P3** | Not now — deliberate perf exclusion (large libraries) |
| — | Structured `DetectionLogger` | Diagnostics | M | Low | **P3** | Not now — status messages adequate for shipped app |

### Notes on the P1 items

**O1/O2 — noise-list consolidation.** Today `FolderScanner` uses the tiered
`data/blacklist.json`; `SteamLibraryScanner`, `EpicLaunchResolver`, `EpicItemWriter`,
`ContainerScanner`, `ExecutableDiscovery.NoiseSkipForTerminating`, and `LnkParser` each hardcode a
small subset. Tokens like `setup`/`vcredist`/`dxsetup`/`oalinst`/`unins` appear 4–5 times.
*Plan:* expose one `ForbiddenLaunchExe` set + one `NonGameNoiseExe` set from `BlacklistData`, and pass
them to the scanners; keep the blacklist tiers for scoring only. For dirs, first reconcile the
**substring** (`IsNoiseDirectory`) vs **exact** (`NoiseSubDirNames`) semantics — they are deliberately
different today (`jdk-17` matches substring, not exact). **Risk:** changing which list wins can flip
detection outcomes, so do it behind the corpus matrix.

**O9 — title canonicalization.** `FileSystemHelper.NormalizeDisplayName` (display) strips
Remastered/Definitive/Enhanced/Ultimate/Special/GOTY/Edition; `TitleText.StripEdition` (search) strips
Reloaded/Definitive/Complete/Enhanced/Ultimate/Deluxe/Gold/Anniversary/GOTY. They are used in
different contexts, so alignment must preserve the `TitleSource` contract. **Do with title tests.**

**SteamEmu false-positive — design call.** `StoreSignalDetector` returns `SteamEmu` for any root
`steam_api64.dll`/`steam_api.dll` (and weak `steam_appid.txt`). This is **intentional** — the corpus
fixture `StoreSteamEmu` is built from `steam_api64.dll` and the matrix expects `Steam Emulator`. A
legit Steamworks standalone therefore groups as `SteamEmu`. Options:
(a) keep as-is (grouping by "uses Steamworks"); (b) require `steam_emu.ini`/`steam_settings/` for
`SteamEmu` and map `steam_api*.dll`-only to `Standalone`; (c) add a distinct `Steamworks` kind.
Requires a decision, then a fixture + Python `_check_steam_emu` update. **Do not change unilaterally.**

---

## 15. Plan 123 amended review gate — closed (agent, 2026-10-14)

The gate required reviewing the final Python scanner as the reference before further detection change.
Review outcome:
- **Ported (C#, 2026-09-07, 10 parity tests):** T1.5 terminating rule, GAP A whole-name, GAP B UE-wrap,
  E4/E5 parent-bound, BattleNet `.build.info`/`.product.db`/`.patch.result`, store-tier preservation,
  `epicgames` noise, UE-aware non-game filter (3 layers).
- **Closed 2026-10-14:** E6a bare `tool` (C# blacklist), 3-level fallback (doc error; `ScanDepth`),
  deterministic tie-break, Python scoring parity, container deep-signal parity.
- **Remaining, LOW/intentional:** deep 4-level walk, `DetectionLogger`.
- **Python matrix:** 22/22 PASS after the C#↔Python sync.

**Status:** REVIEWED (agent) — pending human sign-off.

---

## 16. Binding principles
No hardcoded path/store/publisher rules (signal/structure/scoring only); corpus-first; no speculative
filter removal; negative tests mandatory; Steam locked; folder name never authority; PE+cloud last
resort; privacy (aggregate-only corpora).
