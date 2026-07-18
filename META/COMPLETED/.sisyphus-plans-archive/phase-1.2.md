# Phase 1.2: Research & Data Collection

## Goal
Validate parsing approaches for game store data formats (Steam ACF, Epic manifests, standalone directories) using Python helper scripts. The objective is to extract *just enough* information to identify games, generate configuration files, and support migration. Scripts are **development-environment only** and MUST NOT disclose specific game names, paths, or registry keys back to the Agent. Confirm parsing logic against one representative sample per format and document the required structural schemas generically.

## Approach
We know the base structural format for all data sources. Full dataset enumeration is not required. Scripts validate the parsing approach against a single representative sample for each format:
- **One ACF file** → Extract key fields for identification, configuration, and migration.
- **One `common/` folder listing** → Validate cross-reference logic with ACF AppID.
- **One Epic JSON manifest** → Confirm schema parsing for essential identification/configuration fields.
- **One standalone folder with `.egsstore`** → Confirm EGS marker detection.

All output from scripts stays within `tools/` or `docs/research/` as generic structural documentation only.

> **Note on existing tools:** The `tools/` directory already contains `detect_folder.py`, `parse_manifest.py`, `epic_search.py`, and related scripts that partially overlap Phase 1.2. New scripts should reference and complement these rather than duplicate them.

---

## Tasks

