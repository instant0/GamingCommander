# Detection Logic — 03: Store Signal Detection

**Reference:** Python `tools/detect.py` `_scan_root` / `_probe_store_at` / `_deep_signal_scan`.
**Audience:** Anyone touching store classification.

---

## 1. Priority chain (all stores, single pass over collected names)

Checked **in this exact order**; first match wins (matching C# `StoreSignalDetector`):

| # | Store | Signal (file/dir at folder root) |
|---|-------|-----------------------------------|
| 1 | **GOG** | `goggame.dll`, `goggame-*` prefix, `gog_*` prefix, `gog.ico` |
| 2 | **EA** | `__Installer/` dir, `touchup.exe`, `ActivationUI.exe` |
| 3 | **Ubisoft Emulator** | `uplay_loader*` or `uplay_r*_loader*` + `.ini` containing `Username=` + `AccountId=` |
| 4 | **Ubisoft** | `uplay_install.manifest`, or `uplay_r*_loader*.dll` |
| 5 | **Epic** | `.egstore/` or `.egsstore/` dir |
| 6 | **Blizzard/BattleNet** | `.battle.net/` dir, **OR `.build.info` / `.patch.result` / `.product.db`** (E2, 2026-09-07) |
| 7 | **Xbox** | `default-metadata.json` |
| 8 | **Rockstar** | `title.rgl` |
| 9 | **Steam Emulator** | `steam_api64.dll` or `steam_api.dll` |

C# also adds: registry fallback (EA/Ubi/GOG/Rockstar) and `steam_appid.txt` (weak Steam-Emu signal).

---

## 2. Steam registry bootstrap (planned, 2026-09-07)

Steam is a LOCKED separate system (`SteamLibraryScanner`), but the **entry point** to find its
libraries comes from one registry value:

```
HKLM\SOFTWARE\Valve\Steam  →  InstallPath  =  P:\Program Files (x86)\Steam
```

Chain: `InstallPath` → `<InstallPath>\steamapps\libraryfolders.vdf` → all library roots
(each `"path"` entry = a library containing `steamapps\common\<Game>\` + `appmanifest_*.acf`).

- Verified against `/mnt/r/hklmvalve.reg` (HKLM `InstallPath`) and `/mnt/r/valve.reg` (HKCU
  `SteamPath`/`SteamExe` are equivalent). **`Apps\*` subkeys are irrelevant — ignored.**
- **vdf shape (parsed from `/mnt/r/libraryfolders.vdf`):** numeric keys `"0"`/`"1"`/`"2"` each hold
  a `"path"`. **Library `"0"` is the install folder itself** (its path == the registry
  `InstallPath`); external libraries (`E:\SteamLibrary`, `D:\SteamLibrary`) are additional roots.
- **AUTHORITY RULE (user, 2026-09-07):** `libraryfolders.vdf` is a **PATH LOCATOR ONLY** — it
  answers "WHERE are the Steam libraries?" and nothing else. The catalogue structure is identical
  for every library (`<lib>\steamapps\appmanifest_*.acf` + `<lib>\steamapps\common\<Game>\`).
  **ACF + folder scan is the ONLY content authority.** The vdf's `apps` blocks are informational
  (appid → size) and IGNORED — never used to override/augment ACF findings.
- **Dedup:** when offering "add all Steam libraries", skip roots already configured; on a fresh
  setup add all. **Rescan offer:** reconcile vdf paths vs configured roots — offer to add paths
  present in the vdf but not configured, and remove configured roots absent from the vdf.
- This is a **known-fixture lookup**, not a detection rule: it only locates the Steam client,
  then the existing locked `SteamLibraryScanner` (ACF+folder) takes over.
- **Virtual catalog (user, 2026-09-07):** the launcher shows ONE catalog per source type
  ("Standalone" / "Epic" / "Steam") — NOT one per physical root (`Steam-1`, `Steam-2`). N Steam
  library roots feed a single "Steam" catalog; `GameSourceKind` is the catalog axis, `LibraryRoot`
  stays per-game metadata.
- **Confirmed design (2026-09-07):** a library's `reference` may be a directory (Standalone/GOG/EA),
  a vdf file (Steam), or a manifest location (Epic). The parser branches on `DefaultType`:
  Steam → vdf-parse + ACF/folder scan; Epic → manifests; others → directory scan. Games are linked
  to the VFS library they belong to (one linkage per game).
- **Add-Steam UX (Q4 confirmed):** `CanAddSteamLibraries` is true ONLY when the registry
  `InstallPath` resolves AND the vdf yields ≥1 library. **Button hidden when Steam is not
  installed** — same as Epic (hidden when manifests folder absent). A user-supplied Steam-path
  folder (ACF + `steamapps\common`) still adds via the folder picker: `NormalizeLibraryRoot`
  walks up to the Steam library root before the library anchor is registered.
- **Migration (Q1 confirmed):** existing individual Steam roots collapse into the single "Steam"
  library when `AddSteamLibrariesAsync` runs; user settings preserved, only per-game VFS linkage
  changes.
- **ACF cross-library remediation (user, 2026-09-07):** with all library + ACF paths known,
  mismatches (game folder on D, ACF on E) are detectable → propose a remedy (e.g., move the ACF),
  user-confirmed. Detection authority unchanged (ACF+folder); this adds a repair pathway.
- Registry access via the existing `IRegistryReader` abstraction (mock-able; Windows-only in prod).

---

## 2. The BattleNet `.build.info`/`.product.db` signal (E2, 2026-09-07)

**Real corpus shape (full-d + full-e):** BattleNet games do NOT have a `.battle.net` dir at the
game folder. Instead they carry:

```
Blizzard\Diablo III\.build.info        (+ .patch.result, .product.db)
COD\Call of Duty\.build.info           (+ .product.db, Launcher.db, _retail_\.flavor.info)
Diablo Immortal\.product.db
World of Warcraft\...  (via Blizzard parent)
```

**Fix:** `_scan_root` now returns `Blizzard` for `.build.info` / `.patch.result` / `.product.db`.
Verified against all 3 corpora: these markers appear **only** in BattleNet contexts — zero false
positives.

**Why it matters:** without this, `Diablo III` was typed Unknown/Standalone instead of
Blizzard/Secure, losing the store-Secure tier and store metadata.

---

## 3. Store tier preservation (2026-09-07)

A folder may carry BOTH a store marker AND a folder-name match (e.g. `Diablo III` has
`.build.info` AND `Diablo III.exe` matches the folder name).

**Fix:** the terminating rule (T1.5) no longer overwrites a store-Secure/Locked tier with Candidate.
Store signals always win over folder-name matching.

---

## 4. Store markers at depth (deep signal scan, Phase 2)

For folders with no root store signal, Python walks up to **4 levels** collecting
`.exe/.dll/.ini` names and matches `_match_markers` (GOG/EA/Ubi/Epic/Steam-Emu markers found in
subdirs). This is the **only** way Python finds store markers buried in subdirs.

C# does NOT do this walk (perf); it relies on root signals + targeted probes + container recursion.

---

## 5. Store verification evidence (corpus)

`tools/train_detection.py` classifies all 140 real games through the detection logic:

| Store | Count (of 140) |
|-------|----------------|
| GOG | 11 |
| Epic | 2 |
| EA/Origin | 2 |
| Ubisoft | 2 |
| BattleNet | 1 (plus the E2-fixed Diablo III/COD/World of Warcraft cases) |
| Rockstar | 1 |
| standalone | 121 |

Validation matrix fixtures cover all 9 store types + the real BattleNet-manifest shape
(`StoreBlizzardBnet/`).