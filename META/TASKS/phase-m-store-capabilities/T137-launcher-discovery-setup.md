# Task T137: Launcher Discovery Setup Choice

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High (startup/scan gating; the setting changes what is read)
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117B2 (launcher preview + policy wiring, done)

> **Test policy:** follow `planning/136-test-suite-reduction.md` — one test per behavior/failure, no
> app-version back-compat tests.

## Terms (do not conflate)

- **Display mode** — Library / Flat / Stores / Engines in the main window. T72/T73. **Not this task.**
- **Launcher discovery setting** — the Setup choice: Automatic / Selected / FileOnly. **This task.**
- **Game-folder scan** — F5/add-library scan. Existing.
- **Store inventory sync** — optional per-store launch check (T121), after the setting allows it.
- **Full rescan** — explicit Setup action: discovery follows the setting, then folders are scanned.

“Changing the launcher discovery setting” means changing this Setup choice, not changing the display.

## Implementation notes (resolved here)

- **Config property:** `AppConfig.LauncherDiscovery` (type `LauncherDiscoverySetting`) plus
  `SelectedLaunchers` (`IReadOnlyList<GameSourceKind>`). The enum is the setting; the property is not
  named “mode”.
- **Runtime holder:** one `LauncherDiscoveryContext` in `App/Services` holding `Setting` and
  `SelectedLaunchers`, initialized from config at startup and updated by setup. `FolderScanner`,
  `LibraryManager`, and the setup VM read it; they do not reload `settings.json` per folder.
- **Scanner gate:** `FolderScanner` takes an optional `Func<bool> allowRegistryFallback`; `LibraryManager`
  takes the context and skips the Epic ProgramData route when `AllowsEpicCatalog` is false. No scanner
  re-construction on setting change.
- **Selected list:** use `LauncherDiscoveryService.Supports(kind)` (add it) to show only stores with a
  locator as selectable. EA/Battle.net are informational; Arc is not a store.
- **Full rescan:** sequential `RescanLibrary` over configured anchors, after one optional discovery. No new
  service.
- **UI:** a setting selector plus a selected-launcher checklist (visible only for Selected); the existing
  preview/policy rows remain below. FileOnly hides both launcher sections and the Steam/Epic add buttons.

## Objective

Let the user choose how launchers are handled, persist it, and make every read follow it:

1. **Automatic** — detect all known launchers on an explicit action.
2. **Selected** — detect only the launchers the user says they have.
3. **FileOnly** — no launcher discovery/registry/ProgramData reads; scan only the folders the user adds.

The setting is asked on first run and can be changed later in Setup. Discovery is one-shot per action
(first run, Detect, setting change, full rescan), never continuous and never per launch. Full design and
matrix: `planning/138-launcher-discovery-setup.md`.

## What Needs to Change

### 1. Core model and config
- [x] Add `LauncherDiscoverySetting` (`Unset`, `Automatic`, `Selected`, `FileOnly`) in Core.
- [x] Add `AppConfig.LauncherDiscovery` (default `Unset`) and `SelectedLaunchers`
      (`IReadOnlyList<GameSourceKind>`, default empty). Keep selection separate from `StorePolicies`.
- [x] Map both in `JsonConfigService`; no schema-version fields.
- [x] Add one pure `LauncherDiscoveryPolicy` helper: `AllowsDiscovery(kind)`, `AllowsRegistryFallback`,
      `AllowsEpicCatalog`, `AllowsStorePolicies`. No service, no cache.

### 2. No startup probe
- [x] Remove `DiscoverAll()` from `App.axaml.cs` and its log line. Constructing the service is fine;
      probing before the user chooses is not.
- [x] No discovery from `ShellViewModel`, `MainWindow` construction, F5, or launch.

### 3. FileOnly gating
- [x] `FolderScanner` receives no `IRegistryReader` in `FileOnly`; `RegistryFallbackDetector` must not run.
- [x] `LibraryManager` does not route to `EpicLibraryScanner`/`EpicItemCatalog` in `FileOnly`; use
      `FolderScanner` with local `.egstore`/`.mancpn` signals.
