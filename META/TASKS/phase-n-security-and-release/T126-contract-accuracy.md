# Task T126: Reconcile the Data Contract

**Tier:** 1 — Documentation
**Phase:** N — Security and Release Integrity
**Risk:** Low
**Status:** completed (2026-10-31)
**Prerequisites:** None (the Epic lookup disposition was merged into this task)

> Documentation-only. Do not run build/test. Do not change code in this task.

## Objective

Make the live documents describe exactly what the shipped application reads, writes, and contacts —
no more and no less — before the launcher-discovery and inventory work adds new readers. This includes
closing the Epic store-lookup question: the client is kept as a deferred **identity lookup**, not described
as removed, metadata enrichment, or `.item` generation.

## Merged: Epic store lookup disposition (2026-10-31)

The decision that was going to be a separate document lives here and in the live docs:

- **Keep** `EpicStoreSearch`. It is compiled and has no callers; it is not dead code.
- **One purpose, two uses:** (A) identify a detected Epic game whose local `.item`/`.mancpn` is missing or
  incomplete (orphan, moved, renamed), and (B) resolve the canonical title before the PCGW/Steam query.
  A and B are one request family: at most one search per game per lookup run.
- **Not a metadata source:** local Epic manifests supply display name, install location, launch exe,
  namespace, catalog id, app name and flags; PCGW and the Steam lookup supply the rest.
- **Not an `.item` generator and not a registration fix.** Regeneration is deferred; an identification-only
  `.item` does not register an app EOS does not report. That failure is registration, not reading/lookup.
- **Gate:** the existing online-metadata switch, no second toggle; never from scan, launch, sync, or health;
  timeout, no retry storm, no account or API key; show the hit and never overwrite local ids.
- **Docs must stop contradicting each other:** `ONLINE-AND-DATA` says "not called in v1", the blueprint
  says both "retained" and "removed", and `CODE_MAP` has no entry.

## What Needs to Change

### 1. `docs/ONLINE-AND-DATA.md`
- [x] Add the Epic `.item` **modification** to §2 with its exact target folder, trigger, and user-visible
      entry point. State plainly that the app cannot create an `.item`; it updates one Epic already wrote,
      and refuses when it is absent or unreadable.
- [x] Add the Steam ACF **creation** rule: only when the target is absent, only in an existing Steam library.
- [x] Add `data/repair-journal.jsonl` and `data/backups/<utc-stamp>/` to the app-owned write list, and state
      what deleting `data/` removes.
- [x] Reconcile the "never written" and "never contacted" lists with the code.
- [x] State that neither repair path creates a store or ProgramData directory; a missing target is a
      refusal, not a creation.
- [x] Keep one online section: local Epic manifests are the Epic metadata source; the Epic store lookup is
      **not called in v1** and, when wired, is identity-only. Do not list it as a metadata provider.

### 2. `GamingCommander.Readme.txt`
- [x] Bring ACCESS 1–12 into line with the writes the app can perform, including the Steam library and
      Epic ProgramData paths.
- [x] Correct ACCESS 10: it currently says `data/` is "the ONLY folder GamingCommander writes to". Name the
      two repair writes and the journal/backups, or the claim is false.
- [x] Keep the existing plain-language register; do not turn it into a security document.

### 3. `docs/FAQ.md`
- [x] Add or correct the entry that explains catalogue loss and the backup behaviour once the shared
      JSON write path exists.

### 4. `docs/detection/10-store-capabilities.md`
- [x] Confirm the `.item` modification description matches §1 of this task.
- [x] State that Epic store lookup is **identity only** (resolve title/namespace/catalog id for a game local
      files cannot identify; resolve the title before PCGW). Do not call it metadata enrichment and do not
      describe it as `.item` generation.
- [x] State that local `.item`/`.mancpn` manifests are the Epic metadata source, with PCGW and the Steam
      lookup filling the remaining fields.
- [x] State that the store GraphQL endpoint is **not called in v1** and is reserved for that identity
      lookup. Do not describe it as removed or as "never contacted".

