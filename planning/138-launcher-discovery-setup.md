# 138 — Launcher Discovery Setup Choice

**Status:** implemented (2026-10-31). Implementation task: `T137` (completed).
**Scope:** the first-run/setup choice for launcher discovery, persistence of that choice, changing it later,
and the scan gates that follow.

---

## Terminology (read this first)

These are four different things. Do not use “mode” for more than one of them.

| Term | Meaning | Where |
|---|---|---|
| **Display mode** | What the main window shows: Library / Flat / Stores / Engines | T72/T73; **not this task** |
| **Launcher discovery setting** | The Setup choice: Automatic / Selected / FileOnly | **this task** |
| **Game-folder scan** | F5/add-library scan of the user's folders; file signals classify stores | existing |
| **Store inventory sync** | Optional per-store launch check, after the setting allows it | T121 |

**“Changing the launcher discovery setting”** means editing the Setup choice. It does **not** change the
display from flat to store-specific; that is the separate display mode.

**“Full rescan”** is an explicit Setup action: run launcher discovery according to the setting (skip it in
FileOnly), then scan the configured folders. F5 remains a game-folder scan and never probes launchers.

### Relationship to the cancelled T136 tasks

The setup choice changes *whether/when* discovery runs, not what is readable:

- **EA** and **Battle.net** stay detection-only. Their rows are informational `Unsupported`; no locator or
  reader is reintroduced by this task.
- **Arc** is not a store.
- **Ubisoft** has an approved registry inventory. A minimal registry-inventory locator is reinstated so the
  preview can advertise that source; it does not read the manifest.
- **Selected** mode lists only stores with a locator or inventory reader as selectable. Detection-only
  stores are shown as information, not checkboxes.

---

## 1. Proposal evaluation

The proposal replaces “always discover everything” with an explicit setup choice and separates three
operations that are currently conflated:

| Operation | Trigger | What it reads | Frequency |
|---|---|---|---|
| **Launcher discovery** | first run, “Detect launchers”, changing the setting, full rescan | launcher locations/inventory sources (registry, known paths, ProgramData) | once per action, **never per launch** |
| **Game-folder scan** | F5 / add library / full rescan | files under the user's roots | on demand |
| **Store inventory sync** | launch check, if the user opted in | the launcher's inventory | only for enabled stores, after the setting decision |

The rule: discovery says where a launcher is; it never proves a game is installed, and it does not run
continuously. The launcher discovery setting decides whether discovery/import is allowed at all.

### Benefits

- Permission is explicit: auto-detect, a selected subset, or file-only.
- Unknown stores no longer force a choice; FileOnly is a complete, supported setting.
- Fewer registry/ProgramData reads: no startup probe, no background probe.
- The setting can be changed later; the first choice is not permanent.
- FileOnly preserves the core: folder/file signals still classify games.

### Risks / constraints

- Setting gating must not silently change detection. FileOnly must still detect file signals
  (`__Installer`, `uplay_*`, `.battle.net`, `title.rgl`, `steam_appid`, ACF/vdf, etc.).
- The setting must not be confused with per-store policy: setting allows discovery; policy says what to do
  with a discovered store.
- Changing the setting must not delete games or user data. It changes what may run next, not what exists.
- `App.LauncherDiscovery.DiscoverAll()` currently runs at startup for a log line; that is exactly the
  behaviour this proposal forbids.

---

## 2. The three launcher discovery settings

```csharp
public enum LauncherDiscoverySetting
{
    Unset,       // first run: ask; no probing before the choice
    Automatic,   // detect all known launchers on an explicit action
    Selected,    // detect only the launchers the user selected
    FileOnly,    // no launcher probing; scan only the folders the user adds
}
```

### Automatic

- First run: “Scan for installed launchers”.
- On the explicit action (first run, Detect, full rescan), run all registered locators once.
- Show results; **do not enable any store by default**.
- The user still chooses Index / Check on launch / Enrich per store (policy).

### Selected

- First run: the user checks the launchers they have (Steam, Epic, GOG, EA, Ubisoft, Battle.net, Xbox,
  Rockstar).
- Only those locators run; a selected but missing launcher shows `NotFound`/`Unsupported`.
- Persist the selected set separately from per-store action policies.

### FileOnly

- No launcher discovery, no registry probing, no ProgramData inventory read.
- The user adds game folders/libraries and scans them. File signals still classify stores:
  Steam structure/ACF, Epic local `.egstore`/`.mancpn`, EA `__Installer`, Ubisoft `uplay_*`, Battle.net
  `.battle.net`/`.build.info`, Xbox `default-metadata.json`, Rockstar `title.rgl`, GOG files.
- The launcher/preview section is hidden; store policies are not actionable.
- Existing store-imported entries are **kept**, not deleted; they are not refreshed by a launcher.
- `FolderScanner` is constructed without a registry fallback; `LibraryManager` does not use the ProgramData
  Epic catalog. This is the only setting that changes scanning code paths.

---

## 3. Decision matrix by setting

| Setting | Launcher discovery | Registry fallback in folder scan | Epic ProgramData catalog | Store policies | Game-folder scan |
|---|---|---|---|---|---|
| Unset (first run) | no, until chosen | no | no | none | after the choice |
| Automatic | yes, on action | yes | yes | available | yes |
| Selected | only selected kinds | yes | yes if Epic selected | available for selected | yes |
| FileOnly | no | **no** | **no** | disabled | yes, file signals only |

---

## 4. What exists today

