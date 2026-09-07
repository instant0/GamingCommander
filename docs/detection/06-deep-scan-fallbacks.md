# Detection Logic — 06: Deep Scan, Fallbacks & Engine Detection

**Reference:** Python `tools/detect.py` Phase 2 + helpers.
**Audience:** Anyone touching the fallback/unknown-resolution path.

---

## 1. Deep signal scan (Phase 2, unknowns only)

For folders still unknown after T1/T1.5/T2/container/parent-bound/non-game gates:

- Walks up to **4 levels** collecting `.exe/.dll/.ini` names.
- `_match_markers` detects GOG/EA/Ubi/Epic/Steam-Emu markers found in subdirs.
- Deep exes are scored with `_pick_best_root_exe`; best becomes primary.
- If a deep store marker is found → store-typed entry (Tier Secure).
- Otherwise → `Unknown`, `needs_review=True` (enrichment target).

**Perf note (C#):** C# does NOT do this 4-level walk — it uses targeted probes only
(`steam_emu.ini`, `UbiStats.dll`, UE layout, root exe/.lnk). This is the main remaining
detection-parity gap.

---

## 2. Engine detection (`_detect_engine`)

| Engine | Signal |
|--------|--------|
| Unreal | `Engine/` + `Binaries/`, or child `<Game>/Binaries/Win64` |
| Unity | `UnityPlayer.dll` + `*_Data` |
| RAGE | `title.rgl` + `common.rpf` |
| Frostbite | `Engine.BuildInfo_Win64_retail.dll` |

Engine detection identifies **layout for exe discovery** (`Engine/Binaries/Win64/`), NOT store
classification. C# adds Source/Godot/CryEngine display tags.

---

## 3. `.lnk` resolution (`_find_exe_via_lnk`)

- Reads the `.lnk` binary, latin-1 decodes, regex-extracts `.exe` names.
- Skips DLLs (`steam_api.dll`, `steam_api64.dll`, `eos.dll`, `upc.dll`).
- Picks the longest candidate (most likely the game).
- Searches subdirs 3 levels deep; exact match returns immediately, fuzzy
  (`-Name.exe`, `copy of Name.exe`) falls back.

---

## 4. GOG metadata (`_extract_gog_metadata`)

- Scans root + 1-level non-noise subdirs for `goggame-*.info`.
- Prefers `gameId == rootGameId` (main game), falls back to first non-main (DLC).
- `playTasks` yields the primary exe + arguments; relative path kept.

---

## 5. Non-game folder check (`_is_non_game_folder`)

Three layers (Python):

1. **Exact name match** — `_NON_GAME_DIR_NAMES` (redist, soundtrack, mod managers, …).
2. **Child-all-non-game** — if ALL children are non-game subdir names → skip.
3. **File-type analysis** — no non-noise exe + no meaningful file (only music/support ext) → skip.

**C# gap (HIGH):** C# `ContainerScanner.IsNonGameFolder` implements **only layer 1** (name match).
Layers 2–3 are missing — C# can promote folders Python would reject (false-positive risk).
This is the top porting priority for Plan 123.

---

## 6. The non-game filter vs UE layouts (2026-09-07, E3)

**Bug found:** the probe's non-game filter rejected a real UE game folder whose only exe lives in
`Binaries/Win64/`:

```
Indiana\Binaries\Win64\IndianaEpicGameStore-Win64-Shipping.exe
```

`_is_non_game_folder` only scanned root files → no exe at root → wrongly "data-only".

**Fix:** `_platform_child_has_game_exe` now descends **2 levels** into the platform child
(`Binaries/Win64/*.exe`), matching production's `_find_exe_in_subdirs` UE path. The probe and
production now agree: `Indiana` → Candidate with the real UE exe.

---

## 7. The `install` substring pattern (2026-09-07, E3 verified-safe)

The `install` noise substring was audited against all corpora — it catches ONLY installer /
anti-cheat / update exes (`install.exe`, `install64.exe`, `EAAntiCheat.Installer.Tool.exe`,
`firewallinstall.exe`, `d3d11install.exe`, …). **No real game exe is wrongly filtered.**
No change needed.