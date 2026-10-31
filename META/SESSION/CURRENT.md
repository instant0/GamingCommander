# Current Project State

**Updated:** 2026-10-29

## State
- Dual-pane shell over typed JSON persistence (`libraries.json`, `games.json`, sidecar metadata, `collections.json`); schema versioning and identity reconciliation shipped.
- Standalone + Steam scan/launch shipped; folder/registry detection covers GOG, Epic, EA, Ubisoft, Battle.net, Xbox, Rockstar.
- Detection blueprint is the reference (`docs/detection/09-blueprint.md`); Steam vs SteamEmu classification is implemented (Steam-library location wins, ACF optional).
- Store launcher discovery + read-only inventory import is the next product track (`planning/129`); manifest-first/stat-only checks, per-store opt-in, launch-time sync, enrichment, and Setup controls.
- Epic `.item` re-creation is a dead end: registration is server/account-driven, so detection stays read-only. SyncMove migration is deferred.
- Local-only app; online extras remain opt-in PCGW + Steam Store. No full store clients; nothing has shipped, so persisted schemas carry no back-compat burden.

## Shared path rule (2026-10-29)

`PathText` (`Core/Services/PathText.cs`) is the single path rule: `TrimTrailingSeparators` for real paths, `NormalizeForCompare`/`Equals`/`IsUnder`/`Leaf` for comparison. The five duplicate normalisers and the Epic folder-name fallback are gone; stored paths keep their original casing and separators, comparison is `OrdinalIgnoreCase` on a derived form. Behaviour changes (all tested): `/` vs `\` now compare equal, `C:/Games/X` is under `C:\Games`, and two folders sharing a last name are no longer the same game. Recorded in `planning/130`; store identity, locator, reader, writer, policy gate and stat decisions stay open for their consumers.

## Security review outcome (docs-only, 2026-10-29)

Class-calibrated review of a local single-user launcher: no elevation, no services, no telemetry, no writes into game installs, no hidden network. Out of scope by decision: Authenticode, provenance/SLSA, reproducible builds, PSIRT/CVE, bounty, fuzzing, sandboxing, encryption at rest.

- **Split, not one review:** T124 (detection/store/identity duplicates), T124A (connection map), T124B (persistence write integrity), T124C (repair-write safety), T124D (Epic online client disposition). Each has its own plan slot: 130, 132, 133, 134.
- **Repair writers:** no `Directory.CreateDirectory` outside the application data root. Generate in `data/staging/`, read it back to prove it parses, copy to the target, length-check, delete the staging file. Nothing else is written to the store directory — no temp files, no renames.
- **Safe local-state writes, not tamper detection.** One sequence: copy to `data/games.json.bak` → write → read back and parse → accept, or restore the `.bak` and report. On read, a parse failure keeps the damaged bytes as `.corrupt`, restores the `.bak`, and says so; it never returns an empty catalogue. No hash, signature, or hash chain. Required before per-launch sync; a one-shot import may land first.
- **`EpicStoreSearch` stays, as an identity lookup only.** Local `.item`/`.mancpn` manifests supply the Epic metadata; PCGW and the Steam lookup fill the rest. The client resolves the canonical title, namespace, and catalog id for a game local files cannot identify (orphan/renamed/incomplete), and resolves the title before the PCGW/Steam query. It returns title/namespace/id only, is gated, and is never called from scan, launch, or sync. `.item` regeneration and registration are separate, deferred subjects. The earlier removal proposal is withdrawn.
- **`games.json` integrity checks dropped.** Same-user write access is not a boundary worth controlling.
- **Not carried:** CC BY-NC licence terms, version stamping/publish path, absolute paths in `startup.log`.

## Task review (2026-10-31)

Corrected beliefs across the pending task set:

- **Disk is truth for installation.** A launcher record identifies a game; `Installed` needs the folder. Only Steam and Epic have a usable inventory today; GOG/EA/Ubisoft/Battle.net/Rockstar are best-effort registry/marker detection, and absence of a record is not removal.
- **No package API and no SQLite.** Xbox stays marker-only and reports `Unsupported`; Battle.net Agent `product.db` is unread. Steam's VDF is a path locator (its `apps` blocks are not the installed set); Epic's `.item` is the primary inventory and `LauncherInstalled.dat` is at most a cross-check.
- **Epic lookup is identity only** (title/namespace/catalog id, plus resolving the title before PCGW). Local manifests are the Epic metadata source; `.item` regeneration and registration are separate, deferred subjects.
- **Wiring gaps are task work, not assumptions:** the policy provider is wired in the launcher-discovery first-run step; `GameStoreIndex` is built by import/sync; no name-only identity matching (same store + unique only).

## Active queue
See `META/SESSION/NEXT.md` for the canonical execution order. Task index: `META/TASKS/README.md`. Pending tasks are T124D, T126, T133, T117 (A/B), T118 (A/B/C), T119 (A/B), T121 (A/B/C), T122 (A/B), T125, T127, T128, plus the P2 UI track (T72→T73). Done on 2026-10-31: T130, T129, T132. The connection map is at `META/CONNECTION_MAP.md`; the I/O audit result is `planning/135-io-call-audit.md`.

## Blockers / deferred
- Steam-vs-SteamEmu is implemented; the `Extensions{}` / emulator model still needs a user decision.
- SyncMove manifest repair is deferred until detection is settled.
- EA format caveat is blocked on a complete EA install; .NET 8→9 upgrade is deferred indefinitely.

## Verification baseline
- Build: 0 errors / 0 warnings.
- Tests: 755 passing (Core 219, Migration 1, App 535).
- Detection matrix: 22/22.
