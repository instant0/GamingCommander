# ADR-012: Platform-Suffixed Binaries Outrank Exact-Name Matches

## Status
Accepted

## Date
2026-08-23

## Context
Executable scoring gave a large +40 bonus only when the exe stem's letters+digits key exactly equaled the folder name (`MyGame.exe` in `MyGame\`). A platform-suffixed binary such as `MyGame-Win64.exe` or `Binaries\Win64\MyGame-Win64-Shipping.exe` failed that equality ("mygamewin64" ≠ "mygame"), so a root exact-name exe could tie or beat the real game payload. In practice a root `FolderName.exe` is frequently a launcher stub, while platform-suffixed binaries are almost always the actual game executable. Discovered via failing test `ScoreExecutable_Win64Binary_AddsBonus` (test/code drift from commit a870a45).

## Decision
In `ExecutableDiscovery.ScoreExecutable`, when strict equality fails, award the same +40 exact-match bonus if the name key equals the folder key after stripping known platform tokens (`win64`, `win32`, `wingdk`, `shipping`). Platform-suffixed binaries therefore outrank bare exact-name matches (suffix bonuses still apply on top). `TitleText.MatchesFolderAndExe` itself is unchanged — folder/exe equality remains an identity/search signal; this decision affects primary-executable selection scoring only.

## Consequences
- Deep-path shipping binaries now decisively beat root exact-name exes (previously a tie at 65 vs 65).
- Same-level `-Win64` binaries beat plain exact-name exes (70 vs 65).
- Risk of false positives is minimal: stripping only removes well-known platform tokens; launcher/updater names remain penalized by the existing pattern list.
- Scoring rules documented in META/CODE_MAP.md updated accordingly.
