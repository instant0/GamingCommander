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
- [ ] Verify whether `%ProgramData%\Battle.net\Agent\product.db` is SQLite and document its schema before coding.
- [ ] Read it read-only (no SQLite dependency until verification); distinguish Agent `product.db` from game-root `.product.db`.
- [ ] Read `.build.info` product code/version.
- [ ] Missing folder → `Missing`.

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
- [ ] SQLite is only added if the schema is verified; otherwise no dependency is introduced.
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
