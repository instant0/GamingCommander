# Task T117A: Locators for Steam, Epic, GOG, EA, Ubisoft

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T117A depends on Task T117

## Objective

Implement the file-backed launcher locators. Each returns the launcher install path and the
inventory source we can read, with an explicit read-only safety rating.

## What Needs to Change

### 1. Steam
- [ ] Probe `HKLM\SOFTWARE\Valve\Steam\InstallPath`, `HKCU\Software\Valve\Steam\SteamPath`, the `WOW6432Node` variant, and the default path.
- [ ] Confirm the library source: `steamapps\libraryfolders.vdf` + `steamapps\common`.
- [ ] `ReadOnlySafety.Safe`.

### 2. Epic
- [ ] Probe `HKLM\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher\AppDataPath`, the default `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests`, and `LauncherInstalled.dat` presence.
- [ ] `ReadOnlySafety.Safe` (identification only; no registration).

### 3. GOG
- [ ] Probe `GOG.com\GalaxyClient\paths\client` / `common`, `GOG.com\DefaultPackPath`, and the default Galaxy path.
- [ ] Mark the Galaxy SQLite DB as `NeedsVerification`; the per-game registry + `.info` are `Safe`.

### 4. EA
- [ ] Probe `Electronic Arts\EA Desktop\InstallLocation` / `LauncherAppPath`, `EA Core\ClientPath`, and the legacy EA Games path.
- [ ] Mark the encrypted `IS` inventory as `Unsupported`; legacy `installerdata.xml`/`.mfst` as `NeedsVerification`.

### 5. Ubisoft
- [ ] Probe `Ubisoft\Launcher\InstallDir` and `Ubisoft\Launcher\Installs\{gameId}\InstallDir`.
- [ ] Registry is `Safe`; `uplay_install.manifest` is `NeedsVerification`.

## Context

- Evidence and paths: `docs/research/launcher_discovery.md`, `planning/129` §2.
- Use the repository's registry abstraction (`IRegistryReader`) so tests can use mock `.reg` data.

## Requirements

- [ ] Every locator validates that a plausible inventory source exists before reporting `Found`.
- [ ] Missing keys/values return `Not found` with a reason, not an exception.
- [ ] No writes.

## Verification

- [ ] Unit tests with mock registry keys and temp folders for each store.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] No regression to `SteamInstallPathLocator`.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
