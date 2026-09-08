# Detection Logic — 05: Executable Discovery & Scoring

**Reference:** Python `tools/detect.py` `_find_game_executables`, `_find_exe_in_subdirs`,
`_pick_best_root_exe`, `_pick_primary_executable`, `_find_exact_folder_match`.
**Audience:** Anyone touching exe discovery or primary-exe selection.

---

## 1. Discovery order

| Step | Function | Finds |
|------|----------|-------|
| Root scan | `_scan_root` | Non-noise exes at the folder root |
| UE fast-path | `_find_exe_in_subdirs` | `Engine/` + `<Game>/Binaries/{Win64,Win32,Steam,Linux}/`, or root `Binaries/{Win64,Win32,Steam,Linux}/` |
| Generic fallback | `_find_exe_in_subdirs` | Child dirs up to 3 levels (noise dirs skipped) |
| Launcher extension | Tier 2 | If the root exe is a launcher, find the real game exe deeper |
| `.lnk` resolution | `_find_exe_via_lnk` | The exe named inside a root `.lnk` (3-level search, exact + fuzzy) |
| GOG `.info` | `_extract_gog_metadata` | The `playTasks` primary exe path |

**Noise-dir note:** the terminating-rule match search does NOT skip noise dirs — the exact stem
match is the guard (covers `redist/PENUMBRA.EXE`).

---

## 2. The terminating rule (T1.5) — `_find_exact_folder_match`

**This is the highest-priority non-store gate.** If an exe at or below the folder has a stem that
exactly matches the folder name, the folder IS the game folder and that exe IS the game exe.

### Match forms (GAP A, 2026-09-07)

- **Token match:** exe stem == one folder token.
  `Neverwinter.exe` ↔ `Neverwinter_en` (tokens `{neverwinter, en}`).
- **Whole-name match:** exe stem, separators stripped, == folder name, separators stripped.
  `Diablo III.exe` ↔ `Diablo III`; `deadspace3.exe` ↔ `Dead Space 3`;
  `DungeonSiege2.exe` ↔ `Dungeon Siege 2`.
  (7 real corpus cases; without this the terminating rule missed them.)

### Backup exclusion (E5 guard preserved)

The following never match the terminating rule:
- Leading-dash backups: `-Penumbra.exe`
- `copy of X.exe`, `X - copy.exe`
- Numbered backups: `10 org X.exe`, `10101116_org_X.exe`

This keeps `redist/PENUMBRA.EXE` (the real game) winning over `redist/-Penumbra.exe`.

### Search bound

Bounded to relative depth ≤ 4 (81/102 real game exes are at rel 1–4; deeper is
noise/emulators/backups — NO WALK_MAX_DEPTH increase; a depth-6 walk was tried and reverted).

### Working-set effect

Once it fires, **deeper candidates are excluded from the working set** the client sees:
- `Neverwinter_en\Neverwinter\Live\x64\GameClient.exe` → never a candidate.
- `Diablo III\x64\Diablo III64.exe` + `x64 - Copy\Diablo III64.exe` → never candidates.

---

## 3. Scoring — `_pick_best_root_exe`

Used for root exes, parent-bound child exes, and deep exes. Applies penalties/bonuses then sorts
**deterministically** by `(-score, base_name)` (tie-break is alphabetical — no filesystem-order
nondeterminism).

| Signal | Score |
|--------|-------|
| Backup (`copy of`, `-copy`, `org`, `original`) | `-30` to `-40` (never picked if a clean exe exists) |
| Tool names (`_TOOL_NAMES`) | `-25` |
| Small exe (<100KB) | `-15` |
| Small exe (<500KB) | `-5` |
| Folder token match | `+10` per token |
| Exact stem match | `+15` |
| Abbreviation (short stems sharing first letter) | `+8` |
| Roman numeral match | `+12` |
| UE path bonus | `+5` |
| File size bonus | `+min(size/10M, 10)` |
| PE metadata (FileDescription/ProductName match) | `+15` / `+10` (top-3 only) |

**Deterministic tie-break (2026-09-06):** all three scorers sort by `(-score, base_name)` so ties
resolve alphabetically — reproducible across runs and filesystem orders.

---

## 4. Deep scoring — `_pick_primary_executable`

Scores the deep candidate set with the same signals plus a stronger PE-metadata tiebreak pass over
the top-3. Returns `(rel_path, pe_metadata, bat_launcher_paths)`.

---

## 5. Parent-bound exe promotion (E4/E5, 2026-09-06)

When the ONLY exes live inside a platform/build/redist child:

```
Elex\system\ELEX.exe        → parent Elex wins, primary system/ELEX.exe
Penumbra\redist\PENUMBRA.EXE → parent Penumbra wins, primary redist/PENUMBRA.EXE
```

The parent folder is the entry; the child exes are scored against the **PARENT** name. The child
(`system`, `redist`) is never promoted as a game.

---

## 6. Acronym PE-title relaxation (E6, 2026-09-07)

**Unblocked finding:** the plan assumed E6 needed Windows PE data (P2.2) — WRONG. `pefile`
(2024.8.26) reads PE version info **on Linux**; the real game drives are mounted at `/mnt/d` so the
actual PE metadata was read directly:

| Folder | Exe | Real PE FileDescription | Verdict |
|--------|-----|-------------------------|---------|
| `jag2` | `ja2.exe` | **"Jagged Alliance 2 Gold"** | ✅ genuine E6 case |
| `mmxl` | `Might and Magic X Legacy.exe` | *(no version resource)* | ExeStem handles it |

Also fixed `_read_pe_metadata` in detect.py — pefile returns `FileInfo` as a **list of lists**, so
the old flat iteration silently returned `{}` for every exe.

**The relaxation (C# `TitleText.AcronymMatchesTitle`):** for a SHORT acronym folder (3-4
letters+digits), the `SharesNameToken` guard is replaced by initial-letter matching. Three rules,
each validated against the real corpus PE data:

| Rule | Example |
|------|---------|
| (a) title key starts with folder key | `eve` ↔ "EVE Online", `nier` ↔ "NieR:Automata" |
| (b) folder key == word-initials (incl. digits, skip stopwords) | `mmxl` ↔ "Might and Magic X Legacy", `ra3` ↔ "Red Alert 3 Launcher" |
| (c) folder letters prefix first title word + digits present | `jag2` ↔ "Jagged Alliance 2 Gold" |

Rejected: `elexII` ↔ "System" (folder too long — falls back to SharesNameToken), `jag2` ↔
"Just Another Generic Game 2" (initials J-A-G-G). Wired into `FolderScanner.AddGameEntry` as
`SharesNameToken(pe) || AcronymMatchesTitle(pe, folder)`. 10 new tests; C# suite 559 green.

---

## 7. C# scoring differences (from parity doc)

- C# uses one unified `ScoreExecutable` (Python has two scorers).
- Exact-match precedence is stronger in C# (`+40` folder-name exact, ADR-012).
- C# adds PE noise penalties (`-25` noise desc, `-20` noise internal name), `+18` for
  classic names (`game`/`start`/`play`).
- Minor diffs: `copy of` `-25` vs Python `-30`; C# lacks the `<500KB -5` tier; C# file-size bonus
  capped lower.
- C# 3-level generic subdir fallback is NOT ported (only root + children + UE + `bin/`, recursive
  capped at 2) — a known gap feeding E3/E5.