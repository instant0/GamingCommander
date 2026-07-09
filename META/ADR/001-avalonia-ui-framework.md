# ADR-001: Avalonia UI Framework

## Status
Accepted

## Date
2025-01-15

## Context
The project needed a cross-platform UI framework for a Windows-native application. Options considered: Terminal.Gui, Consolonia, and Avalonia. Requirements included a retro Norton Commander look, adaptive resizing, Windows usability, maturity, packaging practicality, and keyboard-first UX.

## Decision
Use Avalonia 11.x as the UI framework. Terminal.Gui as fallback.

## Consequences
- Cross-platform development possible on Linux while targetting Windows.
- Requires Avalonia-specific knowledge for UI implementation.
- Windows-native feel achievable with platform-specific styling.
- Larger dependency than pure terminal UI.
- Chosen over Terminal.Gui due to better resizing and modern window support.

## Alternatives Considered
- Terminal.Gui: Better terminal-native feel, but limited resizing and modern window support.
- Consolonia: Terminal-hosted Avalonia, immature for production use.
