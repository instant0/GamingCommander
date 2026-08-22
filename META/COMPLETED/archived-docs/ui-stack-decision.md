# UI Stack Decision

## Phase 0 Decision

- **Primary UI stack:** Avalonia
- **Fallback UI stack:** Terminal.Gui
- **Deferred for current workflow:** WinUI 3 / WPF

## Why Avalonia

Avalonia is the best fit for the current combination of constraints:

- Linux-based implementation environment with .NET 8 available
- later manual validation on Windows
- a Windows-first product goal
- a keyboard-first, commander-style interface that must also support mouse interaction
- a resizable layout that should adapt to modern window sizes instead of emulating a fixed DOS grid

## Why Not Make Terminal.Gui Primary

Terminal.Gui is a credible fallback because it matches the retro text-mode aesthetic naturally, but it is less aligned with the long-term Windows-native desktop feel and packaging goals of the project.

## Why WinUI 3 / WPF Are Deferred

Both are strong Windows-native choices, but they are a poor match for the current Linux-first implementation workflow. They remain valid future comparison points if Avalonia becomes blocked.

## Phase 0 Implementation Consequences

- Build the first app shell in Avalonia.
- Keep core models and service boundaries UI-agnostic.
- Make resize behavior and mouse interaction first-class proof-of-concept requirements.
- Treat Windows packaging and runtime validation as a manual downstream task.
