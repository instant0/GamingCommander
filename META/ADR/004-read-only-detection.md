# ADR-004: Read-Only Detection

## Status
Accepted

## Date
2025-01-20

## Context
The application must scan installed games to detect them. There is a risk of accidentally modifying user data during the detection process.

## Decision
Detection must be read-only. Scanning must not modify any game files, registry entries, or user data.

## Consequences
- All detection operations must be non-destructive.
- `FolderScanner` must not write to game directories.
- Detection results are stored separately in `data/games.json`.
- Migration operations use separate, explicitly-authorized code paths.
- Simplifies testing (no side effects from detection).
