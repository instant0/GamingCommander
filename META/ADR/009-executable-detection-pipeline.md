# ADR-009: Executable Detection Pipeline

## Status
Accepted

## Date
2025-03-15

## Context
The folder scanner needs to identify the primary game executable from potentially many executables in a game folder. Anti-cheat installers, launchers, and redistributables should not be selected as the primary game executable.

## Decision
Use a multi-stage scoring pipeline:
1. Enumerate all executable candidates in the folder.
2. Apply exclusion scoring — filter out known non-game executables (anti-cheat installers, setup tools, launchers, redistributables) using a pattern list.
3. Apply positive scoring — executables whose filename partially matches the parent folder name receive a bonus.
4. Rank candidates by score.
5. Return the highest-scoring executable.

## Consequences
- Folder-name-matching executables are preferred over larger but irrelevant exes.
- Non-game exe exclusion list (~25 patterns) must be maintained.
- Size-based tiebreaker only applies when no name match exists.
- Detection quality depends on pattern list completeness.