### 5. `docs/detection/09-blueprint.md` and `META/CODE_MAP.md`
- [x] Blueprint: reconcile both places. Keep the "unused but retained" note; remove `EpicStoreSearch` from
      the §11 removed list; describe it as a deferred identity lookup.
- [x] Blueprint: mark the old O8 path-normaliser row closed — `PathText` is the single rule and the old
      `NormalizePath` copies are gone.
- [x] `META/CODE_MAP.md`: add an `EpicStoreSearch` row marked deferred, naming the identity purpose and the
      two uses. Do not add a planning document for it.

## Context

- `docs/README.md` names `ONLINE-AND-DATA.md` as the live contract for "ACF/`.item` writes".
- `src/GamingCommander.App/Services/EpicItemWriter.cs` and `SteamAcfWriter.cs` are the only writers
  outside `data/`; both now go through `RepairWriteGuard` (Steam create-only, Epic modify-only).
- `src/GamingCommander.App/Services/EpicStoreSearch.cs` is compiled but unreferenced. It is a deferred
  identity lookup, not dead code; do not remove it here.
- `src/GamingCommander.App/Services/JsonDocumentStore.cs` and `AppPaths.cs` are the JSON write path and the
  directory guard.
- ADR-005 defines the safety expectations for writes of this class.

## Requirements

- [x] Every write path in the code appears in the contract with its target and trigger.
- [x] Every network endpoint in the code appears in the contract, or is marked as deferred / not called
      in v1.
- [x] The Readme access inventory and the contract agree.
- [x] The Epic lookup is recorded as identity-only with two uses; `.item` regeneration and registration are
      explicitly out of scope.
- [x] The blueprint keeps the client and stops calling it removed; `CODE_MAP` has a deferred row.
- [x] No new "never" claim is introduced without a code reference to support it.
- [x] No task IDs appear in the documents.

## Verification

- [x] `grep` for `HttpClient`, `File.Write`, `Directory.CreateDirectory`, `Process.Start` and compare the
      results with §2 of the contract.
- [x] `grep` for `.item`, `ACF`, `graphql`, and `never` across the live docs; no contradiction remains.
- [x] `grep -rn "EpicStoreSearch" docs META/CODE_MAP.md` describes it consistently as deferred/identity,
      never as removed and never as a metadata source.
- [x] The GraphQL endpoint is described as deferred, not absent.
- [x] `docs/README.md` live table still points at the correct files.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Reconciled the live contract with the shipped code. `ONLINE-AND-DATA`: Epic storefront
  is present but not called in v1 (identity-only, two uses); write section now covers `libraries.json`,
  `.bak`/`.corrupt`, `repair-journal.jsonl`, `staging/`, `backups/`, Steam ACF create-only and Epic `.item`
  modify-only; the "only data/" and "never written" claims are corrected; JSON recovery is documented.
  `GamingCommander.Readme.txt`: ACCESS 10 corrected, ACCESS 10b added for the two repair writes, generated
  files list expanded, network section no longer claims Epic is contacted or absent in a misleading way.
  `docs/FAQ.md`: corruption recovery replaces "silent reset", reset/uninstall/troubleshooting updated.
  `docs/detection/10-store-capabilities.md`: Epic path-exact health, modify-only repair, lookup identity,
  class-name note. Blueprint: `EpicStoreSearch` retained, O8 closed via `PathText`, store priority aligned.
  `META/CODE_MAP.md`: `EpicStoreSearch` deferred identity row. T124D closed and merged here.
- **Verification:** `grep` for "ONLY folder", "silently resets", "Epic GraphQL is not used", "fuzzy path",
  "writes an identification" returns no live-doc contradiction. `EpicStoreSearch` appears in the blueprint,
  capability matrix, and `CODE_MAP` consistently as deferred/identity, never as removed or metadata.
  Docs-only: no build/test.
- **Issues encountered:** `collections.json` is listed as app-owned but the collections service is not wired,
  so it is marked "not written today". `LauncherInstalled.dat` remains documented-but-unread. Research docs
  still describe the old GraphQL plan; they are historical and not the live contract.
