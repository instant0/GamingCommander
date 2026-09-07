# Detection Logic — 04: Containers, Collections & Publisher Wrappers

**Reference:** Python `tools/detect.py` Phase 3 (container check) + collection fixes.
**Audience:** Anyone touching container/collection/catalog behavior.

---

## 1. The layered-library model (user model, 2026-09-06)

```
D:\Games\                    ← base library (scan root)
  epicgames\                 ← store collection folder (container)
    snuffbox\binaries\snuffbox.exe   ← game-shaped child → PROOF the parent is a collection
    othergame\...                     ← second proof
  arc-install\               ← publisher collection
    neverwinter_en\neverwinter.exe   ← game child
    othergame_en\...                 ← sibling entity (catalog signal)
```

**Rules:**
- A **container parent is never an entry** — its children are the games.
- The **first game-shaped finding** (child with a deeper exe) proves the parent is a collection.
- **Siblings at the same level are separate entities** — possibly games, but not the same game.

---

## 2. Container triggers (Phase 3)

A folder is a container if **any non-parent-bound child** has:

1. A **store marker** (`_scan_root` → store), OR
2. A **game exe at root** that is not a data/utility folder, OR
3. A **game-shaped subtree** — an exe found via `_find_exe_in_subdirs` (bounded, handles UE
   `Binaries/Win64/` layouts).

**Parent-bound children** (`system/`, `bin/`, `win64/`, `Binaries/`, `redist/`, …) are NEVER a
container trigger — they belong to the parent game (E4).

### The GAP B fix (2026-09-07): UE-wrapped containers

The old container check manually scanned 2 levels below a wrapper and **missed UE layouts**:

```
Ashen\Ashen\Binaries\Win64\Ashen-Win64-Shipping.exe   ← exe 3 levels below wrapper
```

`R1_Ashen` was never recognized as a container (missed in tree scans; found only as scan root).
**Fix:** the deep container check now reuses `_find_exe_in_subdirs`, which descends the UE path.
Verified: `Ashen` → container → inner `Ashen` resolves to `Binaries/Win64/Ashen-Win64-Shipping.exe`.

---

## 3. Real publisher-wrapper shapes (corpus-grounded)

Real single-game-child wrappers from full-d + full-e — all handled as containers, child resolves:

| Wrapper | Game child | Resolved entry |
|---------|-----------|----------------|
| `SquareEnix` | `FINAL FANTASY XIV - A Realm Reborn` | `game/ffxiv.exe` |
| `Stardock` | `TotalGaming` → `GalCiv2` | `GC2Launch.exe` |
| `qfg5` | `SIERRA` → `QFG5` | `SIERRA/QFG5/QFG5.exe` |
| `COD` | `Call of Duty` | `_retail_/cod.exe` |
| `ubi` | `Tom Clancy's Rainbow Six Siege X` | `RainbowSix.exe` |
| `GearsJack` | `GearGame` | `Binaries/Win64/GearGame-Win64-Shipping.exe` |

All verified in `testdata/mock/probe/PublisherWrapper/` (S-D3).

---

## 4. Collection detection with stray root files (2026-09-07)

**Bug found:** the collection check was gated on `not has_files_at_root` — so a collection with
stray launcher residue (`EpicGamesLauncher.url`, `readme.txt`) was misread as one "Unknown" game and
its children were missed.

**Fix:** the container signal is the **game-shaped children**, NOT the absence of root files.
Verified: stray-collection → both children found. Fixture: `CollectionWithStray/`.

---

## 5. Deep-nested collection games (2026-09-07)

**Bug found:** a collection game with a DEEP exe
(`monsterhunter\win64\binaries\monsterhunter.exe`) was missed — the container data-only check only
looked at direct children, wrongly skipped it as "data-only", then promoted `win64`.

**Fix:** (a) the container data-only check now searches deep exes via `_find_exe_in_subdirs`;
(b) the deep container check skips parent-bound children; (c) parent-bound exe collection reaches
2 levels. Result: `monsterhunter` → `win64/binaries/monsterhunter.exe` (parent wins).
Fixture: `CollectionDeepNest/`.

---

## 6. What was DROPPED (P3c-2, 2026-09-07)

The "single-child-not-a-collection" rule and the "1-proof/2-proof counter + re-scan" mechanism were
proposed but **rejected by corpus evidence**:

- Every real single-child chain (Ashen, SquareEnix, Stardock, qfg5) was already resolved correctly
  by the existing container recursion + parent-bound + terminating-rule logic.
- No corpus folder with a single game-shaped child is wrongly promoted as a collection.
- Adding a collapse rule would have REGRESSED the working publisher-wrapper cases
  (SquareEnix/Stardock would collapse to the wrapper instead of the game).

**Verdict:** the terminating rule (T1.5) already resolves the deep-duplicate cases that
single-child collapse was meant to fix. P3c-2 dropped; the plan records this decision.