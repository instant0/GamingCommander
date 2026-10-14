# Task T78: Epic .item AppName Precedence + EOSH Expectation Copy

**Tier:** 3 — Logic/Behavior
**Phase:** J — Post-126 Follow-ups
**Effort:** ~45 min
**Risk:** Medium
**Status:** **DEFERRED (2026-10-14)**
**Prerequisites:** None (Plan 126 investigation closed)

> **DEFERRED — manifest re-creation is a dead end.** Plan 126 could not make Epic `.item`
> re-creation register a game, and the user has directed that this line of work be deferred. The
> supported path is **detection from existing manifest files** (read `.item`/`.mancpn`/`.manifest`),
> not writing them. Keep this task as recorded knowledge for a possible future revisit; do **not**
> implement the writer changes now. The AppName precedence finding is preserved below.

---

## Objective (deferred)

Plan 126 proved that the Epic `.mancpn` AppName is authoritative and that the `.manifest`
FString[0] "strip Staging" rule is **Death-Stranding-specific** (old `.ovt`-era install). The writer
currently ignores the `.mancpn`/`.ovt` AppName it already reads and prefers the manifest value, so it
can emit the wrong `AppName` for `.mancpn`-era installs (The Captain, Shadowrun). Also, the repair UI
promises registration that Plan 126 proved impossible for apps EOSH does not report. Fix the write
precedence and the expectation copy.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/EpicItemWriter.cs`
**File:** `src/GamingCommander.App/Services/EpicItemWriter.cs`
**Current state:** line 47 reads `.mancpn`/`.ovt` AppName into `localApp`; lines 82-83 set
`app = appFromManifest ?? folder`, ignoring `localApp`. Comment claims "AppName = .manifest strip
Staging. Never .ovt sub."
**Actions:**
- [ ] Split the AppName capture: keep `.mancpn` AppName and `.ovt` AppName distinct (e.g. `mancpnApp`, `ovtApp`).
- [ ] Set precedence: `mancpnApp` → `appFromManifest` (strip-Staging, old `.ovt` installs) → `ovtApp` → folder leaf.
- [ ] Correct the comment: `.mancpn` is authoritative; manifest strip-Staging is DS-specific.

### 2. `docs/research/epic_item_format.md`
**Current state:** states "AppName = `.manifest` strip Staging" as a general rule (~line 12, Required
Fields table ~line 63, ~line 258). Optimistic wording about identification `.item`s surviving.
**Actions:**
- [ ] Mark the strip-Staging rule as **Death-Stranding-specific** (old `.ovt` installs without `.mancpn`).
- [ ] Add the `.mancpn`-authoritative rule and cross-reference `planning/126-facts-identity.md`.
- [ ] Correct the "accepted/survives" wording: as of launcher 20.2.9, an identification-only `.item` for an app EOSH does not report is removed within ~300 ms (`planning/126-facts-eosh.md` §8o/8r).

### 3. `src/GamingCommander.App/Services/EpicRepairDialog.cs` + `src/GamingCommander.App/MainWindow.PlatformRepair.cs`
**Current state:** dialog says Epic can "officialize" the written `.item`; status message implies one Update registers it.
**Actions:**
- [ ] Add the EOSH caveat to the dialog text: writing an identification `.item` only helps an install EOSH already reports; an unreported game is removed by the launcher.
- [ ] Keep the "one Update per launcher run" operational advice (still true).

### 4. `planning/121-epic-manifest-vfs-investigation.md`
**Current state:** describes orphan `.item` write without the EOSH limit.
**Actions:**
- [ ] Add a one-line note that orphan `.item` write is identification-only and cannot register an app absent from the EOSH reported set (see Plan 126).

## Context

- Evidence: `planning/126-facts-identity.md` (AppName source per era), `planning/126-facts-eosh.md` §8j/8o/8r/8t/8u.
- The correct precedence was proven field-for-field by the Ruiner reinstall (`.mancpn` identity matched Epic's own output).
- No GraphQL for ids (documented constraint, unchanged).

## Requirements

- [ ] `.mancpn` AppName wins over a differing manifest FString[0]; unit test added.
- [ ] Old `.ovt`-only install (no `.mancpn`) still uses manifest strip-Staging (Death Stranding case); test added.
- [ ] `epic_item_format.md` no longer states the DS rule as general.
- [ ] Repair dialog + status message carry the EOSH caveat.
- [ ] No catalog UUIDs invented.

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (EpicItemWriterTests + new cases green)
- [ ] Grep: no remaining "Never .ovt sub" / general strip-Staging claim in `docs/research/epic_item_format.md`

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
