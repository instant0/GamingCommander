# Keyboard Layout & UX Proposal

## Current Layout

```
F1=Help  F2=Setup  F3=View    —    F5=Launch    —    —    F8=Filter  F9=Roots  F10=Quit
                                                                                    ^
                                                    Enter=Drill in  (does NOT launch)
                                                    Backspace=Go up
                                                    Up/Down=Navigate
                                                    T=Retag  (hidden in hint text)
                                                    S=Search  (not implemented)
```

### Problems

1. **Enter does not launch games.** This is the most critical UX flaw. Users naturally press Enter to activate the selected item. On a game entry, Enter does nothing (game entries have `Kind=File`, `IsBrowsable=false`). Users must discover F5.

2. **F5=Launch conflicts with Norton Commander convention.** In every Commander-style file manager (NC, TC, FAR, Midnight Commander), F5=Copy. For game-launch context this is tolerable *if* Enter also launches — but currently it's the only launch method.

3. **Command bar has gaps** — F4, F6, F7 are missing from the bar, creating visual discontinuity:
   ```
   F1  F2  F3  _  F5  _  _  F8  F9  F10
   ```

4. **T for retag is undiscoverable.** It only appears in the interaction hint string, not in the command bar. Users won't find it.

5. **Double-tap does not launch games.** Currently only drills into directories.

6. **Exit/Return keys unequally weighted:** Backspace is the only "go up". No Esc binding.

---

## Norton Commander Heritage (Reference)

The canonical NC F-key layout that millions of users know:

| Key | NC Function | GamingCommander (current) | Conflict? |
|-----|------------|--------------------------|-----------|
| F1 | Help | Help | ✓ Match |
| F2 | User Menu | Setup | Minor |
| F3 | View | View/metadata (planned) | ✓ Match |
| F4 | Edit | — | Unused slot |
| F5 | Copy | Launch | **Major** |
| F6 | Rename/Move | — | Unused slot |
| F7 | MkDir | — | Unused slot |
| F8 | Delete | Filter/category | **Major** |
| F9 | Menu bar | Jump to roots | Significant |
| F10 | Quit | Quit | ✓ Match |

The NC layout is not sacred — this is a game launcher, not a file manager. But the Commander-style interface sets an expectation of F-key discoverability and consistency.

---

## Design Principles

1. **Most frequent action = least friction.** Launch is the #1 action. It should trigger on Enter.
2. **Discoverability.** Every function key shown in the bar should do something useful. No gaps.
3. **NC familiarity where it helps.** Users coming from TC/FAR should feel at home where the mapping makes sense for game ops.
4. **No dead keys.** Every F-key in the bar has a purpose — even if it's a placeholder that says "coming soon."
5. **Destroy operations need distance.** Delete/uninstall should be far from Enter.

---

## Proposed Layout

### Part A: Enter launches games (CRITICAL CHANGE)

```
On a DIRECTORY:  Enter = drill in  (unchanged)
On a GAME:       Enter = launch    (NEW — was: nothing)
On "..":         Enter = go up     (unchanged)
```

This single change fixes the #1 UX problem. F5 becomes a secondary/alternative launch method.

Implementation: In `NavigateInto()`, if the selected item's `Kind` is `File`, call `LaunchSelectedGameAsync()` instead of `LoadGamesForRoot()`.

### Part B: Revised F-key Layout

```
F1     F2       F3      F4      F5      F6        F7     F8        F9      F10
Help   Setup    View    Edit    Launch  Refresh   Add    Filter    Roots   Quit
              (meta)  (retag)          (rescan)  (root) (category)
```

| Key | Function | When | NC Heritage |
|-----|----------|------|-------------|
| F1 | Help | Always | ✓ |
| F2 | Library Setup | Always | ≈ (F2=Menu) |
| F3 | View Metadata | Game selected | ✓ (F3=View) |
| **F4** | **Edit / Retag** | Game selected | ✓ (F4=Edit) — **MOVED from T** |
| F5 | Launch | Game selected | ≈ (NC=Copy; for a launcher, Launch is defensible) |
| **F6** | **Refresh Rescan** | At root or game level | ≈ (F6=Rename; Rescan = refresh state) |
| **F7** | **Add Root** | At root level | ≈ (F7=MkDir; Add root = create new entry) |
| **F8** | **Filter / Category** | Always | ✗ (NC=Delete). Delete is not a primary game-mgmt op |
| F9 | Jump to Roots | Always | ✗ (NC=Menu). Re-purposed for drive/root navigation |
| F10 | Quit | Always | ✓ |

