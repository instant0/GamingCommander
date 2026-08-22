# Plan 120 — PCGW identity (P1) + details sidecar (P2)

**Status:** COMPLETE (shipped 2026-08-22). Later addenda: year-hint OpenSearch, F3 page picker.  
**Depends on:** Plan 119 v1 (sidecar file, Steam lookup, PCGW Parse path, `EnableOnlineMetadata`)  
**Does not replace:** Scan / `games.json` / local engine / launch.  
**Live contract:** `docs/ONLINE-AND-DATA.md` (if this plan and that file disagree, that file + code win).  
**Live check:** Cyberpunk 2077 page (2026-08-22) — sections and templates below are real, not guessed.

---

## As shipped (do not re-open)

| Spec in this file | What landed |
|-------------------|-------------|
| Nested sidecar `identity {…}` | **Not used.** Identity fields stay **flat** on `GameMetadataRecord` (`Developer`, `SteamAppId`, `PcGamingWikiUrl`, …). |
| `confidence` high/medium/low; skip Part 2 on low | **Not used.** A resolved page is parsed. Ambiguous OpenSearch → year hint + **F3 picker**. |
| Part 1 name-search only when scan failed | **Narrowed.** Lookup never writes `GameEntry.DisplayName`. Named Steam/GOG games still get **Part 2 extras** (F5 queue / F3). |
| Steam first, then PCGW | **Reversed.** PCGW first; Steam Store fills holes (score/cover). |
| HTTP on list select | **Forbidden.** Highlight = cache only. |
| F4 “Launch exe with extras (ignore steam://)” | **Removed.** That invert was a bug. Steam URI stays Steam URI. |
| `LaunchExeWithExtras` | **Deleted.** |

**Triggers (Online chip + flag on):** F5 enqueue after rescan; F3 force lookup (picker if ≥2 clean titles); F4 may start a **background** refresh and does **not** wait; launch may enqueue if sidecar stale. Probe: one `HEAD` PCGW favicon per process.

**Page resolve:** Steam AppID → `appid.php`; else OpenSearch (limit 8, noise filter) → `PcgwTitleFilter.PickBest(titles, yearHint)` from PE/`PeProductYear`; F3 override via `pageTitleOverride`.

---

## 0. Why 119 is not enough

119 stores who/what at a glance (developer, genre, score, wiki URL).  
A PCGW page also has the **operator data** you actually use:

| You asked for | PCGW section / template (Cyberpunk) |
|---------------|-------------------------------------|
| Bypass launcher / extra args | `=== Bypass REDlauncher or GOG Galaxy ===` + `{{Fixbox}}` with `--launcher-skip -skipStartScreen -modded` |
| All command-line arguments | `=== Command line arguments ===` → `{{Standard table/row\|-width X\|...}}` |
| Config paths per install | `{{Game data/config\|Windows\|{{P\|localappdata}}\CD Projekt Red\Cyberpunk 2077}}` |
| Save paths per install | `{{Game data/saves\|Windows\|{{p\|userprofile}}\Saved Games\CD Projekt Red\Cyberpunk 2077}}` |
| Video features | `== Video ==` → `{{Video ...}}` (FOV, ultrawide, 60 FPS, …) |

119’s Infobox-only parser never reads these sections. Genre taxonomy also missed on this page. That is a **parser gap**, not a missing website.

**Two jobs, same sidecar, never overwrite a good local identity.**

```
Part 1  IDENTIFY   → LAST RESORT only when scan/ACF/GOG/PE all failed
Part 2  POPULATE   → details + F4 argument catalog (uses a known page / store id)
F4      TOGGLE     → user picks PCGW args; explainers stay visible; write ExtraLaunchArguments
```

Part 2 needs a **confirmed PCGW page**. That page usually comes from a store id we **already** have (Steam AppID, GOG id) — that is *lookup for details*, not *identify the game*. Online name-search is only Part 1.

---

## 1. Part 1 — Identify only if everything else failed

### Goal

Name + developer from the network **only** when the offline chain produced nothing useful.

Offline already owns identity for most libraries:

| Already have | Then do **not** run Part 1 name search |
|--------------|----------------------------------------|
| Steam ACF `name` + AppID | Identity done. Use AppID later for Part 2 page resolve only. |
| GOG `goggame-*.info` name / gameId | Identity done. |
| PE FileDescription / store signal title | Identity done if not blacklisted (`AppName`, etc.). |
| User F4 DisplayName | Never overwrite. |

**Part 1 runs when:** DisplayName is empty or equals a raw folder token, PE was empty/blacklisted, no ACF/GOG name, no user override.

### Online fallback order (no Cargo, no SteamDB)

**Have Steam AppID (ACF):** PCGW `appid.php` (correct page) → extras. Then Steam Store `appdetails` for score/cover.

**No AppID:** PCGW OpenSearch on display name → parse Availability/store Steam ID → then Steam Store with that ID.

SteamDB is **optional later** (same role as Steam Store: confirm title). Do not block Part 2 on SteamDB.

1. If a leftover AppID exists without a name → `appid.php` / Steam `appdetails` for title only.  
2. Else OpenSearch on folder / exe stem (not Epic codenames).  
3. Reject soundtrack / demo / disambiguation (`PcgwTitleNoise`).  
4. Write **sidecar** `identifiedTitle` / developer only. Write `GameEntry.DisplayName` only if still empty and confidence is `high`.

If Part 1 is skipped (normal Steam/GOG game), we still know the page via AppID for Part 2.

### Sidecar identity block

```json
"identity": {
  "identifiedTitle": "Cyberpunk 2077",
  "developer": "CD PROJEKT RED",
  "publisher": "CD PROJEKT RED",
  "pcgwPage": "Cyberpunk 2077",
  "pcgwUrl": "https://www.pcgamingwiki.com/wiki/Cyberpunk_2077",
  "steamAppId": "1091500",
  "confidence": "high",
  "identitySource": "steam-appid+pcgw-title"
}
```

`confidence`: `high` (store id → matching page), `medium` (name search), `low` (weak OpenSearch). Part 2 runs only on `high` or `medium`.

### Success (Part 1)

- [x] AppID path uses `appid.php` then Parse (fixture + live optional)  
- [x] Wrong OpenSearch hit (soundtrack) rejected  
- [x] Flag off → no HTTP, identity from cache or empty  
- [x] Fixture tests; no live Valve/PCGW in `dotnet test`

---

## 2. Part 2 — Populate details (after identity)

### Goal

One sidecar row has **structured** operator facts: paths, video caps, and a **catalog of command-line arguments with explainers**. The catalog is what F4 toggles. The right pane can show a short summary.

Not a dump of the 59KB wikitext.

### Extractors (each its own function + parser)

All read the **same** Parse wikitext already fetched in Part 1. No extra HTTP for Part 2.

| Function | Template / heading | Output |
|----------|-------------------|--------|
| `ParseGameDataPaths` | `{{Game data/config\|OS\|path}}`, `{{Game data/saves\|OS\|path}}` | `{ kind, os, pathTemplate }` |
| `ParseCommandLineTable` | `{{Standard table/row\|arg\|notes}}` under Command line arguments | `{ argument, notes }` |
| `ParseFixboxes` | `{{Fixbox\|description=...}}` under Essential improvements | `{ title, suggestedArgs, notes }` |
| `ParseVideoCaps` | `{{Video \|ultrawidescreen = true\|...}}` | selected keys only |

**Keep PCGW tokens** in storage: `{{P|game}}`, `{{P|localappdata}}`, `{{p|userprofile}}`.  
Resolve to Windows paths **only in the UI** (`%LOCALAPPDATA%`, `%USERPROFILE%`, install dir). Do not invent Linux paths.

### Video keys to keep (boolean / short text)

From `{{Video}}` (names vary by page): widescreen, ultrawidescreen, 4k, 60 fps, 120+ fps, fov slider, borderless, vsync, hdr.  
Skip WSGF award spam and screenshot `{{Image}}`.

### Explicitly out of Part 2

- Full Issues / Mods lists (huge, stale)  
- Auto-enabling any argument without the user toggling it  
- Downloading cover art  
- Save backup / cloud repair  
- Cargo queries  

### Sidecar `details` block (Cyberpunk-shaped example)

```json
"details": {
  "configPaths": [
    { "os": "Windows", "template": "{{P|localappdata}}\\CD Projekt Red\\Cyberpunk 2077" }
  ],
  "savePaths": [
    { "os": "Windows", "template": "{{p|userprofile}}\\Saved Games\\CD Projekt Red\\Cyberpunk 2077" }
  ],
  "commandLine": [
    { "argument": "--launcher-skip", "notes": "skips the separate launcher" },
    { "argument": "-skipStartScreen", "notes": "skips Breaching start screen" },
    { "argument": "-modded", "notes": "required for REDmod when skipping launcher" },
    { "argument": "-width X", "notes": "resolution width" }
  ],
  "fixes": [
    { "title": "Bypass launcher with --launcher-skip -skipStartScreen -modded", "suggestedArgs": "--launcher-skip -skipStartScreen -modded" }
  ],
  "video": { "ultrawide": null, "notes": "from {{Video}} when present" }
}
```

Fixbox args and the Standard table are **merged and deduped** by argument string.

---

## 3. Right pane (after Part 2)

Show only if the sidecar has the block. Identity first, then:

1. Developer / publisher (already 119)  
2. **Launch tips** — suggested bypass args (text; copy later)  
3. **Command line** — compact list, not the whole table if > 12 rows (show first 12 + “see PCGW”)  
4. **Config (Windows)** / **Saves (Windows)** — resolved tokens  
5. **Video** — two or three caps  
6. PCGW URL  

No new windows. No auto-launch change until the user toggles args in F4.

---

## 3a. F4 — catalog options → constructed launch args

Today F4 is one text box. Steam titles store `steam://rungameid/…` in `CommandLineArguments`; launch **drops** extra args on URI start. That is why toggling wiki flags into that field is wrong — we would either smash the URI or launch a bare exe.

**Goal:** Setup lists PCGW flags as **options**. Checking them **constructs** the argument string. Enter / play uses that string. No flag is on until the user checks it.

Composer already exists: `LaunchArgumentComposer` (`Toggle`, `Combine`, `ForProcessStart`). F4 and launch must both call it — do not re-split strings in the window.

### Split launch data

| Field | Owner | Role |
|-------|--------|------|
| `CommandLineArguments` | existing | Steam URI **or** legacy free-text for exe |
| `ExtraLaunchArguments` | **new** on `GameEntry` | Constructed extras from toggles + typed values. User-owned. |
| *(removed)* `LaunchExeWithExtras` | **bug** | Never invert Steam → raw exe |
| sidecar `details.commandLine[]` | PCGW catalog | `{ argument, notes, needsValue, source }` — options list only |

### How F4 builds the string

1. Read catalog from sidecar `Details.CommandLine` (same page as paths/video).  
2. Flags without a placeholder (`--launcher-skip`, `-windowed`) → checkbox + PCGW sentence.  
3. Check → `LaunchArgumentComposer.Toggle(extras, arg, true)`. Uncheck → `Toggle(..., false)`.  
4. Rows with `NeedsValue` (`-width X`) → **not** a checkbox; hint “type in extras”.  
5. Free-text box is the extras string (user can still type unknown flags).  
6. Preview line: `ForProcessStart(CommandLineArguments, extras)`.  
7. Save writes `ExtraLaunchArguments` + `UserOverrides`. Rescan must not wipe (same rule as today’s args).

```
PCGW launch options
[x] --launcher-skip     skips the separate launcher
[ ] -skipStartScreen    skips Breaching / start screen
[ ] -modded             load REDmod when skipping launcher
[ ] -windowed           windowed mode
    -width X            needs value — type below

Launch extras: --launcher-skip
Will start: C:\...\Cyberpunk2077.exe --launcher-skip
```

### How launch uses it

| Situation | Process |
|-----------|---------|
| Standalone / GOG exe | `FileName = ExecutablePath`, `Arguments = ForProcessStart(cmd, extras)` |
| Steam URI (`steam://rungameid/{id}`) | `FileName = steam://…`, extras **unused** |

Do **not** put extras into `CommandLineArguments` when that field is a URI.  
Do **not** invert Steam → raw exe.

### Landed

- `ExtraLaunchArguments` on `GameEntry` (exe launch only; Steam URI unchanged)  
- F4 checkbox list bound to sidecar catalog + extras preview  
- `GameLaunchResolver` + `MainWindow` launch (Steam URI if present; else exe + extras)

---

## 4. Pipeline (extends 119, does not fork it)

```
EnableOnlineMetadata?
    no  → cache only
    yes → Part 1 identify (Steam + PCGW page)
            confidence low? stop
          Part 2 parse same wikitext → details
          MetadataStore.Upsert sidecar only
          right pane binds identity + details
```

Still: never during scan; never on list highlight; 0.6s PCGW / 10s Steam; 30-day freshness.

---

## 5. Files

| File | Change |
|------|--------|
| `planning/119-metadata-sidecar.md` | 119 stays identity+basic extras; this plan owns details |
| `PcgwSectionParser.cs` | **new** — paths, table rows, Fixbox, Video |
| `CommonMetadataParser` / `GameMetadataRecord` | add `identity` + `details` (or nested records) |
| `MetadataService` | run Part 2 only after Part 1 page is set |
| `MainWindow.axaml` | new detail groups |
| `GameSetupWindow` | PCGW arg toggles + extras (exe launch only) |
| `GameEntry` / DTO / rescan merge | `ExtraLaunchArguments` |
| `MainWindow` launch | exe + extras; `steam://` default unchanged |
| `tools/probe_pcgw.py` | optional: dump section names + Game data / table counts |
| Tests | fixtures cut from Cyberpunk wikitext slices (config, saves, cmdline, Fixbox) |

`GamesDatabaseService` / `GameEntry` — **no schema change** except optional later “apply args” (not this plan).

---

## 6. Implementation order

1. **Fixtures** — Cyberpunk slices under `tests/.../Fixtures/pcgw/`. **done**  
2. **Part 1 harden** — refuse soundtrack/demo pages; never write `GameEntry.DisplayName`. **done**  
3. **`PcgwSectionParser`** + tests on fixtures (no HTTP). **done (parser; not wired to sidecar/UI)**  
4. **Sidecar schema** `Details` on `GameMetadataRecord`; merge keeps catalog. **done**  
5. **Right pane** Windows paths + short arg summary. **done**  
6. **F4 toggles** + `ExtraLaunchArguments` + launch compose. **done**  
7. Stop. **done**  
8. Later (same plan family, not a new number): online gate + F5 queue + F3 force lookup + year hint + multi-page picker. **done**

---

## 7. Success

- [x] Cyberpunk fixture: Windows config + save templates extracted  
- [x] Cyberpunk fixture: `--launcher-skip` and `-width X` present  
- [x] Confidence enum **not shipped** (declined — see As shipped)  
- [x] Lookup writes sidecar only (`games.json` launch fields untouched)  
- [x] Right pane shows paths/args only when sidecar `details` exists  
- [x] F4 lists sidecar catalog + notes; toggle writes `ExtraLaunchArguments` only  
- [x] Steam URI launch is never replaced by a raw exe  
- [x] Flag off: no HTTP  
- [x] DisplayName never overwritten by lookup  
- [x] F3 picker when OpenSearch returns ≥2 clean titles  
- [x] Year hint (`PeProductYear`) prefers e.g. Dead Space (2023) over 2008  

---

## 8. Out of scope

SyncMove, Epic GraphQL, Cargo, SteamDB (unless Part 1 still blind), IGDB, auto-applying launch args, cover download, Issues/Mods scrape, Linux path invention.
