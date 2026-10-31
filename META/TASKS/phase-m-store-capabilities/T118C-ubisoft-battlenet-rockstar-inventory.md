# Task T118C: Ubisoft + Battle.net + Rockstar Inventory Readers

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T118C depends on Task T118

## Objective

Read the Ubisoft, Battle.net, and Rockstar inventories read-only. Verify each format before parsing
it; do not decrypt or write.

## What Needs to Change

### 1. Ubisoft
- [ ] Enumerate `Ubisoft\Launcher\Installs\{gameId}\InstallDir` as inventory.
- [ ] Persist the `{gameId}` in `StoreInfo`/`GameIdentity`.
- [ ] Read `uplay_install.state` where the format is confirmed; mark `uplay_install.manifest` as `NeedsVerification` (binary/encrypted) and do not parse it.
- [ ] Missing folder → `Missing`.

### 2. Battle.net
- [ ] Use game-root markers (`.build.info`, `.product.db`, `.patch.result`, `.battle.net/`) and the
      documented Agent registry key. Verify the key before coding.
- [ ] Do **not** read `%ProgramData%\Battle.net\Agent\product.db`: it is an unverified SQLite store, and
      "read it without a SQLite dependency" is not possible. If it is ever needed, it becomes its own task
      with an explicit dependency decision.
- [ ] Read `.build.info` product code/version from the game folder.
- [ ] Missing folder → `Missing`; absence of a marker is not evidence of removal.

### 3. Rockstar
- [ ] Read `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json` library folders + per-game paths (schema verification first).
- [ ] Enumerate per-game registry keys; persist a title ID if `title.rgl` can be read safely.
- [ ] Missing folder → `Missing`.

## Context

- Evidence: `docs/research/blizzard_format.md`, `ubisoft_format.md`, `rockstar_format.md`.
- Existing: `RegistryFallbackDetector` already enumerates parts of all three; reuse it.
- Verification first: if a schema is unconfirmed, mark the reader `NeedsVerification` and fall back to registry/known paths.

## Requirements

- [ ] Each reader is read-only and returns records for missing folders.
- [ ] No SQLite dependency is added; the Agent `product.db` is explicitly out of scope.
- [ ] Absence of a registry entry or marker does not mark an existing game `Missing`.
- [ ] No writes or decryption.

## Verification

- [ ] Fixtures for registry-only, file-present, and missing-folder cases.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Explicit "needs verification" note for any unconfirmed format.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
