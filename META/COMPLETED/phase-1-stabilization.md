# Phase 1.1a: Setup & GUI Stabilization — COMPLETED

**Date:** 2025-Q4
**Status:** Complete

## Deliverables
- Folder scanner excludes non-game folders
- Primary exe detection heuristic with name-matching bonus
- User-configurable ignore list (HiddenFolders in settings.json)
- Game entries are terminal (non-browsable) — selecting shows details
- ".." parent entry rendered at top of every game list
- Backspace goes up one level
- Arrow keys work after every navigation (Focus restored)
- SelectedIndex preserved across navigation
- ScrollIntoView works after data reload
- Command buttons clickable and wired to actions (F2, F3, F5, F8, F9, F10)
- Double-click drills into folders
- Mouse selection populates details panel
- F10 quits the app
- S shows search placeholder
- Mock data: data/mock/ directory tree with Steam, Epic, standalone, anti-cheat scenarios
- Mock registry .reg files for 5 launchers
- Python validation tools (ACF parsing, registry parsing, Epic manifest decoding)
- 17 tests (up from 3): model tests, scanner filter tests (6), integration tests (5)
- GamingCommander.App.Tests project created

## Key Decisions
- Executable detection pipeline with scoring system (ADR-009)
- Python research tools kept as dev-only (never disclosed to agents)