### Part C: Keyboard-only navigation additions

| Key | Function | Notes |
|-----|----------|-------|
| **Esc** | Go up / Deselect | Esc = go up (same as Backspace) or cancel current operation |
| **Space** | Toggle selection / Quick view | Future: quick metadata peek |
| **Ctrl+F** | Focus search bar | When S-search is implemented |
| **Delete** | Remove game from library | Destructive, kept distant from Enter |
| **Tab** | Switch between left/right pane | Future dual-pane navigation |

### Part D: Command bar display

```
┌─────────────────────────────────────────────────────────────────┐
│ F1 Help  F2 Setup  F3 View  F4 Edit  F5 Launch  F6 Refresh    │
│ F7 Add   F8 Filter F9 Roots F10 Quit                          │
└─────────────────────────────────────────────────────────────────┘
```

Two-line layout to show all 10 keys without cramming. Each entry ≤120px wide.

When a key is not applicable (e.g. F3/F4 at root level), the label dims or shows a dash:
```
F1 Help  F2 Setup  F3 View  F4 —  F5 Launch  F6 Refresh
F7 Add   F8 Filter F9 Roots F10 Quit
```

### Part E: Mouse additions

| Action | Function |
|--------|----------|
| **Double-tap on game** | Launch (was: nothing) |
| **Double-tap on directory** | Drill in (unchanged) |
| **Click command bar button** | Trigger key action (unchanged) |

---

## Implementation Plan

### Step 1: Enter launches games (single-file change in MainWindow.axaml.cs)

In `OnKeyDown`, keep `Enter → NavigateInto()` as-is.
In `NavigateInto()` method (ShellViewModel), add:

```csharp
public void NavigateInto()
{
    ShellPaneItemViewModel? item = SelectedItem;
    if (item is null || !item.IsBrowsable) return;

    // Handle ".." parent-directory entry — go up one level
    if (item.Kind == FileSystemEntryKind.ParentDirectory)
    {
        NavigateUp();
        return;
    }

    // Handle game file — launch it
    if (item.Kind == FileSystemEntryKind.File)
    {
        RequestLaunch?.Invoke(item);
        return;
    }

    // Handle directory — drill in
    // ... existing code ...
}
```

Add `public event Action<ShellPaneItemViewModel>? RequestLaunch;` to ShellViewModel.
Wire it in MainWindow to `LaunchSelectedGameAsync()`.

### Step 2: F4 = Retag (move from T)

Add `case Key.F4:` to the switch in OnKeyDown → call retag logic.
Keep `case Key.T:` as an alternative shortcut.

### Step 3: F6 = Refresh Rescan

Rescan the current root: `_libraryManager.RescanRoot(currentPath, _scanner.Scan(...))`.

### Step 4: F7 = Add Root

Open folder picker directly (currently only available via F2 → Library Setup window).

### Step 5: Update command bar

Refactor `ShellViewModel.Commands` to include all 10 F-keys in order.
Use two-line WrapPanel or adjust ItemWidth to fit.

### Step 6: Esc = Go up

Add `case Key.Escape:` → `NavigateUp()`.

### Step 7: Update double-tap to launch games

In `LeftListBox_DoubleTapped`, if item is a game file, launch it.

---

## Summary of Key Changes

| Change | Impact | Effort |
|--------|--------|--------|
| Enter launches games | **High** — fixes #1 UX problem | 1 file, ~10 lines |
| F4 = Retag (was T) | Medium — discoverability | 1 file, ~5 lines |
| F6 = Refresh | Medium — new feature | 2 files, ~20 lines |
| F7 = Add Root | Medium — convenience | 2 files, ~30 lines |
| Esc = Go up | Low — expected behavior | 1 file, ~2 lines |
| Double-tap launches games | Medium — mouse parity | 1 file, ~3 lines |
| Command bar shows all keys | Low — cosmetic | 1 file, ~10 lines |

**Total: ~80 lines changed across 4 files.**
