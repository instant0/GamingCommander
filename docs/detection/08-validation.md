# Detection Logic — 08: Validation & How to Verify

**Reference:** Plan 123 Phase 2 + tools.
**Audience:** Anyone verifying detection behavior.

---

## 1. The three verification tools

| Tool | Command | What it proves |
|------|---------|----------------|
| **Validation matrix** | `python3 tools/validate_probe_matrix.py` | Each scenario fixture (S-A…S-J + store types) probes to the EXPECTED tier + primary exe. Exit 0 = all pass. |
| **Baseline** | `python3 tools/baseline_compare.py` | Production `scan_directory` vs the corrected model (`--probe`) over all scenario fixtures. Counts missed games, wrong-folder promotions, false positives. |
| **Detection-pattern test** | `python3 tools/test_detection_patterns.py <corpus-dir>` | Noise-filter accuracy, scoring accuracy (primary pick), backup penalty, store detection against REAL corpus text files. |

**Probe single folder:** `python3 tools/detect.py --probe <folder>` renders the full decision
chain (rule → evidence → accepted/rejected → next → tier). Stages 1–13; the terminating rule is
stage 13.

**Scan a tree:** `python3 tools/detect.py <root> --json` runs production `scan_directory`.

---

## 2. Current results (2026-09-07)

| Check | Result |
|-------|--------|
| Validation matrix | **22/22 PASS** |
| Baseline | **13 scenarios: 0 missed, 0 wrong-folder, 0 FP** + 2 collection regressions PASS |
| Detection patterns (original corpus `/projects/game-text`) | **1 issue** (was 29): noise 100%, scoring 67/67, backups 46/46 |
| Corpus store classification | 140 games through real logic (GOG 11, Epic 2, EA 2, Ubi 2, BN 1+, Rockstar 1, standalone 121) |
| C# build | 0 errors (4 pre-existing XAML warnings) |

---

## 3. Fixture layout

All scenario fixtures live in `testdata/mock/probe/`:

| Fixture | Scenario |
|---------|----------|
| `GameAlpha` | S-A valid standalone |
| `EpicGameGamma` | S-B store game |
| `PublisherCollection` | S-C container (3 children) |
| `ArcInstall/neverwinter_en` | S-D deep-nested game (terminating rule) |
| `ArcInstall` | S-D2 exact-match terminating / container |
| `PublisherWrapper/` | S-D3 real publisher wrappers (SquareEnix/Stardock/qfg5/COD) |
| `UEShipping/Indiana` | S-D4 UE-shipping deep exe (real: `IndianaEpicGameStore-Win64-Shipping.exe`) |
| `Elex` | S-E engine-subfolder (parent-bound) |
| `Penumbra` | S-F redist-only (E5) |
| `MultiRunner` | S-G multi-exe |
| `Mmxl` | S-H acronym |
| `NoSignal` | S-I no signal |
| `SoundtrackFolder` | S-J false game folder |
| `Store{Gog,Ea,Ubi,Blizzard,SteamEmu,Xbox,Rockstar}` | store-signal coverage |
| `StoreBlizzardBnet/Diablo III` | real BattleNet `.build.info` shape |
| `CollectionWithStray`, `CollectionDeepNest` | collection regressions |

**Adding a fixture:** create the real folder shape → add a matrix row
(`tools/validate_probe_matrix.py`) → add a baseline row (`tools/baseline_compare.py`) →
run both + `test_detection_patterns.py`.

---

## 4. Real corpora (harness input, aggregate-only)

| File | Source | Content |
|------|--------|---------|
| `testdata/samples/full-d.txt` | `D:\Games` | 12,193-line filtered listing |
| `testdata/samples/full-e.txt` | `E:\Games` | 2,467-line listing |
| `testdata/samples/full-p.txt` | `P:\Program Files (x86)` | 18,106-line filtered listing (SEPARATE scenario, Plan 123 §8.3) |

**Privacy rule:** these are aggregate analysis inputs only — no raw paths are published in docs.

**Corpus use:** `tools/train_detection.py <dir-of-txt-files>` runs the real detection logic over
them. `tools/selective_visits.py` emits the minimal Windows physical-visit list (PE reads,
manifest contents) for signals the listing cannot answer.

---

## 5. Regression rule (Plan 123)

- **No hardcoded path/store-name detection rules.** Every rule must be signal-/structure-based.
- **No blacklist/filter change without corpus evidence.**
- **Acceptance gate for experiments:** recall may rise only if false positives do not rise;
  negative tests mandatory.
- **Corpus-first, not theoretical:** only real occurrences (or very-likely-real shapes) justify
  behavior changes. P3c-2 was dropped because the corpus disproved it.