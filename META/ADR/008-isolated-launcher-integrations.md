# ADR-008: Isolated Launcher Integrations

## Status
Accepted

## Date
2025-01-15

## Context
The application must support multiple game launchers (Steam, GOG, Epic, EA App, Ubisoft Connect) without core modifications.

## Decision
Launcher-specific logic is isolated behind interfaces (`ILauncher`). Each launcher has its own implementation class. The core application depends on the interface, not on specific launcher implementations.

## Consequences
- New launchers can be added without modifying existing code.
- Launcher implementations can be tested independently.
- Core application remains stable as launcher integrations evolve.
- Requires careful interface design to accommodate all launcher types.

## Superseded (2026-Q2)

The `ILauncher` interface was retired in plan 95 (bugfix-and-cleanup). The practical two-tier scanner architecture (`LibraryManager` → `FolderScanner`/`SteamLibraryScanner`) replaced it while preserving the original isolation intent. Launcher detection logic remains decoupled from core logic through the `ILibraryManager` abstraction.
