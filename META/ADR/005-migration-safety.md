# ADR-005: Migration Safety

## Status
Accepted

## Date
2025-01-20

## Context
Game migration involves moving large amounts of data and modifying launcher manifests. Mistakes can result in lost games or broken launcher configurations.

## Decision
Migration is treated as a safety-critical operation with the following requirements:
- Validate destination has enough disk space.
- Validate destination does not already exist.
- Validate source is accessible.
- Provide clear operation modes (Move+Symlink, Move Only, Dry Run).
- Preserve recovery information (backup manifests, migration log).
- Avoid destructive actions without explicit user confirmation.

## Consequences
- Migration has separate code paths from detection.
- Backup manifests stored in `data/backups/` before modification.
- Migration log written to `data/migration_log.jsonl`.
- Operations are reversible (documented reversal steps).
- Dry run mode allows preview without mutation.

### 2026-07-09 Clarification
The original ADR assumed GamingCommander would move game files. In practice, the app
does **not** move game files — that is the user's responsibility via OS tools. The app
only repairs launcher registration (manifests, ACF files, registry) after detecting that
a user-initiated file relocation has occurred.

The `Move+Symlink` and `Move Only` modes are replaced by a single `Fix Registration` mode:
- **Fix Registration** — update manifest/registry to match the detected file location
- **Dry Run** — preview changes without writing

The safety requirements (backup, logging, reversibility, dry run) remain the same.
