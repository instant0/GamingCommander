# Plan 122 — Type-to-Search (Silent Keyboard Capture)

**Status:** COMPLETE — 2026-08-23. ShellViewModel buffer + live header, MainWindow key routing (S/T freed), help dialog, 7 tests. Live Windows smoke test pending user.
**Audience:** Builder
**Depends on:** Shipped F8/S filter infrastructure (`GameFilterMatcher`, `ShellViewModel.ApplyFilter`) — no data-model changes.

---

## 1. Problem Statement

The only way to find a game today is F8 (or `S`), which opens a modal filter dialog. Users think in titles: they want to just start typing "Assa" and see Assassin's Creed entries appear from every library root without touching the mouse or a dialog.

Because most plain letters are unbound, the keyboard can capture typing silently. After the 3rd typed character the app begins filtering the entire VFS (all library roots) by **name and tags**, live per keystroke.

---

## 2. Behavior Specification

### Capture rules

| Input | Action |
|-------|--------|
| Printable char (letters, digits, Space, `-`, `'`, `.`) | Append to search buffer. At buffer length ≥ 3, apply live wildcard filter. Only when no Ctrl/Alt/Meta modifier (Shift allowed). |
| Backspace | Remove last char. Still ≥ 3 → re-filter. Drops below 3 → clear filter, return to library roots, buffer empty. |
| Esc | If buffer non-empty or filter active → cancel search, clear filter, return to roots. Otherwise existing NavigateUp behavior. |
| Up/Down/Enter/F-keys | Normal behavior. Buffer is retained, so typing continues refining after navigation. |

### Matching

Reuse `GameFilterKind.Wildcard` exactly as shipped: case-insensitive substring against `DisplayName`, `FolderName`, store label, and tags (game `Tags` + sidecar genre/engine extras). Substring, not prefix — "assa" matches "Assassin's Creed III" and "Assault".

### Scope

Cross-root, like the existing filter: results come from every configured library root regardless of where the user was browsing. Works from the roots view, inside a root, or while an F8 filter is active (typing replaces it once threshold is reached).

### Feedback

- **Live query in the left pane header** while typing (this is the primary indicator):
  - Buffer non-empty → header shows the typed text, e.g. ``Search: 'Assa'`` — updates on every keystroke, including below threshold.
  - While a search buffer exists it takes precedence over the `Filter: …` header (the user sees exactly what they typed); F8-chosen filters keep the existing `Filter: {caption}` header.
  - Header reverts to `Library Roots` / root path / `Filter: …` when the buffer empties.
- Status bar supplements: below threshold `Keep typing…`; at/above threshold match count (existing `ApplyFilter` status).
- The filtered list keeps its `".."  Clear filter` first row; selecting it clears the search.
- Selection resets to top on each re-filter (list rebuilds).
- `InteractionHint` gains a type-to-search hint.

---

## 3. Required Binding Changes

The premise "no ASCII letters are bound" is not quite true. Two conflicts must go:

| Key | Today | Change |
|-----|-------|--------|
| `S` | Opens F8 filter dialog | Removed. F8 remains the dialog entry point. |
| `T` | Legacy retag/game-setup shortcut | Removed. F4 remains the primary key. |

This alters help-dialog text that currently documents `F8 / S`. F8 functionality itself is unchanged.

---

## 4. Files Affected

| File | Change |
|------|--------|
| `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` | Add `_searchBuffer` + `AppendSearchChar(char)`, `SearchBackspace()`, `CancelSearch()`. Threshold logic applies `new GameFilter(GameFilterKind.Wildcard, buffer)` via existing `ApplyFilter`; below-threshold status text. Update `InteractionHint`. |
| `src/GamingCommander.App/MainWindow.axaml.cs` | `OnKeyDown`: route printable keys to `AppendSearchChar`; `Back`/`Esc` precedence per table above; delete `case Key.S` and `case Key.T`. |
| `src/GamingCommander.App/Services/HelpDialogBuilder.cs` | Replace `F8 / S` row; add type-to-search row. |
| `tests/GamingCommander.App.Tests/` | New `ShellViewModelSearchTests`: threshold gating, live refine, backspace-below-threshold clears, Esc cancels, wildcard matches name + tag. |

No model, persistence, scanner, or DI changes.

---

## 5. Testing Strategy

Deterministic unit tests (no filesystem beyond test fixtures the VM already supports):

1. Two typed chars → no filter applied, no view change.
2. Third char → `ActiveFilter` is Wildcard with the full buffer; items are cross-root matches.
3. Typing refines: buffer grows, filter value tracks buffer.
4. Backspace within threshold range re-filters; below threshold clears filter and empties buffer.
5. Esc with active search clears filter; Esc with no search still navigates up.
6. Match correctness: title substring ("Assa"), tag substring, folder-name substring; case-insensitive.

Manual smoke: type `assa` with multiple libraries configured → Assassin's-style titles from several roots; Enter launches selected result.

---

## 6. Success Criteria

- [ ] Typing anywhere in the shell is silent until the 3rd printable char.
- [ ] At 3+ chars the left pane shows live cross-root matches on name OR tags, updating per keystroke.
- [ ] Scenario verified: typing "Assa" surfaces partially-matching titles (e.g., Assassin's Creed entries) across libraries.
- [ ] Backspace erases one char at a time; dropping below 3 chars restores the library-roots view.
- [ ] Esc instantly cancels an active search.
- [ ] Bare `S` and `T` no longer trigger dialogs; F8 and F4 unchanged.
- [ ] Launching (Enter / double-tap) works from search results.
- [ ] Help dialog reflects the new keys.
- [ ] Build clean, all tests pass.

---

## 7. Out of Scope

- Restoring the exact pre-search view on cancel (v1 returns to roots, same as today's ClearFilter).
- Fuzzy/folded matching, non-Latin layouts/IME.
- Debounce or result caching (in-memory iteration over hundreds of games per keystroke is trivial).
- A visible search box / Ctrl+F focus model.
- Persisting the last search across restarts.
