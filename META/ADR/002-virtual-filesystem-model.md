# ADR-002: Virtual File System Model

## Status
Accepted

## Date
2025-02-10

## Context
The initial design assumed real filesystem browsing for game navigation. This proved problematic because real filesystem browsing shows irrelevant folders (logs, saves, DLCs, etc.), games must be identified by parsing folders rather than just listing them, and raw filesystem navigation provides a poor user experience.

## Decision
Navigation uses a virtual filesystem stored in `data/games.json`. The real filesystem is only touched during Setup/Rescan operations.

## Consequences
- Games are the primary navigation unit, not folders.
- A scanning step is required when adding library roots.
- `data/games.json` becomes the source of truth for navigation.
- Enables metadata enrichment per-game.
- Eliminates slow filesystem enumeration during navigation.

## Implementation
- Top level (F9): Lists configured library roots as "drives"
- Inside a root: Lists games parsed from `data/games.json`
- No real filesystem browsing during navigation
- Scanner detects game type via marker files and heuristics
