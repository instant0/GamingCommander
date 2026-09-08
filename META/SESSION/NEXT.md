## Status

**Plan `125-stabilization-refactor.md` — COMPLETE (2026-09-09).**

All phases + final stage delivered, every success criterion met:
- Phase 0 identity contract, 1a composition root, 1b projection dedup, 1c MainWindow partials (base 1,022→113 L), Phase 2 (enrichers 526→352; ContainerScanner `SignalSummary`; 2a scoring partial 749→533; 2b `PlatformMetadataKeys`; 2c `CheapExeFinder`; 2d `GameSourceParser`), final stage ShellViewModel domain split (1,023→178 L + 9 partials), App warning cleanup (8→3, only pre-existing AVLN3001), and the user-approved constants round: `TitleSourceValues` (8), `SteamAcfFields` (10), `EpicItemSchema` (9) + 6 Core pin tests. Phase 3 `TestGameFactory` DROPPED (plan gate → audit found only 3 single constructions; P1b `CreateViewModel` fixture already shared).
- Build: full solution **0 errors** (UI 0 warnings; App 3 × AVLN3001 designer notices — accepted). `dotnet test` **App 398 + Core 165 + Migration 1 = 564 pass / 0 fail**.
- Deviations (recorded in plan): 2a partial-file naming (`ExecutableDiscovery.Scoring.cs`); ShellViewModel criterion resolved by domain cohesion per user direction; FolderScanner 352 vs "~300" (within <400).

## Next: Plan 123 — Detection bugfixes (PLANNED in planning/README)

`planning/123-detection-bugfixes.md`: detection tightening — confidence tiers (Locked/Secure), Steam locked, Epic manifest Secure, Missing-Manifest fix, safe-first. Not started; read the plan before implementing.

Standing rules unchanged: one file at a time; new files before removal; behavior-preserving (constants values pinned by contract tests — never rename persisted strings); Windows paths opaque; full build + suite after each step.