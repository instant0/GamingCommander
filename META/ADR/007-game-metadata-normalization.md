# ADR-007: Game Metadata Normalization

## Status
Accepted

## Date
2025-02-10

## Context
Steam, GOG, Epic, and standalone games expose different data formats for metadata. The core application should not depend on any specific launcher's data model.

## Decision
Game metadata is represented as normalized domain objects (`GameEntry`, `GameRecord`, `GameMetadata`) rather than launcher-specific structures. Each launcher integration maps its native format to the common domain model.

## Consequences
- Core application is launcher-agnostic.
- New launcher integrations only need a format mapper.
- Metadata enrichment (Phase 2.2) operates on normalized fields.
- Loss of launcher-specific detail that doesn't map to the common model.
