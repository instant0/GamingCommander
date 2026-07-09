# Phase 1.2: Research & Data Collection — COMPLETED

**Date:** 2026-Q1
**Status:** Complete

## Deliverables
Research documents in docs/research/:
- steam_acf_schema.md — Steam ACF required fields for identification + migration
- steam_vdf_schema.md — libraryfolders.vdf structure for discovery
- steam_common_schema.md — common/ folder cross-reference approach
- standalone_schema.md — Three-tier classification for folder detection
- ea_format.md — EA App format documentation
- gog_format.md — GOG format documentation
- epic_item_format.md — Epic .item JSON format + GraphQL API
- ubisoft_format.md — Ubisoft format documentation
- pcgamingwiki_notes.md — PCGamingWiki research notes
- launcher_discovery.md — Launcher registry discovery notes

Python tools in tools/:
- parse_steam_acf.py, list_standalone_games.py, discover_steam_libraries.py
- list_steam_common.py, detect_folder.py, validate_steam_libraries.py
- decode_manifest.py, parse_manifest.py, epic_search.py
- setup_mock_data.py, generate_mock_registry.py, parse_registry.py

## Bugs Found (C# Implementation)
- See META/BACKLOG/TECH_DEBT.md for bugs discovered during research
- GOG: checks exact goggame.info name — real files are goggame-<id>.info (prefix)
- EA: checks eaapp_ prefix — real detection needs __Installer/ directory
- Ubisoft: checks folder name — real detection needs uplay_install.manifest
- Performance: Directory.GetFiles("*", AllDirectories) in DetectType() is recursive
