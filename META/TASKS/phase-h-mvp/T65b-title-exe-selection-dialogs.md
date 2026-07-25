# Task T65b: Title & Exe Selection Dialogs

**Tier:** 3 — UI/Behavior
**Phase:** H — MVP (post-detection parity)
**Effort:** ~60 min
**Risk:** Medium
**Status:** Pending
**Prerequisites:** T65 (GOG .info parser provides metadata for comparison)
**WP:** WP-4 (UX polish)

---

## Objective

When the scanner detects a game with multiple exe candidates or when metadata sources disagree on the title, the user should be able to pick the correct exe or title. Currently, the scan picks the best candidate silently and there's no way to override without manually editing paths. These dialogs surface the scan's confidence and let the user correct it.

## Two Dialogs

### Dialog A: Exe Selection (first launch or F4 edit)

**Trigger:** User presses Enter to launch a game that has multiple exe candidates stored in metadata, OR user presses F4 to edit a game with multiple candidates.

**Behavior:**
- Show a modal dialog listing all detected exe candidates
- Each entry shows: exe name, path (truncated), and source (e.g., "GOG .info", "Root scan", "UE Binary")
- Pre-select the one the scanner picked as primary
- User picks one → stored as the game's `ExecutablePath`, flagged as `"UserSupplied"` in `PlatformMetadata["ExeSource"]`
- User cancels → launch proceeds with the scanner's pick (or edit is cancelled)

**Data model:**
- `GameEntry.PlatformMetadata["CandidateExes"]` = JSON array of `{ path, source }` objects
- Populated during scan when `ExecutableDiscovery` finds multiple exes OR when GOG .info provides an additional candidate
- Cleared after user selects (or never populated if only one candidate)

### Dialog B: Title Selection (F4 edit)

**Trigger:** User presses F4 to edit a game where `PlatformMetadata["AutoDetectedTitle"]` differs from `DisplayName` (i.e., the scanner used a source-derived title but the folder name suggests something different).

**Behavior:**
- In the existing GameSetupWindow (F4), add a "Title" section showing:
  - Current `DisplayName` (with `TitleSource` badge: "GOG", "Steam", "Auto-detected", "User")
  - `AutoDetectedTitle` from folder name (if different)
  - Source-specific title (if different)
- User can pick one or type a custom title
- Stored as `DisplayName` with `TitleSource = "UserSupplied"`

**Integration:** Extend `GameSetupWindow` rather than creating a new dialog. The existing window already handles `DisplayName` editing — add the source comparison UI.

## What Needs to Change

### 1. `GameEntry` model — candidate storage

No model change needed — `PlatformMetadata` (Dictionary<string, string>) already supports arbitrary keys:
- `CandidateExes`: JSON array string (e.g., `[{"path":"...","source":"GOG .info"},{"path":"...","source":"Root scan"}]`)
- `TitleSource`: string tracking title origin
- `AutoDetectedTitle`: folder-name-derived title (when source title differs)

### 2. `FolderScanner.AddGameEntry()` — populate candidates

When multiple exes are found:
- Store all candidates in `PlatformMetadata["CandidateExes"]`
- Mark the selected one in the metadata (e.g., `"SelectedExeIndex": "0"`)

### 3. New dialog: `ExeSelectionWindow.axaml`

- Simple modal with a ListBox of exe candidates
- Returns the selected path
- Triggered from `MainWindow` on Enter (when candidates exist) or from `GameSetupWindow` on F4

### 4. `GameSetupWindow` — title comparison UI

- Add a row showing the source-derived title when it differs from the current DisplayName
- Allow user to pick or type custom title
- Store `TitleSource` in PlatformMetadata

## Context

- `GameSetupWindow.axaml.cs` already edits `DisplayName`, `ExecutablePath`, `CommandLineArguments` — title comparison and exe candidate selection extend this naturally
- `PlatformMetadata` is the right place for candidate storage (no model changes needed)
- The scan phase should never block on user input — it picks the best candidate and moves on; the dialog is on-demand
- This task is post-MVP detection parity (WP-4, P1) — the scan works without it, this is UX polish

## Requirements

- [ ] Exe candidates stored in `PlatformMetadata["CandidateExes"]` during scan
- [ ] `ExeSelectionWindow` modal shows candidates with name, path, source
- [ ] User selection overrides `ExecutablePath` and sets `ExeSource = "UserSupplied"`
- [ ] `GameSetupWindow` shows title source comparison when `AutoDetectedTitle` differs
- [ ] User can pick source title or enter custom title
- [ ] `TitleSource` stored in `PlatformMetadata`
- [ ] Cancel/Escape preserves existing values
- [ ] Build clean, existing tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Manual: scan a folder with multiple exes → Enter shows picker
- [ ] Manual: F4 on GOG game → title section shows GOG title vs folder name
- [ ] Manual: pick custom title → stored as UserSupplied

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
