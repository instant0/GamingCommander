# detect.py Module Split Procedure

**Status:** PLANNED  
**Reference:** `tools/detect.py` (1829 LOC)  
**Goal:** Break the monolithic `detect.py` into focused, reusable modules while preserving identical behavior.

---

## 1. Why Split

`detect.py` is a well-structured 1829-line file with clear section markers (═══ headers), but it's a single file. Splitting it into modules provides:

1. **Maintainability** — each module is <400 LOC, focused on one concern
2. **Reusability** — other tools (e.g., `lookup_metadata.py`) can import shared logic
3. **Testability** — individual modules can be unit-tested in isolation
4. **Readability** — new contributors find logic faster

---

## 2. Module Map

The file splits into **8 modules** + **1 CLI entry point**:

```
tools/
├── detect/                    # NEW package directory
│   ├── __init__.py            # Re-exports scan_directory for backward compat
│   ├── signals.py             # Store signal checks (Phase 1)
│   ├── scoring.py             # Exe scoring + selection
│   ├── exe_discovery.py       # Exe finding in subdirs + .lnk parsing
│   ├── engine.py              # Engine detection (UE, Unity, RAGE, Frostbite)
│   ├── gog.py                 # GOG metadata extraction
│   ├── container.py           # Container detection + non-game folder logic
│   ├── enrichment.py          # Phase 4: PE metadata + PCGW + name candidates
│   ├── scanner.py             # Main scan pipeline (Phase 1-3 orchestration)
│   └── cli.py                 # CLI entry point + output formatting
├── detect.py                  # KEPT — thin wrapper for backward compat
├── detect_folder.py           # DEPRECATED (kept for reference)
└── ...
```

---

## 3. Module Details

### 3.1 `detect/signals.py` — Store Signal Checks

**Source lines:** 169–532 (SKIP_NAMES, _ROOT_SIGNAL_CHECKS, _scan_root inline checks)

**Exports:**
```python
SKIP_NAMES: set[str]
ROOT_SIGNAL_CHECKS: list[tuple[str, str, Callable]]
NOISE_EXE_PARTS: tuple[str, ...]
NOISE_DIR_PARTS: tuple[str, ...]

def is_noise_exe(name: str) -> bool
def is_noise_dir(name: str) -> bool
def scan_root(path: Path) -> ScanResult  # Returns (store, signal, has_exe, root_exe, has_lnk, child_dirs)
def check_gog(d: Path) -> bool
def check_ea(d: Path) -> bool
def check_ubisoft_emu(d: Path) -> bool
def check_ubisoft(d: Path) -> bool
def check_epic(d: Path) -> bool
def check_blizzard(d: Path) -> bool
def check_xbox(d: Path) -> bool
def check_rockstar(d: Path) -> bool
def check_steam_emu(d: Path) -> bool
```

**Dependencies:** None (pure stdlib)

### 3.2 `detect/scoring.py` — Exe Scoring

**Source lines:** 331–420 (_pick_best_root_exe) + 935–1043 (_pick_primary_executable)

**Exports:**
```python
def pick_best_root_exe(d: Path, exe_names: list[str]) -> str | None
def pick_primary_executable(d: Path) -> tuple[str | None, dict, list[str]]
    # Returns (relative_path, pe_metadata_dict, bat_launcher_paths)
```

**Dependencies:** `signals.py` (is_noise_exe), `enrichment.py` (read_pe_metadata)

### 3.3 `detect/exe_discovery.py` — Exe Finding

**Source lines:** 269–328 (_parse_lnk_exe_name, _find_exe_via_lnk) + 866–932 (_find_game_executables) + 1276–1364 (_find_exe_in_subdirs)

**Exports:**
```python
def parse_lnk_exe_name(lnk_path: Path) -> str | None
def find_exe_via_lnk(d: Path) -> str | None
def find_game_executables(d: Path) -> tuple[list[Path], list[Path]]
    # Returns (exe_candidates, bat_launchers)
def find_exe_in_subdirs(child: Path, child_dirs: list) -> list[str]
```

**Dependencies:** `signals.py` (is_noise_exe, is_noise_dir)

### 3.4 `detect/engine.py` — Engine Detection

**Source lines:** 765–804 (_detect_engine, _has_unreal_engine, _has_unity, _has_rage, _has_frostbite)

**Exports:**
```python
def detect_engine(d: Path) -> str
def has_unreal_engine(d: Path) -> bool
def has_unity(d: Path) -> bool
def has_rage(d: Path) -> bool
def has_frostbite(d: Path) -> bool
```