- [x] `SteamLibraryScanner` stays file-based (ACF/vdf/common) and is allowed.
- [x] File signals still classify: EA `__Installer`, Ubisoft `uplay_*`, Battle.net `.battle.net`/`.build.info`,
      Xbox `default-metadata.json`, Rockstar `title.rgl`, GOG files.
- [x] No registry/ProgramData read in `FileOnly`; tests use counting readers/fakes.

### 4. Setup UI
- [x] First-run setup starts with the three launcher discovery settings and a one-line effect for each.
- [x] **Automatic:** “Detect launchers” → preview rows → per-store toggles; nothing enabled by default.
- [x] **Selected:** launcher checklist → detect only those → same preview/policy; a selected missing
      launcher shows `NotFound`/`Unsupported`. Only stores with a locator or an inventory reader are
      selectable; detection-only stores (EA, Battle.net, Arc) are shown as information, not checkboxes.
- [x] **FileOnly:** hide launcher discovery/store controls; keep Add folder and scan.
- [x] “Full rescan” action: discovery once in Automatic/Selected, skipped in FileOnly, then scan libraries.
- [x] “Change launcher discovery setting” section in F2 setup; changing it does not delete games/user data.
- [x] Effect text: no continuous scanning; discovery happens only on first run, Detect, setting change, or
      full rescan.

### 5. Changing the setting
- [x] Automatic → FileOnly: set FileOnly, disable store policies (keep rows), keep entries; no discovery.
- [x] FileOnly → Automatic/Selected: show the setting choice/preview; do not auto-enable any store.
- [x] Selected → Automatic: allow all locators; keep existing policies.
- [x] `Selected` with an empty set is allowed but detects nothing until the user checks a launcher.
- [x] Any change persists immediately; the next scan uses the new gate.

### 6. Tests
- [x] Setting + selected launchers round-trip through `settings.json`; default `Unset`.
- [x] Startup in every setting makes zero discovery calls (fake locator counter).
- [x] Automatic: one explicit action runs all locators; a second launch does not.
- [x] Selected: only chosen locators run; missing selected store is `Unsupported`.
- [x] FileOnly: zero registry reads, zero Epic ProgramData reads, zero locator calls; folder scan still
      detects Steam/EA/Ubisoft/Rockstar file signals.
- [x] Changing Automatic→FileOnly preserves games and policy rows; FileOnly→Automatic enables nothing.
- [x] Full rescan: discovery once/skip per setting, then scan.
- [x] View-model: setting choices, custom checklist, effect text, preview rows.

### 7. Docs
- [x] `docs/FAQ.md` / `GamingCommander.Readme.txt`: the three launcher discovery settings, what is read, no
      continuous launcher scan, how to change it.
- [x] `docs/ONLINE-AND-DATA.md`: FileOnly means no registry/ProgramData launcher reads; file signals only.
- [x] `META/CONNECTION_MAP.md`: launcher discovery setting gate in the scan pipeline.

## Requirements

- [x] No launcher probe at startup or per launch.
- [x] FileOnly performs no registry/ProgramData launcher reads and still scans folders.
- [x] Automatic/Selected probe only on an explicit action and never auto-enable a store.
- [x] Setting and selection persist and can be changed.
- [x] Changing the setting never deletes games, policies, tags, or overrides.
- [x] No new dependency, no second settings file, no schema version.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] Grep: no `DiscoverAll()` in `App.axaml.cs` or startup paths.
- [x] Counting `IRegistryReader` proves zero reads in FileOnly.
- [x] A FileOnly scan of a folder with a Steam/EA/Ubisoft/Rockstar marker still classifies it.
- [x] Automatic→FileOnly→Automatic leaves the catalogue intact.
- [x] Manual: first run asks the launcher discovery setting; change it later; full rescan follows it.

## Stop rules

- Do not add a watcher, timer, or background discovery.
- Do not make F5 a launcher probe; keep it as game-folder scan. Full rescan is an explicit Setup action.
- Do not delete entries when switching to FileOnly.
- Do not gate file-signal classification behind the launcher setting; FileOnly still uses files.
- Do not add store readers or new stores here.
- Do not use “mode” for this setting; “mode” belongs to the display (T72/T73).

## Open decisions

