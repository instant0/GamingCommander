# Plan 125 — Stabilization Refactor (complete)

**Date:** 2026-09-09
**Plan:** `planning/125-stabilization-refactor.md`
**Predecessor:** Plan 124 (complete).

Shipped: identity contract (Phase 0), composition root (1a), projection dedup (1b), `MainWindow`
partials (1c), enricher/container/scoring splits (Phase 2), `PlatformMetadataKeys`, `CheapExeFinder`,
`GameSourceParser`, `ShellViewModel` domain split (base 178 L + 9 partials), App warning cleanup, and
the constants round (`TitleSourceValues`, `SteamAcfFields`, `EpicItemSchema`) with Core pin tests.
Phase 3 `TestGameFactory` **dropped** (plan gate — only 3 single constructions; shared fixture existed).

**Verification:** full solution build 0 errors (UI 0 warnings; App 3 × pre-existing AVLN3001);
`dotnet test` **564 pass / 0 fail** (App 398 + Core 165 + Migration 1). Note: Plan 123's C# detection
port (549) predates this; the 564 figure supersedes it.

**Deviations recorded in-plan:** `ExecutableDiscovery.Scoring.cs` partial naming; ShellViewModel
criterion resolved by domain cohesion; FolderScanner 352 L (< 400).
