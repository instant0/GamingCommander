# Phase 1.2: Research & Data Collection

## Goal

Validate parsing approaches for Steam ACF files, Epic manifests, and standalone directories using Python helper scripts. Scripts are **development-environment only** — they never disclose output or findings back to the Agent. Confirm the parsing logic is correct against one representative sample per format, then document the structural schemas generically.

> **Privacy constraint:** Scripts operate on local machine data. Research findings are documented as generic structural notes in `docs/research/`, never as concrete paths, game names, or library contents that would disclose the developer's environment.

## Approach

We already know the base structural format of all three data sources. Full dataset enumeration is not the goal. Scripts validate the parsing approach against one representative sample each:
- **One ACF file** → confirm field extraction logic.
- **One `common/` folder listing** → confirm cross-reference logic with ACF AppID.
- **One Epic JSON manifest** → confirm schema parsing.
- **One standalone folder with `.egsstore`** → confirm EGS marker detection.

All output stays in `tools/` or `docs/research/` as generic structural documentation only.

---

## Tasks

### 1. Steam ACF Parsing (One Sample)

- [ ] Write `tools/parse_steam_acf.py`: parse one `appmanifest_*.acf` file.
- [ ] Extract fields: `AppID`, `AppName`, `installdir`, `StateFlags`, `LastUpdated`.
- [ ] Validate output structure is correct against known schema.
- [ ] Document generic ACF field schema in `docs/research/steam_acf_schema.md` — no concrete game names.

### 2. Steam `common/` Folder Cross-Reference (One Library Root)

- [ ] Write `tools/list_steam_common.py`: list folders under `steamapps/common/` for one library root.
- [ ] Cross-reference `installdir` from ACF against folder names — validate the logic.
- [ ] Document cross-reference approach in `docs/research/steam_common_schema.md`.

### 3. Steam Library Folder Discovery

- [ ] Write `tools/discover_steam_libraries.py`: read `libraryfolders.vdf` to enumerate all Steam library paths.
- [ ] Handle VDF parsing (Valve Data Format — quoted keys, nested blocks).
- [ ] Document VDF format in `docs/research/steam_vdf_schema.md`.

### 4. Standalone Directory Analysis (One Sample)

- [ ] Write `tools/list_standalone_games.py`: scan one library root (e.g. `Y:\Games`).
- [ ] Detect `.egsstore` subfolder as EGS signal.
- [ ] List `.exe` files per game folder.
- [ ] Validate detection logic against one folder containing `.egsstore`.
- [ ] Document structure in `docs/research/standalone_schema.md`.

### 5. Epic Games Store Manifest Parsing (One Sample)

- [ ] Write `tools/parse_epic_manifests.py`: read one JSON manifest from `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\`.
- [ ] Extract fields: `AppId`, `AppName`, `DisplayName`, `InstallLocation`, `LaunchExecutable`.
- [ ] Validate schema parsing against one sample.
- [ ] Document schema in `docs/research/epic_manifest_schema.md`.

### 6. PCGamingWiki Research

- [ ] Research PCGamingWiki data format: API endpoint or scraped data.
- [ ] Identify useful fields: genre, developer, publisher, save locations, compatibility notes, system requirements.
- [ ] Document plan for local metadata cache strategy.
- [ ] Output: `docs/research/pcgamingwiki_notes.md`.

### 7. Other Launchers — Registry & Manifest Locations

- [ ] **GOG Galaxy**: document registry keys and local manifest/database files.
- [ ] **EA App**: document registry paths and install location conventions.
- [ ] **Ubisoft Connect**: document manifest locations and registry paths.
- [ ] Output: `docs/research/launcher_discovery.md`.

### 8. Validation Script

- [ ] Write `tools/validate_approach.py`: run all parsers against one sample each, confirm correct structure.
- [ ] Output is developer-only — confirms the C# implementation will parse correctly.

---

## Deliverables

- [ ] `tools/parse_steam_acf.py`
- [ ] `tools/list_steam_common.py`
- [ ] `tools/discover_steam_libraries.py`
- [ ] `tools/list_standalone_games.py`
- [ ] `tools/parse_epic_manifests.py`
- [ ] `tools/validate_approach.py`
- [ ] `docs/research/steam_acf_schema.md`
- [ ] `docs/research/steam_common_schema.md`
- [ ] `docs/research/steam_vdf_schema.md`
- [ ] `docs/research/standalone_schema.md`
- [ ] `docs/research/epic_manifest_schema.md`
- [ ] `docs/research/pcgamingwiki_notes.md`
- [ ] `docs/research/launcher_discovery.md`

---

## Exit Criteria

Phase 1.2 is complete when:
- Each Python parser validates correctly against one sample — no errors, correct field extraction.
- All schema documentation in `docs/research/` describes structure only, with no concrete game/library data.
- Phase 2 can begin implementation without any further data format research.
