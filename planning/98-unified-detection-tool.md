# Plan 98 — Unified Game Detection Tool

**Status:** Completed
**Priority:** P0 (Stabilization — part of detection pipeline)
**Estimated effort:** 2–3 hours
**Depends on:** None (standalone Python tool, used for C# reference)

---

## Problem

We have **two Python scripts** with overlapping but divergent game detection logic:

| Script | Architecture | Signals | Features |
|--------|-------------|---------|----------|
| `list_standalone_games.py` | Fast-then-deep (correct) | GOG, EA, Ubisoft, Epic, Steam Emu | Container detection, needs_review flag |
| `detect_folder.py` | Walk-everything (slow, 57s) | All of above + **Blizzard, Xbox, Rockstar, Ubisoft Emu** | PE metadata, GOG metadata, engine detection, PCGW lookup, exe scoring, noise lists |

**Consequences:**
- Bugs fixed in one script don't transfer to the other
- Results diverge for the same folders (18 unrecognized vs 29 unknown on /mnt/d/games)
- When porting to C#, we'd have to pick one or merge — neither is complete alone
- `detect_folder.py` takes 57s because it does expensive operations (stat, PE parse, iterdir) on ALL folders, even clearly-identified ones

## Solution

Create **one unified tool** (`detect.py`) that combines:
- The **fast-then-deep architecture** from `list_standalone_games.py`
- The **full signal set + advanced features** from `detect_folder.py`

The key principle: **never do expensive work on folders you've already classified.**

---

## Architecture

```
detect.py <directory> [--metadata] [--pcgw] [--json]

Phase 1: FAST SCAN (all folders, ~3s on 120+ dirs)
  Root scan → signal match → classify → DONE
  
Phase 2: DEEP SIGNAL SCAN (unknowns only)
  Walk .exe/.dll/.ini only → find store signals → classify
  
Phase 3: CONTAINER CHECK (remaining unknowns)
  Check children for markers → recurse if container
  
Phase 4: ENRICHMENT (optional, --metadata / --pcgw flags)
  PE metadata → PCGamingWiki → name resolution
  Only runs on Phase 1-3 unknowns that still need identification
```

**Expected performance:** ~3s for Phase 1-3 (same as current `list_standalone_games.py`). Phase 4 adds ~1s per unknown folder (network-bound for PCGW).

---

## Implementation Steps

### Step 1: Create `tools/detect.py` with Phase 1 + Phase 2 + Phase 3

Merge the architecture from `list_standalone_games.py` with the signal set from `detect_folder.py`.

**Phase 1 — Root scan** (fast, no walk):
```
For each child directory in root:
  1. Scan root entries (os.scandir — one level, no stat)
  2. Check signals in priority order:
     a. GOG          — goggame* files
     b. EA           — __Installer/ dir
     c. Ubisoft Emu  — uplay_loader* + .ini with Username=
     d. Ubisoft      — uplay_install.manifest, uplay_r*_loader*.dll
     e. Epic         — .egstore/ or .egsstore/ dir
     f. Blizzard     — .battle.net/ dir
     g. Xbox         — default-metadata.json
     h. Rockstar     — title.rgl
     i. Steam Emu    — steam_api64.dll / steam_api.dll
  3. If signal found → DONE (Tier 1: HIGH confidence)
  4. If root .exe found (non-noise) → DONE (Tier 2: LOW confidence)
  5. Else → unknown, proceed to Phase 2
```

**Phase 2 — Deep signal scan** (unknowns only, extension-filtered):
```
Walk unknowns to WALK_MAX_DEPTH (4 levels):
  - ONLY process .exe, .dll, .ini files (skip everything else)
  - Collect: store signals + exe names
  - No stat() calls, no PE parsing
  If signal found → classify as Tier 1
  If exe found → classify as Tier 2
  Else → proceed to Phase 3
```

**Phase 3 — Container check** (remaining unknowns):
```
For each remaining unknown:
  Check if any child directory has launcher markers
  If yes → it's a container, recurse into children
  If no → flag as needs_review, proceed to Phase 4 (if enabled)
```

### Step 2: Add Phase 4 enrichment (optional flags)

**`--metadata` flag** — PE metadata extraction:
```
For needs_review folders only:
  1. Find executables (root + UE layout paths)
  2. Score by: folder-name match, shipping/win64 bonus, file size
  3. Parse PE metadata (FileDescription, ProductName, CompanyName)
  4. Use as game name candidates
```

**`--pcgw` flag** — PCGamingWiki lookup:
```
For needs_review folders only:
  1. Build name candidates from:
     - Folder name (normalized)
     - PE metadata (FileDescription, ProductName)
     - Executable stem names
  2. Query PCGW OpenSearch API (rate-limited, 0.6s interval)
  3. Score results by: name similarity, store-ID match, release year
  4. Cache results to data/pcgw_cache/
```

**`--json` flag** — output format (default: summary table, --json: full JSON).

### Step 3: Add engine detection

From `detect_folder.py`, port the engine detection functions (all fast, root-level only):
- Unreal Engine: `Engine/` dir with `Binaries/`
- Unity: `UnityPlayer.dll` + `*_Data/` child
- RAGE: `title.rgl` + `common.rpf`
- Frostbite: `Engine.BuildInfo_Win64_retail.dll`

Store as `engine` field in result dict.

### Step 4: Merge noise/blacklist lists

Combine the best of both:
- `list_standalone_games.py` `SKIP_EXE_SUBSTR` (130+ JSON-sourced patterns)
- `detect_folder.py` `NOISE_EXE_PARTS` (21 tiers, 90+ patterns)
- Deduplicate, keep the union
- Also merge `NOISE_DIR_PARTS` for directory filtering

### Step 5: Add GOG metadata extraction

From `detect_folder.py`, port the GOG `.info` file parsing:
- Read all `goggame-*.info` JSON files
- Prefer entry where `gameId == rootGameId` (main game)
- Extract: name, primary exe path from `playTasks`
- Only for GOG-detected games

### Step 6: Add Steam/Epic library exclusion

From `list_standalone_games.py`, keep the `--steam-libraries` flag.
Add `--epic-manifests` flag to exclude Epic launcher manifest dirs.

### Step 7: Test against both drives

Run unified `detect.py` against `/mnt/d/games` and `/mnt/e/games`:
- Verify same results as current `list_standalone_games.py` (3-4s)
- Verify unknowns match expectations
- Run with `--metadata` and `--pcgw` on unknowns to validate enrichment
- Compare with `detect_folder.py` output to ensure no feature loss

### Step 8: Document and deprecate

- Update `tools/README.md` (if exists) to document the unified tool
- Mark `detect_folder.py` as deprecated in its docstring
- Mark `list_standalone_games.py` as deprecated in its docstring
- Keep both files for now (don't delete — reference for C# porting)

---

## Feature Mapping (detect_folder.py → detect.py)

| Feature | detect_folder.py | detect.py | Notes |
|---------|:---:|:---:|-------|
| Root signal detection | 9 stores | 9 stores | Same + Ubisoft Emu added |
| Deep signal detection | steam_emu.ini, UbiStats, UE layout | Same | Extension-filtered walk |
| Container detection | 1-level child scan | Same | |
| Noise filtering | 21-tier exe + dir lists | Merged | Union of both lists |
| Engine detection | 4 engines | 4 engines | Ported as-is |
| GOG metadata | .info parsing | Optional | Part of enrichment |
| Exe scoring | Size + name tokens | Optional | Part of enrichment |
| PE metadata | pefile library | Optional (`--metadata`) | Only for unknowns |
| PCGW lookup | OpenSearch only | Full pipeline (`--pcgw`) | Uses lookup_metadata.py patterns |
| Epic metadata | Not in detect_folder | Future | Epic GraphQL via lookup_metadata.py |

---

## CLI Interface

```bash
# Fast scan only (default, ~3s)
python tools/detect.py /mnt/d/games

# With PE metadata enrichment (slower, ~1s per unknown)
python tools/detect.py /mnt/d/games --metadata

# With PCGamingWiki enrichment (slowest, network-bound)
python tools/detect.py /mnt/d/games --pcgw

# Full enrichment (metadata + PCGW)
python tools/detect.py /mnt/d/games --metadata --pcgw

# JSON output (for C# comparison testing)
python tools/detect.py /mnt/d/games --json

# Exclude Steam libraries
python tools/detect.py /mnt/d/games --steam-libraries /mnt/d/SteamLibrary/steamapps
```

---

## Success Criteria

1. **Performance:** Phase 1-3 completes in ≤5s on /mnt/d/games (115+ dirs, 124K files in EVE)
2. **Feature parity:** All 9 store signals from detect_folder.py present
3. **Feature parity:** Engine detection, GOG metadata, noise filtering present
4. **Enrichment:** PE metadata and PCGW work on unknowns when flags enabled
5. **Output:** JSON format matches structure needed for C# reference implementation
6. **No regression:** Known games detected same as current list_standalone_games.py

---

## C# Porting Notes

This unified tool becomes the **reference implementation** for the C# `FolderScanner` and future `GameDetector` classes. Key design decisions to carry forward:

1. **Two-phase architecture:** Fast root scan → deep scan only for unknowns
2. **Extension-filtered walks:** Only .exe/.dll/.ini in deep scans
3. **Signal priority order:** GOG → EA → Ubisoft → Epic → Blizzard → Xbox → Rockstar → Steam Emu
4. **Container detection:** Only promote children with launcher markers (not bare exes)
5. **Enrichment as separate layer:** PE metadata and PCGW are optional, run only on unknowns
6. **Noise lists:** JSON-loaded (already in C# via `data/blacklist.json`)

---

## Files Modified/Created

| File | Action | Purpose |
|------|--------|---------|
| `tools/detect.py` | **Create** | Unified detection tool |
| `tools/detect_folder.py` | Deprecate (keep) | Reference for PE/PCGW features |
| `tools/list_standalone_games.py` | Deprecate (keep) | Reference for fast-then-deep architecture |
| `META/SESSION/NEXT.md` | Update | Add plan 98 |
| `META/SESSION/CURRENT.md` | Update | Track progress |
