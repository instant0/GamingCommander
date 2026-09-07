# Detection Logic — 01: Overview & Decision Pipeline

**Nature:** Fresh reference (2026-09-07). Describes the **Python reference scanner**
(`tools/detect.py`) and its relationship to the C# implementation.
**Audience:** Any agent touching detection. This is the primary entry point.

---

## 1. Two detection systems

| System | Where | Role |
|--------|-------|------|
| **Python reference** | `tools/detect.py` | Ground truth. All detection behavior is validated here FIRST, then ported to C#. |
| **C# implementation** | `src/GamingCommander.App/Services/` (`FolderScanner`, `ContainerScanner`, `ExecutableDiscovery`, `StoreSignalDetector`, `FallbackSignalDetector`, …) | Ships in the app. |

**Workflow (Plan 123):** Python-first. A behavior change is only accepted into C# after it is
corpus-validated in Python and passes the validation matrix + baseline.

**Steam is a separate locked system:** `SteamLibraryScanner` handles Steam exclusively via the
structural path `{library}/steamapps/common/{GameName}/` + ACF cross-reference. The generic scanner
(FolderScanner / Python `scan_directory`) never handles real Steam. Steam Emulator games
(`steam_api64.dll`) ARE handled by the generic scanner.

---

## 2. The decision pipeline (Python `scan_directory`)

For each top-level folder under a scan root, the pipeline runs **in this exact order**.
The first gate that fires wins; later gates are skipped.

```
Phase 1: Root scan (one os.scandir) → store, root exe, root .lnk, child dirs

  Gate T1  Store signal at folder root ............ → classify by store, resolve exe, DONE
  Gate T1.5 Exact folder-name match (TERMINATING) .. → folder IS game, that exe IS primary, DONE
  Gate T2  Root exe or root .lnk .................... → standalone, DONE

Phase 3: Container check (children have own signals)
  Gate C1  Child has store marker / game exe ....... → container: recurse into children, DONE
  Gate C2  Parent-bound child holds the only exe .... → parent IS game (E4/E5), DONE

  Gate N1  Non-game folder check ................... → skip (redist/music/mod/tool)

Phase 2: Deep signal scan (unknowns only)
  Gate D1  Deep store marker found (≤4 levels) ..... → classify by store, DONE
  else    ............................................ → Unknown, needs_review

Phase 4: Enrichment (optional, only for needs_review): PE metadata / PCGW lookup
```

### Gate semantics

| Gate | Fires when | Result |
|------|-----------|--------|
| **T1 store** | Root-level store marker (GOG/EA/Ubi/Epic/Blizzard/Xbox/Rockstar/SteamEmu) | Tier Secure/Locked, store-typed entry |
| **T1.5 terminating** | An exe at/below the folder has a stem that exactly matches the folder name (token OR whole-name-normalized) | Folder IS game folder; that exe IS primary; **deeper candidates excluded from working set** |
| **T2 root exe/.lnk** | Non-noise exe at root, or `.lnk` resolving to an exe | Standalone entry |
| **C1 container** | A child has a store marker OR a game-shaped subtree (exe at depth ≤3 inside it) | Parent is a collection/catalog; children are the entries (recursion) |
| **C2 parent-bound** | Only exes live inside a platform/build/redist child (`system/`, `bin/`, `win64/`, `Binaries/`, `redist/`, …) | Parent wins; score child exes against PARENT name (E4/E5) |
| **N1 non-game** | Folder name or contents are clearly non-game (redist, music, mod manager, data-only) | Skipped |
| **D1 deep signal** | Store marker found by 4-level `.exe/.dll/.ini` walk | Store-typed entry |

---

## 3. Working-set reduction (terminating rule)

The terminating rule (T1.5) is a **structural short-circuit**, not a scoring bonus:

- Real shape: `Neverwinter_en\Neverwinter.exe` + nested `Neverwinter\Live\x64\GameClient.exe`
  + double-nested duplicate.
- The root match `neverwinter` ≡ folder token `neverwinter_en` resolves the game and
  **excludes the nested `GameClient.exe` from the candidate working set** the client sees.
- Same for `Diablo III\Diablo III.exe` vs `x64\Diablo III64.exe` (whole-name match) —
  the backup/deeper copies never become candidates.

Match forms:
- **Token match:** exe stem == one folder token (`Neverwinter.exe` ↔ `neverwinter_en`).
- **Whole-name match (GAP A, 2026-09-07):** exe stem, separators stripped, == folder name
  separators stripped (`Diablo III.exe` ↔ `Diablo III`, `deadspace3.exe` ↔ `Dead Space 3`).
- **Backups never match:** leading-dash (`-Penumbra.exe`), `copy of X`, `10 org X` are excluded
  (E5 guard preserved).

The match also provides the **catalog signal**: a sibling at the same level (`othergame_en` beside
`neverwinter_en`) is a separate entity — a distinct game, not this one.

---

## 4. Confidence tiers

| Tier | Meaning | Set by |
|------|---------|--------|
| **Locked** | Authoritative, structural | Steam `steamapps/common`, Epic Locked manifest |
| **Secure** | Store-signal verified | Any store marker (T1, D1) |
| **Candidate** | Signal-based best guess | T1.5, T2, C2, D1-with-exe |
| **Unknown** | No reliable signal | Falls through everything; `needs_review=True` |

A store-Secure/Locked tier is **never downgraded** by a later gate (fix 2026-09-07: the terminating
rule no longer overwrites Secure with Candidate).

---

## 5. Related documents

| Doc | Content |
|-----|---------|
| `02-noise-filtering.md` | Noise exe/dir classification, blacklist tiers |
| `03-store-signals.md` | Store marker detection (all stores) |
| `04-containers.md` | Container / collection / publisher-wrapper logic |
| `05-executable-discovery.md` | Exe discovery + scoring |
| `06-deep-scan-fallbacks.md` | Deep signal scan, engine detection, .lnk, GOG metadata |
| `07-csharp-parity.md` | C# ↔ Python gap analysis |
| `08-validation.md` | How to validate (matrix, baseline, corpus) |

---

## 6. Scan order safety note

The pipeline is deliberately **safe-first**: folder/exe false-positive reduction before identity
resolution. The terminating rule fires BEFORE the container check so a folder with a deep duplicate
is never misread as a collection. Container-before-filter ordering (S1/S2) was a probe finding —
container analysis must run before the non-game filter or every container is rejected.