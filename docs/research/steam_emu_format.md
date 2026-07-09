# Steam Emulators & Ubisoft Emulators — Detection Guide

## Purpose
Document how to detect games using Steam emulators and Ubisoft emulators, distinguish them from legitimate installations, and avoid false positives from engine middleware bundles.

## Overview

Emulators (also called "cracks" or "loaders") are used to run games without their original launcher. They work by providing stub implementations of launcher DLLs. From a detection perspective, these games are functionally standalone — they don't require the original launcher to run — but they originated from a launcher-managed install.

Detection of emulated games is important for:
1. **Accurate store attribution** — treat as the emulated store, not standalone
2. **Migration planning** — the game may have emulator-specific files that must move with it
3. **Launch logic** — emulated games launch via direct executable, not launcher URI

---

## Steam Emulators

### Primary Detection Signal: `steam_api64.dll` / `steam_api.dll` at Game Root

The strongest signal for a Steam emulator is the presence of `steam_api64.dll` or `steam_api.dll` at the **game folder root** (not in a subdirectory).

### Critical: Path Context Matters

**Steam API DLLs inside a legitimate Steam library path (`steamapps/common/`) do NOT indicate an emulator.** Legitimate Steam games also have these DLLs — they are the real Steamworks API. The distinction is:

| Context | DLL at root means | Action |
|---------|-------------------|--------|
| Inside `steamapps/common/<game>/` | Legitimate Steam game | Detect via libraryfolders.vdf + ACF |
| Outside any Steam library path | Steam Emulator | Detect as Steam Emulator |
| In `Engine/Binaries/ThirdParty/Steamworks/` | Unreal Engine SDK bundle | **Ignore** — not an emulator signal |

### Secondary Detection Signal: `steam_emu.ini`

Some emulators leave a configuration file `steam_emu.ini` at the game root or in a subdirectory. This file typically contains:
- `[Settings]` section with language, app ID overrides
- Account name / user ID settings
- DLC unlock configuration

This is checked as part of deep detection (pass 2) after root-level signals fail.

### Deep Detection: Unreal Engine Layout Check

The Steam emulator deep check (`_has_steam_emu_ini`) also scans the Unreal Engine's ThirdParty Steamworks directory:
```
Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/
```
This catches cases where the emulator config was placed inside the UE SDK path rather than at game root.

### Common Steam Emulators

| Emulator | Files | Characteristics |
|----------|-------|-----------------|
| **Goldberg** | `steam_api64.dll`, `steam_api.dll`, `steam_emu.ini` | Open source; config file at game root; most common |
| **SmartSteamEmu (SSE)** | `SmartSteamEmu.dll`, `SmartSteamEmu.ini` | Self-contained emulator; may inject via `.dll` |
| **CreamAPI** | `cream_api.ini` | DLC unlocker only (not a full emulator); used alongside legitimate Steam install |
| **Ali213 / CODEX** | `steam_api64.dll`, `steam_emu.ini`, `valve.ini` | Pre-Goldberg era; `.ini` at game root |
| **FLT** | `steam_api64.dll`, `flt.ini` | Config file with appid and language settings |

### False Positive: Unreal Engine Steamworks SDK

Unreal Engine games by default bundle the Steamworks SDK in:
```
Engine/Binaries/ThirdParty/Steamworks/
```

This directory contains the **real** `steam_api64.dll` and `steam_api.dll` (not emulators). The detection pipeline must explicitly check for `steam_emu.ini` in this path, not just the presence of the DLLs.

As verified in the real library test (`/mnt/e/games`), 7 of 38 games had the UE Steamworks SDK directory. Only 4 of those had a `steam_emu.ini` present (legitimate emulator configs). The remaining 3 were correctly bypassed.

---

## Ubisoft Emulators

### Primary Detection Signal: uplay Loader + Config INI

The detection function `_check_ubisoft_emu()` checks for:
1. **Loader executable:** `uplay_loader*.exe` or `uplay_r*_loader*.exe` at game root
2. **Configuration file:** Any `.ini` file at root containing both `Username=` and `AccountId=` keys

Both conditions must be true for a positive emulator detection.

### Why Both Conditions Are Required

The `Username=` and `AccountId=` keys are telltale signs of an emulated Uplay environment. Legitimate Ubisoft games do not have editable INI files with hardcoded account credentials. The loader executable is the mechanism that intercepts Ubisoft Connect API calls.

### Common Ubisoft Emulators

| Pattern | Files | Notes |
|---------|-------|-------|
| **uplay_loader** | `uplay_loader.exe`, `uplay_loader64.exe` | Most common; replaces Uplay API |
| **Uplay R* Loader** | `uplay_r1_loader.dll`, `uplay_r1_loader64.dll` | Handles Uplay emulation at DLL level |
| **CODEX / CPY** | `uplay_r1_loader64.dll`, `CODEX.ini` | Includes config INI with Username/AccountId |

### Distinguishing from Legitimate Ubisoft Installs

| Signal | Legitimate Ubisoft | Ubisoft Emulator |
|--------|-------------------|-----------------|
| `uplay_install.manifest` | ✅ Present | ❌ Usually absent |
| `uplay_r*_loader*.dll` | ❌ Not at root (inside subdirectories) | ✅ At root or root/bin |
| `*.ini` with `Username=`, `AccountId=` | ❌ Never | ✅ Always |
| `uplay_loader.exe` | ❌ Not a Ubisoft file | ✅ Present |

---

## Detection Priority in Pipeline

The emulator checks are placed at the **correct priority** in the detection pipeline:

### Root-Level Checks (Pass 1, in order)
1. GOG — highest priority
2. EA
3. Ubisoft Emulator — **before** Ubisoft (emulator takes precedence for correct store attribution)
4. Ubisoft
5. Epic
6. Blizzard
7. Xbox
8. Rockstar
9. **Steam Emulator** — **after** all native stores (only triggers if no higher-priority signal matches)

### Deep Checks (Pass 2, in order)
1. **Steam Emulator** (from `steam_emu.ini`) — after root-level checks fail
2. Ubisoft legacy (UbiStats.dll)
3. Standalone (unreal_binaries)
4. Standalone (root_exe)
5. Standalone (root_lnk)

### Why Steam Emulator is After Native Stores

A game identified as Steam Emulator may also have an `.egstore` directory (Epic) or `goggame-*` files (GOG) if it was copied between launchers. The native store signals take priority because they represent the actual source. The Steam Emulator signal is treated as a fallback — if no native store marker exists, the presence of Steam API DLLs likely means the game was originally a Steam release but is now running through an emulator.

## References
- `tools/detect_folder.py` — `_check_steam_emu()`, `_has_steam_emu_ini()`, `_check_ubisoft_emu()`, `_detect_deep()` functions
- `docs/research/steam_acf_schema.md` — legitimate Steam ACF format
- `docs/research/steam_common_schema.md` — legitimate Steam folder structure
- `docs/research/ubisoft_format.md` — legitimate Ubisoft format
- `docs/findings/detect-folder-verification.md` — verified edge cases from real library test