**Dependencies:** None (pure stdlib)

### 3.5 `detect/gog.py` — GOG Metadata

**Source lines:** 808–859 (_extract_gog_metadata)

**Exports:**
```python
def extract_gog_metadata(d: Path) -> dict | None
```

**Dependencies:** `signals.py` (is_noise_dir)

### 3.6 `detect/container.py` — Container + Non-Game Logic

**Source lines:** 1160–1273 (_NON_GAME_DIR_NAMES, _NON_GAME_SUBDIR_NAMES, _is_non_game_folder)

**Exports:**
```python
NON_GAME_DIR_NAMES: set[str]
NON_GAME_SUBDIR_NAMES: set[str]
NON_GAME_FILE_EXTS: set[str]
SUPPORT_FILE_EXTS: set[str]

def is_non_game_folder(d: Path, child_dirs: list) -> bool
```

**Dependencies:** `signals.py` (is_noise_exe)

### 3.7 `detect/enrichment.py` — Phase 4 Enrichment

**Source lines:** 862–1112 (_read_pe_metadata, _build_name_candidates, _pcgw_lookup) + 1662–1723 (_enrich_unknowns)

**Exports:**
```python
def read_pe_metadata(exe: Path) -> dict
def build_name_candidates(folder: str, d: Path, game: dict) -> list[str]
def pcgw_lookup(name: str) -> dict | None
def enrich_unknowns(games: list[dict], *, extract_metadata: bool, verify_pcgw: bool) -> None
```

**Dependencies:** Optional `pefile` import, `signals.py` (is_noise_exe, is_noise_dir)

### 3.8 `detect/scanner.py` — Main Scan Pipeline

**Source lines:** 1371–1659 (scan_directory, _scan recursive)

**Exports:**
```python
def scan_directory(
    root: str | Path,
    *,
    steam_library_paths: set[str] | None = None,
    extract_metadata: bool = False,
    verify_pcgw: bool = False,
    log_path: str | None = None,
) -> list[dict]
```

**Dependencies:** All other modules. This is the orchestrator.

### 3.9 `detect/cli.py` — CLI + Output

**Source lines:** 1726–1829 (_print_summary, main)

**Exports:**
```python
def print_summary(games: list[dict]) -> None
def main() -> None
```

**Dependencies:** `scanner.py`, `json`, `sys`

### 3.10 `detect/__init__.py` — Backward Compatibility

```python
"""Unified game detection tool — fast scan + deep enrichment."""
from .scanner import scan_directory

__all__ = ["scan_directory"]
```

---

## 4. Migration Strategy

### Step 1: Create Package (5 min)

```bash
mkdir tools/detect
touch tools/detect/__init__.py
```

### Step 2: Extract Modules (one at a time, test after each)

Order of extraction (bottom-up, no circular dependencies):

```
1. engine.py          — no deps, trivial, proves the pattern
2. gog.py             — no deps, simple
3. container.py       — depends on signals (is_noise_exe)
4. signals.py         — no deps, but everything depends on it
5. exe_discovery.py   — depends on signals
6. enrichment.py      — depends on signals, optional pefile
7. scoring.py         — depends on signals, enrichment
8. scanner.py         — depends on all above
9. cli.py             — depends on scanner
```

**For each extraction:**

1. Create the module file
2. Move the relevant functions/classes
3. Add imports from dependencies
4. Update `__init__.py` if needed
5. Run: `python tools/detect.py /path/to/test --json | head` — verify identical output
6. Run: `python -m tools.detect.scanner /path/to/test` — verify module import works

### Step 3: Update `detect.py` Wrapper

```python
#!/usr/bin/env python3
"""Unified game detection tool — backward-compatible entry point.

All logic lives in tools/detect/. This file imports and delegates.
"""
from tools.detect import scan_directory
from tools.detect.cli import print_summary, main

if __name__ == "__main__":
    main()
```

### Step 4: Verify Backward Compatibility

```bash
# Both should produce identical output:
python tools/detect.py /path/to/games --json > old.json
python -m tools.detect.cli /path/to/games --json > new.json
diff old.json new.json
```

---

## 5. Testing Strategy

### 5.1 Import Test

```python
# tests/test_detect_imports.py
def test_import_all_modules():
    from tools.detect import signals, scoring, exe_discovery, engine, gog, container, enrichment, scanner
    assert all(hasattr(m, '__file__') for m in [signals, scoring, exe_discovery, engine, gog, container, enrichment, scanner])
```