- Whether first-run allows adding folders before the launcher setting. Recommendation: choose the setting
  first; the rest of setup follows.
- Whether `Selected` stores are re-checked on full rescan. Recommendation: only on the explicit action.
- Whether an upgrade re-opens setup. Recommendation: keep the setting; no second prompt.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Core: `LauncherDiscoverySetting` (Unset/Automatic/Selected/FileOnly), `AppConfig.LauncherDiscovery`
    and `SelectedLaunchers` (separate from `StorePolicies`), and the pure `LauncherDiscoveryPolicy`
    (`AllowsDiscovery`, `AllowsRegistryFallback`, `AllowsEpicCatalog`, `AllowsStorePolicies`).
    `JsonConfigService` maps both fields without a schema version: a missing file loads `Unset`, an
    existing file without the field loads `Automatic` (legacy behavior).
  - Runtime: one `LauncherDiscoveryContext` built from config at composition and passed to
    `FolderScanner`, `LibraryManager`, `MainWindow`, and Setup. `FolderScanner` gained an optional
    `allowRegistryFallback` gate and builds `RegistryFallbackDetector` lazily, so a false gate performs
    zero registry reads. `LibraryManager` skips the Epic ProgramData route when the policy forbids it and
    falls through to `FolderScanner`; Steam stays file-based. Removed the startup `DiscoverAll()` call and
    its log line. `LauncherDiscoveryService` gained `Supports(kind)` and `DiscoverForPolicy(policy)`.
  - Setup UI: the launcher discovery card with the three settings and their one-line effects, a
    selected-launcher checklist containing only locator-backed stores, informational `Unsupported` rows
    for EA/Battle.net (and any other store without a locator), the existing preview/policy rows for
    Automatic/Selected, and a Full rescan action that runs one policy-allowed discovery then sequentially
    rescans every anchor. FileOnly hides the preview/store controls and the Steam/Epic add buttons while
    keeping Add Folder and per-anchor Rescan. Changing the setting persists immediately, updates the
    shared context, keeps the policy row (the launcher stays listed) while disabling the persisted
    `StorePolicy` flags so nothing stays active, and never enables a store. F5 is unchanged.
  - Tests: config default/legacy/round-trip, policy matrix, startup zero probes, scanner gate (disabled =
    zero reads, enabled = fallback), Epic FileOnly routing plus file-based Steam routing, and view-model
    setting/effect/checklist/selection/policy-disable behavior.
  - Docs: FAQ and Readme setup wording, ONLINE-AND-DATA FileOnly reads, connection map gate in the scan
    pipeline, CODE_MAP and CURRENT counts.
- **Verification:** `dotnet build GamingCommander.sln` succeeds with 0 errors; `dotnet test
  GamingCommander.sln` green, **557 tests** (Core 157, App 399, Migration 1). A full App rebuild still
  reports the pre-existing `AVLN3001` window-constructor warnings and two nullability warnings in
  `LauncherLocators`; no new warnings come from this change. Grep confirms no `DiscoverAll()` call in
  `App.axaml.cs` or startup paths. Counting reader proves zero FileOnly reads and a FileOnly folder scan
  still classifies file-signal games.
- **Review fix:** the first pass disabled the policy rows visually but left the persisted `StorePolicy`
  flags enabled, so a later launch check could resume a store after FileOnly. Setup now clears the
  persisted `IndexEnabled`/`CheckOnLaunch`/`EnrichNewGames` on the FileOnly transition (the launcher row is
  kept); the test asserts the saved policy is off, not just the checkbox.
- **Issues encountered:** the reviewed design replaced the plan's "pass no registry reader in FileOnly" with a per-scan
  `allowRegistryFallback` gate on the shared scanner (keeps existing constructors and avoids scanner
  re-construction). The setting-change discovery is raised by the view-model as an event and executed by the
  setup window once (FileOnly/Unset skip it), so the view-model itself stays I/O-free. Xbox/Rockstar remain
  informational until their locator task lands, and `StorePolicyProvider` still has no import/sync consumer
  (T118/T121). An Unset first run can still add folders before choosing; those scans behave like FileOnly
  (file signals only), and the setting card remains available in F2.
