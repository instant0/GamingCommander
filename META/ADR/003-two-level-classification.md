# ADR-003: Two-Level Game Classification

## Status
Accepted

## Date
2025-03-01

## Context
Games can live outside their native launcher folders (e.g. Steam games copied manually, or Epic games with `.egsstore` inside a non-Epic folder). A marker file found outside a known launcher folder is not a reliable source identifier. The user's intent is the ground truth.

## Decision
Use a two-level classification model:
1. **Root level:** Each library root has a `defaultType`. All games inherit this type unless overridden.
2. **Game override:** Individual games can be tagged with a different type. Override takes precedence over root default.

## Consequences
- Users must explicitly classify library roots during setup.
- Game detection heuristics are secondary to user intent.
- Configuration is persisted in `data/settings.json` with typed library roots.
- Individual game overrides stored in `data/games.json` with `override: true`.
- Enables accurate source identification even for non-standard installations.
