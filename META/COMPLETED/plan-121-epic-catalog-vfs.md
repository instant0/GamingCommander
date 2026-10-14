# Plan 121 — Epic catalog VFS (complete)

**Date:** 2026-08  
**Plan:** `planning/121-epic-manifest-vfs-investigation.md`

Shipped: the ProgramData manifests folder reads as one Epic catalog root (F2 **Add Epic Games Store**), producing base-game rows with Installed/Missing status. Orphan `.item` write via `EpicItemWriter`: AppName from `.manifest`, catalog ids from `.mancpn`/`.ovt` only — no AppVersionString, no `MainGame*`, no GraphQL ids. Matches the launcher-accepted Update file; one Epic Update per launcher run. Launch stays resolved-exe.

**C# files:** `EpicItemCatalog.cs`, `EpicItemClassifier.cs`, `EpicLibraryScanner.cs`, `EpicItemWriter.cs` (see `META/CODE_MAP.md`).

**Declined vs original spec:** Epic GraphQL in the app (Python tooling only), DLC/enrichment beyond Plan 109 scope.

**Contract / verification:** `docs/research/epic_item_format.md`; verify writer changes with `tools/decode_manifest.py`.

**Correction (2026-10-14, Plan 126):** "AppName from `.manifest`" is the Death-Stranding-specific rule;
the authoritative source is the `.mancpn` AppName (see `planning/126-facts-identity.md`). Also, the
orphan `.item` write is **identification-only** — it cannot register an app absent from the EOSH
reported set (server/account-driven). Tracked in `META/TASKS/phase-j-followups/T78-*.md`.
