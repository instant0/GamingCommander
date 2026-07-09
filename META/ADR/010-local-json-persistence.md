# ADR-010: Local JSON Persistence

## Status
Accepted

## Date
2025-03-01

## Context
The application needs persistent storage for configuration, game data, metadata, and migration logs. Requirements include human-readability, simplicity, and no external database dependencies.

## Decision
Use local JSON files for all persistent data:
- `data/settings.json` — application configuration (library roots, hidden folders)
- `data/games.json` — scanned game database
- `data/games_db.json` — enriched metadata cache (Phase 2.2)
- `data/backups/` — migration manifest backups
- `data/migration_log.jsonl` — migration operation log

## Consequences
- No external database dependency.
- Files are human-readable and debuggable.
- Suitable for hundreds of games (not thousands).
- No concurrent write safety (single-user app).
- Easy to backup and restore.