### 5.2 Behavioral Equivalence

```bash
# Run detect.py on known test data, compare JSON output
python tools/detect.py data/mock/standalone --json > /tmp/before.json
# After refactor:
python -m tools.detect.cli data/mock/standalone --json > /tmp/after.json
diff /tmp/before.json /tmp/after.json  # Should be identical
```

### 5.3 Per-Module Unit Tests

| Module | Test File | Tests |
|--------|-----------|-------|
| `signals.py` | `tests/test_signals.py` | Each store check, noise helpers, scan_root |
| `scoring.py` | `tests/test_scoring.py` | Root exe scoring, primary exe selection |
| `exe_discovery.py` | `tests/test_exe_discovery.py` | LNK parsing, subdir search, UE layout |
| `engine.py` | `tests/test_engine.py` | Each engine detection |
| `gog.py` | `tests/test_gog.py` | Metadata extraction, playTasks |
| `container.py` | `tests/test_container.py` | Non-game folder detection |
| `enrichment.py` | `tests/test_enrichment.py` | Name candidates, PCGW lookup |

---

## 6. Dependency Graph

```
cli.py
  └── scanner.py
        ├── signals.py
        ├── exe_discovery.py
        │     └── signals.py
        ├── scoring.py
        │     ├── signals.py
        │     └── enrichment.py
        │           └── signals.py
        ├── engine.py          (no deps)
        ├── gog.py
        │     └── signals.py
        ├── container.py
        │     └── signals.py
        └── enrichment.py
```

No circular dependencies. `signals.py` is the foundation module.

---

## 7. Impact on Other Tools

### `lookup_metadata.py`

Currently standalone. After the split, it can import:
- `tools.detect.signals` — for `is_noise_exe`, `NOISE_EXE_PARTS`
- `tools.detect.exe_discovery` — for `find_game_executables`
- `tools.detect.enrichment` — for `read_pe_metadata`, `build_name_candidates`

This eliminates duplicated noise-check logic between the two tools.

### `detect_folder.py`

**Deprecated.** No changes needed. Can remain as-is or be updated to import from `tools.detect.signals` if desired.

### C# Porting

The modular structure makes it easier to port individual features:
- `EngineDetector.cs` ← `engine.py`
- `NonGameFolderFilter.cs` ← `container.py`
- `PeMetadataExtractor.cs` ← `enrichment.py:read_pe_metadata`

---

## 8. File Size After Split

| Module | Est. LOC | Content |
|--------|----------|---------|
| `signals.py` | ~350 | SKIP_NAMES, noise patterns, store checks, scan_root |
| `scoring.py` | ~200 | pick_best_root_exe, pick_primary_executable |
| `exe_discovery.py` | ~250 | LNK parsing, exe finding, subdir search |
| `engine.py` | ~50 | 4 engine checks |
| `gog.py` | ~60 | GOG metadata extraction |
| `container.py` | ~120 | Non-game folder detection |
| `enrichment.py` | ~250 | PE metadata, name candidates, PCGW, enrich_unknowns |
| `scanner.py` | ~350 | Main scan pipeline |
| `cli.py` | ~80 | CLI + output formatting |
| **Total** | **~1710** | (slightly less due to import overhead removal) |

---

## 9. Rollback Plan

If the split causes issues:

1. Revert all module files
2. `detect.py` remains the single source of truth
3. The deprecated `detect_folder.py` is already kept for reference

---

## 10. Execution Timeline

| Step | Effort | Risk |
|------|--------|------|
| Create package + `__init__.py` | 5 min | None |
| Extract `engine.py` (no deps) | 15 min | None |
| Extract `gog.py` (no deps) | 15 min | None |
| Extract `container.py` | 20 min | Low |
| Extract `signals.py` (foundation) | 30 min | Low |
| Extract `exe_discovery.py` | 25 min | Low |
| Extract `enrichment.py` | 30 min | Low |
| Extract `scoring.py` | 20 min | Low |
| Extract `scanner.py` | 30 min | Low |
| Extract `cli.py` | 15 min | None |
| Update `detect.py` wrapper | 10 min | None |
| Verify behavioral equivalence | 20 min | None |
| **Total** | **~3.5 hours** | **Low** |

---

**Planner note:** This split is a pure refactor — no behavior changes. The `detect.py` wrapper preserves backward compatibility. Other tools can gradually adopt the modular imports. The C# codebase benefits from clearer Python reference code when individual modules are <400 LOC.
