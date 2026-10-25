# Task T116B: GOG + EA + Ubisoft Matrix Rows

**Tier:** 1 — Documentation
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** merged
**Prerequisites:** Task T116B depends on Task T116

> Merged into T116 — the parent task covers this section in one document. Kept as reference.

> Documentation-only. Do not run build/test.

## Objective

Fill the GOG, EA, and Ubisoft sections with verified facts, and correct the "fully implemented" /
"signal only" contradictions.

## What Needs to Change

### 1. GOG
- [ ] Launcher locator: none; document `GalaxyClient` paths and per-game registry keys.
- [ ] Inventory: registry `GOG.com\Games` is used only to classify folders, not as inventory; Galaxy SQLite unverified.
- [ ] Metadata: `.info` name/id/exe/args; registry metadata ignored; multiple `.info` not merged.
- [ ] Exe/health: heuristic first, `.info` fallback; no health states.
- [ ] Correct the "GOG (Fully Implemented)" claim.

### 2. EA
- [ ] Launcher locator: none; document EA Desktop/EA Core keys.
- [ ] Inventory: no reader; modern `IS` encrypted; legacy Origin `.mfst` unparsed; registry used only to classify.
- [ ] Metadata: `InstallLog.txt` name/studio; `installerdata.xml` unparsed; no content ID.
- [ ] Exe/health: heuristic; no health states.
- [ ] Keep the EA caveat ("blocked on a complete install").

### 3. Ubisoft
- [ ] Launcher locator: none; per-game `Installs\{gameId}\InstallDir` is read but the ID is discarded.
- [ ] Inventory: not enumerated as inventory; `uplay_install.state`/manifest not parsed.
- [ ] Metadata: `Support/Readme` title only; publisher parsed then discarded.
- [ ] Exe/health: heuristic; no health states.

## Context

- Matrix home: `docs/detection/10-store-capabilities.md`.
- Evidence: `GogInfoParser.cs`, `EaInstallLogParser.cs`, `UbisoftReadmeParser.cs`, `RegistryFallbackDetector.cs`, `FolderScanner.cs`.
- `docs/research/gog_format.md`, `ea_format.md`, `ubisoft_format.md`.

## Requirements

- [ ] Each shipped claim cites `file:line` or a research doc.
- [ ] Unverified/encrypted formats are marked.
- [ ] No task IDs in the matrix.

## Verification

- [ ] `grep -n "GOG\|EA\|Ubisoft" docs/detection/10-store-capabilities.md` shows all three sections.
- [ ] Spot-check citations against the named files.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
