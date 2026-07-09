# Future Feature: User-Editable Blacklist from VFS Browser

**Status:** Planned (future, not in active phases)
**Depends on:** VFS browser working, game detection working, blacklist registry loaded at startup

## Motivation

The static blacklist (`data/blacklist.json`) is curated by research but can never cover every edge case. Users will encounter game folders where the auto-detection picks the wrong executable (launcher stub instead of the game, dev tool instead of the game, etc.). They need a way to correct this without editing JSON files.

## Feature Description

When browsing the Virtual File System (VFS), the user should have interactive control over which executables are treated as game candidates.

### Core Interactions

| Action | Hotkey | Behaviour |
|--------|--------|-----------|
| Blacklist executable | (TBD) | Add the selected file's name pattern to a **user-defined blacklist category**, persist, re-evaluate folder |
| Unblacklist executable | (TBD) | Remove a pattern from user-defined blacklist, persist, re-evaluate folder |
| Toggle blacklisted items | (TBD) | Show/hide blacklisted items in VFS (shown in a dimmed/different colour) |

### Blacklist Categories

When a user blacklists an executable, they assign it to a category (not just "noise"):

1. **"Installer/Redist"** — installers, VC++ redist, DirectX, etc.
2. **"Launcher/Stub"** — game launchers that aren't the game itself (e.g. `AC3.exe` 200KB vs `AC3SP.exe` 15MB)
3. **"Crash Reporter"** — crash handlers, BugSplat, etc.
4. **"Store/DRM/Service"** — store bootstraps, anti-cheat services, overlay helpers
5. **"Tool/Editor"** — dev tools, editors, modding tools shipped with games
6. **"Other"** — user-defined freeform

Categories control the colour/hint shown in VFS when blacklisted items are visible.

### VFS Visual Treatment

| State | Appearance |
|-------|------------|
| Normal (game candidate) | Normal listing colour |
| Blacklisted, hidden | Not shown (default) |
| Blacklisted, visible | Dimmed / greyed out, with category label |
| Ambiguous (multiple candidates, unsure) | Highlighted, prompt user to pick |

### Persistence

User additions are stored in a separate file from the shipped blacklist:

```
~/.config/gamingcommander/user-blacklist.json    # Linux dev
%APPDATA%/GamingCommander/user-blacklist.json    # Windows runtime
```

Format mirrors the static blacklist but adds a `_user_added: true` flag on each pattern. On startup, both files are merged. The user file is never overwritten by updates.

### User Workflow

1. User opens VFS on a game folder
2. Panel shows detected game executable(s) — highlighted as the primary launch target(s)
3. User sees a secondary executable they know is wrong (e.g. `blender-2.42-windows.exe` in a Vampire Bloodlines folder)
4. User highlights it, presses hotkey (e.g. `F8` or `Del`)
5. Dialog: "Blacklist 'blender-2.42-windows.exe'?" → Category picker → Confirm
6. Pattern `"blender"` is added to user-blacklist.json under "Tool/Editor" category
7. VFS re-evaluates: that exe disappears (or dims), game still detected correctly
8. If detection breaks (no game exe left), user is warned before applying

### Unblacklist Workflow

1. User toggles "Show blacklisted items" (hotkey TBD)
2. Previously hidden/dimmed items appear in a distinct colour
3. User moves cursor to a blacklisted item, presses the same hotkey
4. Dialog: "Remove 'blender' from user blacklist?" → Confirm
5. Pattern removed from user-blacklist.json, VFS re-evaluates

### Implementation Notes

- The C# blacklist loader must support merging two sources (shipped + user)
- Each pattern record should track its source (`builtin` vs `user`)
- Categories are stored as strings so they're extensible without recompilation
- The "evaluate" function (`is_noise_exe(name)`) is shared by both detection and VFS display
- Filesize heuristic (distinguishing launcher stubs from game exes) would be a complementary feature — the user blacklist is the manual override for when heuristics fail

### Open Questions

- Should the user be able to remove builtin patterns? (Probably not — they can only add to their user file, never modify the shipped file)
- Should we track which game folder the pattern came from? (Useful for folder-local blacklisting but adds complexity)
- Hotkey binding — need to check if keys F7-F12 are already claimed by other commander-style functions

### Out of Scope (V1)

- Machine learning / auto-suggestion of blacklist entries
- Cloud sync of user blacklists
- Regex patterns (substring only for now)