### 1. Steam ACF Parsing (Objectives Focused)
- **Objective**: Develop a Python script to parse a single `appmanifest_*.acf` file, extracting *only* the fields necessary for game identification and migration support. The goal is to ensure we can read enough information to fulfill these core functionalities, not to achieve a full understanding of the ACF format. Provide Python tests to achieve these two objectives.
- **Agent Execution Notes**:
    - Script: `tools/parse_steam_acf.py`
    - **Required Fields**: `appid`, `name`, `installdir`, `StateFlags`, `LastUpdated`, `SizeOnDisk`, `buildid`. **IGNORE all other fields.** These are sufficient for identifying the game, its location, and key status information relevant to migration.
    - Validation: Verify the script correctly extracts these specific fields from the provided `appmanifest.acf` sample in the `data/` folder.
    - Output: Document the schema for **only these required fields** in `docs/research/steam_acf_schema.md`. The documentation should clearly state these are the fields relevant for identification and migration support. **Do not include sample data or specific game names.** Assume paths within ACF files are Windows-style (`\`).
- [x] Write `tools/parse_steam_acf.py`
- [x] Implement Python tests to achieve identification and migration support objectives.
- [x] Extract specified fields.
- [x] Validate output structure.
- [x] Document generic schema for required fields.

### 2. Steam `common/` Folder Cross-Reference (One Library Root)
- **Objective**: Create a script to list directories under `steamapps/common/` and validate its cross-reference logic with Steam ACF `installdir` fields.
- **Agent Execution Notes**:
    - Script: `tools/list_steam_common.py`
    - Focus: Validate the *logic* of matching `installdir` from ACF files to folder names. **Do not list all common folders; focus on the validation process.**
    - Output: Document the cross-reference approach in `docs/research/steam_common_schema.md`. **Ignore specific folder names.** Assume paths are Windows-style.
- [x] Write `tools/list_steam_common.py`
- [x] Implement cross-reference logic validation.
- [x] Document the cross-reference approach.

### 3. Steam Library Folder Discovery
- **Objective**: Develop a script to parse `libraryfolders.vdf` and identify all Steam library paths, including a registry fallback. Understand VDF structure *only as needed* for this task.
- **Agent Execution Notes**:
    - Script: `tools/discover_steam_libraries.py`
    - VDF Parsing: Use the `vdf` PyPI package or a simple recursive descent parser. **Focus only on parsing keys relevant to library paths.** Ignore other VDF structures.
    - Registry Fallback: Implement check for `HKCU\Software\Valve\Steam\SteamPath` (Windows registry path). 
    - Output: Document the VDF format *sufficient for library path discovery* in `docs/research/steam_vdf_schema.md`. **Do not document the entire VDF specification.** Assume paths are Windows-style.
- [x] Write `tools/discover_steam_libraries.py`
- [x] Implement VDF parsing for library paths.
- [x] Implement registry fallback.
- [x] Document VDF structure for library path discovery.

### 4. Standalone Directory Analysis (One Sample)
- **Objective**: Create a script to scan a sample directory for standalone games, focusing on identifying executables and EGS marker files.
- **Agent Execution Notes**:
    - Script: `tools/list_standalone_games.py`
    - Focus: Detect `.egsstore` marker files and list `.exe` files within game folders. **Ignore all other file types or directory structures.** Assume paths scanned are Windows-style.
    - Output: Document the detection structure in `docs/research/standalone_schema.md`. **Do not list specific game folder contents.**
- [x] Write `tools/list_standalone_games.py`
- [x] Implement marker file detection and `.exe` listing.
- [x] Validate detection logic against one sample.
- [x] Document the detection structure.

### 5. Epic Games Store Manifest Parsing (One Sample)
- **Objective**: Develop a script to parse Epic's JSON manifests and `LauncherInstalled.dat` to extract essential game identification and configuration data.
- **Agent Execution Notes**:
    - Scripts: `tools/parse_epic_manifests.py` (for manifests), and logic to parse `LauncherInstalled.dat`.
    - **Required Fields from Manifests**: `AppId`, `AppName`, `DisplayName`, `InstallLocation`, `LaunchExecutable`. **IGNORE all other fields.**
    - **Required Fields from `LauncherInstalled.dat`**: `InstallLocation`, `AppName`, `AppVersion` (from the `InstallationList` array). **IGNORE all other data.**
    - **Path Assumption**: All paths encountered within Epic manifests and `LauncherInstalled.dat` are assumed to be Windows-style (`\`).
    - Validation: Parse one sample manifest and `LauncherInstalled.dat`.
    - Output: Document the schema for **only these required fields** in `docs/research/epic_manifest_schema.md`.
- [ ] Write `tools/parse_epic_manifests.py`
- [ ] Implement parsing for JSON manifests and `LauncherInstalled.dat`.
- [x] Extract specified fields.only.
- [ ] Validate schema parsing against one sample.
- [ ] Document schema for required fields.

### 6. PCGamingWiki Research
- **Objective**: Research PCGamingWiki data formats to identify useful fields for game metadata caching, focusing on what's needed for identification, configuration, and migration.
- **Agent Execution Notes**:
    - Focus: Identify fields like genre, save locations, and system requirements that are essential for enriched game data. **Ignore fields not directly relevant to these core functions.**
    - Output: Document findings and a plan for local metadata cache strategy in `docs/research/pcgamingwiki_notes.md`.
- [ ] Research PCGamingWiki data format (API or scraped).
- [ ] Identify essential fields for metadata caching.
- [ ] Document plan for local metadata cache strategy.
- [ ] Output to `docs/research/pcgamingwiki_notes.md`.

### 7. Other Launchers — Registry & Manifest Locations
- **Objective**: Document registry paths and manifest locations for GOG, EA App, and Ubisoft Connect, focusing on data needed for game identification and configuration.
- **Agent Execution Notes**:
    - Focus on registry keys (`HKLM\...`) and file markers that provide game IDs, install paths, and launch executables. **Ignore complex internal manifest formats (e.g., Ubisoft's binary protobuf) if they are not essential for basic identification/configuration.** Defer complex manifest parsing to Phase 3.
    - **Path Assumption**: Documented registry paths and file locations are for Windows systems.
    - Output: Document findings in `docs/research/launcher_discovery.md`.
- [ ] Document GOG Galaxy registry paths and file markers.
- [ ] Document EA App / Origin registry paths and file markers.
- [ ] Document Ubisoft Connect registry paths.
- [ ] Output to `docs/research/launcher_discovery.md`.

### 8. Launch URI Schemes
- **Objective**: Document the launch URI schemes for major game launchers.
- **Agent Execution Notes**:
    - Focus: Record the exact URI format for launching games via Steam, Epic, GOG, EA, and Ubisoft. **No other details about the launch process are required.** These URIs are platform-specific (Windows).
    - Output: Document these schemes in `docs/research/launch_schemes.md`.
- [ ] Document per-launcher launch URI schemes.
- [ ] Output to `docs/research/launch_schemes.md`.

### 9. Validation Script
- **Objective**: Create a script to run all developed parsers against sample data to confirm correct structure and minimal data extraction.
- **Agent Execution Notes**:
    - Script: `tools/validate_approach.py`
    - Focus: Ensure each parser runs without errors and extracts the *specified minimal fields*. **The output of this script is strictly developer-only and must not contain any game-specific data.** The script will be developed with awareness of Windows pathing conventions for the data it's analyzing.
    - Output: Confirm correct structure. The C# implementation will rely on this validation.
- [ ] Write `tools/validate_approach.py`
- [ ] Implement logic to run all parsers against samples.
- [ ] Confirm correct structure and minimal data extraction.

---

## Deliverables
- [x] `tools/validate_steam_libraries.py`
- [x] `tools/parse_steam_acf.py`
- [x] `tools/list_steam_common.py`
- [x] `tools/discover_steam_libraries.py`
- [x] `tools/list_standalone_games.py`
- [ ] `tools/parse_epic_manifests.py`
- [ ] `tools/validate_approach.py`
- [x] `docs/research/steam_acf_schema.md`
- [x] `docs/research/steam_common_schema.md`
- [x] `docs/research/steam_vdf_schema.md`
- [x] `docs/research/standalone_schema.md`
- [ ] `docs/research/epic_manifest_schema.md`
- [ ] `docs/research/pcgamingwiki_notes.md`
- [ ] `docs/research/launcher_discovery.md`
- [ ] `docs/research/launch_schemes.md`

---

## Exit Criteria
Phase 1.2 is complete when:
- Each Python parser successfully runs against one sample and extracts **only the specified minimal fields** without errors.
- All schema documentation in `docs/research/` describes the structure for **only the required fields** and contains no concrete game/library data.
- The `validate_approach.py` script confirms correct parsing structure and minimal data extraction for all implemented parsers, with an awareness of Windows pathing conventions.
- Phase 2 can begin implementation with sufficient, documented data schemas to enable game identification, configuration generation, and migration.

(End of file - total 161 lines)

### 3b. Cross-Library Validation
- **Objective**: Create a validation tool that runs health checks across ALL Steam libraries at once, reporting per-library complete/missing/orphaned counts.
- **Agent Execution Notes**:
    - Script: `tools/validate_steam_libraries.py`
    - Reads `libraryfolders.vdf` to discover all libraries, then cross-references ACFs ↔ common/ folders per library.
    - Produces a JSON health report suitable for feeding the application's game database.
    - Handles `installdir` values that are full absolute paths (Steam stores these when the folder name doesn't match the ACF's expected name).
- [x] Write `tools/validate_steam_libraries.py`
- [x] Run health check against all real libraries
- [x] Document approach in existing schema docs

