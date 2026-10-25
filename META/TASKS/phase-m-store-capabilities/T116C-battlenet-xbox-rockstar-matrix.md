# Task T116C: Battle.net + Xbox + Rockstar Rows and Contradictions

**Tier:** 1 — Documentation
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** merged
**Prerequisites:** Task T116C depends on Task T116

> Merged into T116 — the parent task covers this section in one document. Kept as reference.

> Documentation-only. Do not run build/test.

## Objective

Fill the Battle.net, Xbox, and Rockstar sections, then reconcile the cross-document contradictions
that make the support matrix untrustworthy.

## What Needs to Change

### 1. Battle.net
- [ ] Launcher locator: none; document HKLM/HKCU `Battle.net\InstallPath`.
- [ ] Inventory: Agent `%ProgramData%\Battle.net\Agent\product.db` (SQLite, unverified) and `Battle.net.config`; app reads neither.
- [ ] Metadata: `.build.info` product code only; distinguish Agent `product.db` from game-root `.product.db`.
- [ ] Health/launch: none; raw exe.

### 2. Xbox
- [ ] Launcher/package locator: none; `Get-AppxPackage` documented, not implemented.
- [ ] Inventory: AppX package registration; `default-metadata.json` marker only.
- [ ] Metadata/exe/launch: no `appxmanifest.xml` exe, no `shell:appsFolder` launch.
- [ ] Health: none.

### 3. Rockstar
- [ ] Launcher locator: the `Rockstar Games\Launcher` key is skipped; `Settings.json` unread.
- [ ] Inventory: per-game registry used only to classify.
- [ ] Metadata: `title.rgl` not parsed; RAGE engine tag only.
- [ ] Health/launch: none.

### 4. Contradiction pass
- [ ] Reconcile the stale "signal only" statements for Epic/EA/Ubisoft with shipped parser code.
- [ ] Distinguish Epic `.item` **write** from **registration** everywhere.
- [ ] Fix Battle.net `.battle.net/` vs `.build.info`/`.product.db` marker claims.
- [ ] Fix Xbox "metadata detection" vs "signal-file only" wording.
- [ ] Fix the store-count drift and the omitted Xbox row in the roadmap phase list.
- [ ] Note the Steam registry-key and GOG default-path disagreements.

## Context

- Matrix home: `docs/detection/10-store-capabilities.md`.
- Evidence: `StoreSignalDetector.cs`, `RegistryFallbackDetector.cs`, `EngineDetector.cs`, `GameLaunchResolver.cs`.
- Research: `blizzard_format.md`, `xbox_format.md`, `rockstar_format.md`, `launcher_discovery.md`.

## Requirements

- [ ] All three store sections complete with evidence.
- [ ] Each contradiction is either fixed or explicitly marked "needs verification".
- [ ] No task IDs in the matrix.

## Verification

- [ ] `grep -n "Battle.net\|Xbox\|Rockstar" docs/detection/10-store-capabilities.md` shows all three.
- [ ] `grep -rn "signal only\|fully implemented" docs/GAME-DETECTION-LOGIC.md` is reconciled or marked stale.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
