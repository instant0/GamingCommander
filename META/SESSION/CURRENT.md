# Current Project State

**Updated:** 2026-10-25

## State
- Dual-pane shell over typed JSON persistence (`libraries.json`, `games.json`, sidecar metadata, `collections.json`); schema versioning and identity reconciliation shipped.
- Standalone + Steam scan/launch shipped; folder/registry detection covers GOG, Epic, EA, Ubisoft, Battle.net, Xbox, Rockstar.
- Detection blueprint is the reference (`docs/detection/09-blueprint.md`); Steam vs SteamEmu classification is implemented (Steam-library location wins, ACF optional).
- Store launcher discovery + read-only inventory import is the next product track (`planning/129`); manifest-first/stat-only checks, per-store opt-in, launch-time sync, enrichment, and Setup controls.
- Epic `.item` re-creation is a dead end: registration is server/account-driven, so detection stays read-only. SyncMove migration is deferred.
- Local-only app; online extras remain opt-in PCGW + Steam Store. No full store clients; nothing has shipped, so persisted schemas carry no back-compat burden.

## Active queue
See `META/SESSION/NEXT.md` for the ordered work queue and current priority.

## Blockers / deferred
- Steam-vs-SteamEmu is implemented; the `Extensions{}` / emulator model still needs a user decision.
- SyncMove manifest repair is deferred until detection is settled.
- EA format caveat is blocked on a complete EA install; .NET 8→9 upgrade is deferred indefinitely.

## Verification baseline
- Build: 0 errors / 0 warnings.
- Tests: 686 passing (Core 200, Migration 1, App 485).
- Detection matrix: 22/22.