| Area | Current | Gap |
|---|---|---|
| `AppConfig` | no launcher setting/selection | add setting + selected set |
| `JsonConfigService` | maps settings | map the new fields |
| `LauncherDiscoveryService` | constructed at startup; `DiscoverAll()` called for a log | remove the startup call; on-demand only |
| `LauncherDiscoveryService` locators | Steam/Epic registered | filter/disable by setting |
| `LibrarySetupViewModel/Window` | add roots, Steam/Epic buttons | no launcher setting choice, no preview |
| `StorePolicyProvider` | exists, unwired | T117B2 wires it; this task consumes it |
| `FolderScanner` | always receives `IRegistryReader` | add a no-registry construction for FileOnly |
| `LibraryManager` | routes Epic by type/manifests dir | gate the Epic catalog in FileOnly |
| F5 rescan | rescans game libraries | no separate full rescan that includes discovery |
| Startup | `needsWizard` opens setup | the launcher setting prompt belongs in that dialog |

---

## 5. Required changes

### Core

- [x] `LauncherDiscoverySetting` enum in `Core/Models`.
- [x] `AppConfig.LauncherDiscovery` (default `Unset`) and `AppConfig.SelectedLaunchers` (list of
      `GameSourceKind`, default empty). Keep selected launchers separate from `StorePolicies`.
- [x] Config DTO mapping; no version/back-compat fields.
- [x] One pure `LauncherDiscoveryPolicy` helper: `AllowsDiscovery(kind)`, `AllowsRegistryFallback`,
      `AllowsEpicCatalog`, `AllowsStorePolicies`. No service, no cache.

### App services

- [x] Remove the startup `DiscoverAll()` call and its log line.
- [x] `LauncherDiscoveryService` gains a setting-aware `DiscoverForSetting(setting, selected)` wrapper (or
      the caller filters); it must never run in `FileOnly`.
- [x] `FolderScanner`: optional `allowRegistryFallback` gate; the registry detector is built lazily, so
      FileOnly performs zero registry reads and still classifies file signals.
- [x] `LibraryManager`: gate `EpicLibraryScanner`/`EpicItemCatalog`; `FileOnly` uses `FolderScanner`.
- [x] `StorePolicyProvider`: independent of the launcher setting; the setup UI wires it (T117B2). FileOnly disables actions
      without deleting policy rows.
- [x] `App.axaml.cs`: build scanner/library manager from the discovery context; no probe.

### Setup UI (T117B2 + this task)

- [x] First-run screen starts with the three launcher discovery settings and a one-line effect for each.
- [x] Automatic: Detect launchers → preview rows → per-store toggles.
- [x] Selected: launcher checklist → detect only those → same preview/policy. Only stores with a locator or
      inventory reader are selectable; EA/Battle.net/Arc are informational `Unsupported` rows.
- [x] FileOnly: hide launcher discovery/store controls; keep Add folder and scan.
- [x] “Full rescan” action: discovery once in Automatic/Selected, skipped in FileOnly, then scan libraries.
- [x] “Change launcher discovery setting” section in F2 setup; changing it does not delete games/user data.
- [x] Effect text: no continuous scanning; discovery happens only on first run, Detect, setting change, or
      full rescan.

### Changing the setting

- [x] Automatic → FileOnly: set FileOnly, disable store policies, keep entries; no discovery.
- [x] FileOnly → Automatic/Selected: show the setting choice/preview; do not auto-enable any store.
- [x] Selected → Automatic: allow all locators; keep existing policies.
- [x] Selecting `Selected` with an empty set is allowed but detects nothing until the user checks a launcher.
- [x] Any change persists immediately; the next scan uses the new gate.

### Docs

- [x] `docs/FAQ.md` / `GamingCommander.Readme.txt`: the three launcher discovery settings, what is read, no
      continuous launcher scan, how to change it.
- [x] `docs/ONLINE-AND-DATA.md`: FileOnly means no registry/ProgramData launcher reads; file signals only.
- [x] Connection map: the launcher discovery setting gate in the scan pipeline.

---

## 6. Tests

- [x] Setting + selected launchers round-trip through `settings.json`; default `Unset`.
- [x] Startup in every setting makes **zero** discovery calls (fake locator counter).
- [x] Automatic: one explicit action runs all locators; a second launch does not.
- [x] Selected: only chosen locators run; a missing chosen store is `Unsupported`.
- [x] FileOnly: zero registry reads, zero Epic ProgramData reads, zero locator calls; folder scan still
      detects Steam/EA/Ubisoft/Rockstar file signals.
- [x] Changing Automatic→FileOnly disables policies but keeps games; FileOnly→Automatic enables nothing.
- [x] Full rescan: discovery once/skip per setting, then scan.
- [x] View-model: setting choices, custom checklist, effect text, preview rows.

## 7. Open decisions (resolve in the task)

1. **First run before a setting is chosen.** Recommendation: the setting card is first; FileOnly/Custom/
   Detect actions follow the choice.
2. **Selected set vs StorePolicies.** Recommendation: keep separate; policy is “what to do”, not “what to
   look for”.
3. **FileOnly and existing store entries.** Recommendation: keep them; do not delete; they stop being
   refreshed by a launcher. State that in the effect text.
4. **Version upgrades.** Keep the setting; do not re-ask on every upgrade.
5. **Full rescan shortcut.** Recommendation: a Setup action; F5 stays game-folder scan.

## 8. Non-goals

- No continuous launcher monitoring, watcher, or background probe.
- No new store clients or inventory readers.
- No change to existing file-signal classification.
- No second settings file; the setting lives in `settings.json`.
- No automatic enabling of a store based on detection.

## 9. Acceptance

- First run presents the three launcher discovery settings and their effects.
- The choice persists and can be changed later.
- No launcher probe occurs at startup or per launch.
- FileOnly performs no registry/ProgramData launcher reads, while folder scanning still works.
- Automatic/Selected probe only on explicit actions and never auto-enable a store.
- Full rescan follows the setting; F5 remains game scan.
- Tests above pass; existing detection matrix unchanged for the same roots and setting.
