# Phase 2: Steam & Stand-alone Games

**Status:** Baseline ✅ COMPLETE (scan + launch). Leftover: SyncMove → `04-phase-2-syncmove.md`.  
**Updated:** 2026-08-22

## Goal

Implement the baseline game management features.

## Tasks

1. [x] **Steam Integration**
    - Steam library detection (`libraryfolders.vdf`).
    - Parse Steam manifest files (`appmanifest_*.acf`).
    - Installed / Moved / Orphaned / Missing via ACF cross-ref.
    - Steam-internal `common/` folders skipped (Bug 10).
2. [x] **Stand-alone Detection**
    - Signal-based scan for GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emu.
    - Deep exe discovery + scoring; container detection; 21-tier blacklist.
    - Nested Steam trees excluded from FolderScanner (Bug 13b).
3. [ ] **Migration (Steam)** — **not in this baseline**
    - Manifest repair for relocated game folders → [`04-phase-2-syncmove.md`](04-phase-2-syncmove.md).
4. [x] **Launcher Logic**
    - `steam://rungameid/{appid}` for Steam; direct `.exe` for others.
5. [x] **Deliverables (baseline)**
    - Steam games appear in the UI.
    - Stand-alone games detected via folder scan.
    - [ ] Steam games can be migrated safely (Phase 2.1).

Epic local `.item` / `.mancpn` enrichment shipped as Plan 109 (not a Phase 3 prerequisite).
