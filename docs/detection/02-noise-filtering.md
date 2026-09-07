# Detection Logic — 02: Noise Filtering & Blacklist

**Reference:** Python `tools/detect.py` constants + `data/blacklist.json` (C#).
**Audience:** Anyone changing what is or is not treated as noise.

---

## 1. Two noise axes

| Axis | Function | Decides |
|------|----------|---------|
| `_is_noise_exe(name)` | Substring match against `_NOISE_EXE_PARTS` (23 tiers) | Whether an exe is a **candidate at all** |
| `_is_noise_dir(name)` | Substring match against `NOISE_DIR_PARTS` | Whether a directory is skipped during deep search |

Noise classification is **substring-based and tiered**. The same word can appear in a game
name (`Fallout 4 GOTY`) and be fine, while `install` in `installer.exe` is noise.

---

## 2. Noise exe tiers (Python `_NOISE_EXE_PARTS`, 23 tiers)

Exact list lives in `tools/detect.py` (`# ── Skip lists ──`, ~lines 169–265). Categories:

| Tier | Examples | Rationale |
|------|----------|-----------|
| 1 Universal | `setup`, `install`, `update`, `uninst` | Installers/updaters ship alongside games |
| 2 Launcher stubs | `launcher`, `bootstrap`, `dowser` | Penalized in scoring, not always filtered |
| 3 Store bootstraps | `epicgames` (launcher variants only) | Store clients, not games |
| 4 Anti-cheat/DRM | `battleye`, `easyanticheat`, `denuvo`, `punkbuster`, `xigncode`, `vmprotect` | Runtime protection, not the game |
| 5 Crash reporting | `crash`, `bugsplat`, `crashpad` | Telemetry/error handlers |
| 6 Error reporters | `error`, `exception`, `sndrpt` | Same |
| 7 DRM wrappers | `activate`, `activation`, `ccmini` | Activation helpers |
| 8 Installer/patch utils | `autorun`, `7za`, `xdelta`, `vcredist`, `dxsetup`, `oalinst` | Redist/install payloads |
| 9 Dedicated servers/loaders | `dedicatedserver`, `stub`, `loader`, `browser` | Not the game |
| 10 Media/movie players | `movie`, `intro`, `ffplay`, `ffprobe`, `ffmpeg` | Cutscene/codec tools |
| 10 Stardock dist | `sdcr` | Distribution tool |
| 11 Dev/content editors | `editor`, `modmanager`, `packagemanager`, `datacompiler`, `contented`, `leveled`, `resourceed`, `builder`, `worldbuilder`, `configtool` | Tooling |
| 12 Utilities/debug | `debug`, `utils`, `explorer`, `brwc`, `acpc` | Utility processes |
| 13 Trial/stub/demo | `trial`, `_upp` | Not full games |
| 14 Media/codec/streaming | `ffmpeg`, `ffplay`, `ffprobe` | Codec tools |
| 15 Installer frameworks | `squirrel`, `wininst`, `w9xpopen` | Framework runtimes |
| 16 Runtime interpreters | `python`, `scummvm` | Runtimes, not games |
| 17 Web UI/overlay | `webview`, `overlay`, `coherentui`, `cefhost`, `awesomium` | Embedded browsers |
| 18 Repair/service | `repair`, `service`, `reminder`, `startup`, `helper` | Background services |
| 19 Unreal build tools | `unrealpak`, `unrealcefsubprocess` | Engine tooling |
| 20 Patch/update | `patch`, `patcher`, `touchup` | Update processes |
| 21 Utility tools | `resourceed`, `winscp`, `7za` | General utilities |
| 22 Driver/hardware | `driverloader`, `kernelmodedriverloader` | Driver loaders |
| 23 Intro video | `intro` | Cinematics |

**Priority order matters** — the substring list is checked in tier order; first match wins.

---

## 3. The `epicgames` special case (2026-09-07, E3)

`epicgames` in `_NOISE_EXE_PARTS` was **substring-matching real game exes** that merely reference
the Epic SDK in their name:

```
IndianaEpicGameStore-Win64-Shipping.exe   ← real game exe (full-d.txt), was wrongly noise
EpicGamesLauncher.exe                     ← correctly noise (launcher)
EpicGamesUpdater.exe                      ← correctly noise (updater)
```

**Fix:** `_is_noise_exe` special-cases `epicgames` — it is only noise when combined with
launcher-style suffixes (`launcher`, `updater`, `bootstrapper`, `bootstrap`, `overlay`, `webhelper`).
A game exe that merely contains "epicgames" is kept.

---

## 4. Noise dirs (`NOISE_DIR_PARTS`)

Directories that contain only redist/installer payloads and are skipped during deep search:
`redist`, `_CommonRedist`, `__redist`, `vcredist`, `directx`, `installer`, `support`, etc.
(Exact list in `tools/detect.py`.)

**Note:** the terminating rule's match search deliberately does NOT skip noise dirs — the exact
stem match is itself the guard (e.g. `redist/PENUMBRA.EXE` is the game's only real exe).

---

## 5. C# blacklist (`data/blacklist.json`)

The C# side uses a **tiered scoring blacklist** (not a flat substring filter):

| Tier | Purpose | Example patterns |
|------|---------|------------------|
| `tier_1`…`tier_6` | Hard noise (filtered) | installers, anti-cheat, DRM, crash |
| `tier_7`…`tier_23` | Penalized but not always filtered | servers, editors, utils, trial |

Key differences vs Python:
- C# is tier-based with **per-tier penalties** (`-10`…`-30`), Python is a flat `_is_noise_exe` gate.
- `tier_3_store_bootstraps: ["epic"]` is the C# equivalent of the Python `epicgames` entry.
- C# has `IsForbiddenLaunchExe` (hard guard for `unins*`/`uninstall`/`unwise`).

---

## 6. Non-game folder names (`_NON_GAME_DIR_NAMES`)

Folder names that are never games (exact match, checked at the container data-only gate and the
non-game gate): `dlc`, `program files`, `windowsapps`, `squirreltemp`, `epiclauncher`,
`nexus mod manager`, `soundtrack(s)`, `manuals`, `wiiu`, `portable`, `reshade`, `sweetfx`,
`enbseries`, `enb`, `vortex`, `mod organizer`, `dotnet35`, `dotnetfx35`, `msvc2012/2013`,
`vcredist`, `dotnet`, `uninstall`.

## 7. Parent-bound child dirs (`_PARENT_BOUND_CHILD_DIRS`)

Platform/build/redist dirs that **belong to the parent game** — never a container trigger, never a
separate entry (E4/E5):

```
system, bin, bin64, win32, win64, x86, x64, binaries, boot, core, game,
run, app, client, engine, crashsender, common, redist, redistributable,
_installer, install, installer, support, directx, vcredist
```