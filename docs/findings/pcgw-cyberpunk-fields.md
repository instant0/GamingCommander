# PCGW Cyberpunk 2077 — what the page actually sends

**Harvested:** 2026-08-22 via `action=parse&prop=wikitext` (59 121 chars). Cargo still denied.  
**Fixtures:** `tests/GamingCommander.App.Tests/Fixtures/pcgw/cyberpunk_*.wikitext`

This is the receive contract for Plan 120 parsers. Do not invent extra templates.

---

## Prepared to receive (parse)

| Block | Live template | We keep |
|-------|---------------|---------|
| Config paths | `{{Game data/config\|Windows\|{{P\|localappdata}}\CD Projekt Red\Cyberpunk 2077}}` | OS + raw token path (Windows + OS X) |
| Save paths | `{{Game data/saves\|Windows\|{{p\|userprofile}}\Saved Games\…}}` | same |
| Cloud flags | `{{Save game cloud syncing\|steam cloud=true\|gog galaxy=true\|epic games launcher=true}}` | non-empty launcher keys only |
| Cmdline table | `{{Standard table/row\|-width X\|Sets game resolution width to X.}}` | 15 rows; `NeedsValue` when a placeholder follows the flag |
| Bypass / extras | `{{Fixbox\|description=…<code>--launcher-skip -skipStartScreen -modded</code>…}}` | split flags + per-arg `{{note}}` text |
| Direct exe | `{{file\|{{P\|game}}\bin\x64\Cyberpunk2077.exe}}` | suggested executable template |
| Video caps | `{{Video\|ultrawidescreen=limited\|fov=true\|hdr=limited\|…}}` | short values; drop `* notes`, WSGF awards, `* tech` |

`--launcher-skip` and `-modded` are **only** in the Fixbox, not in the Standard table. Merge is required.

Tokens stay stored as `{{P|localappdata}}` / `{{p|userprofile}}` / `{{P|game}}`. UI resolves Windows env names later. `{{P|osxhome}}` is left untouched.

---

## Present on the page, not parsed (v1)

- Infobox taxonomy beyond genre (modes, themes, series) — 119 already takes developer/engine/date/genre  
- `{{Availability/row}}` (Steam 1091500, GOG/Epic slugs, DRM-free note)  
- Issues / Mods / INI / System requirements / Input / Audio / VR  
- Video essays (ultrawide HUD, HDR setup)  
- Pastebin “more arguments” under the cmdline table  

---

## Command-line catalog (live table + Fixbox merge)

**Table:** `-width X`, `-height Y`, `-fullscreen`, `-borderless`, `-windowed`, `-x X`, `-y Y`, `-monitor N`, `-fpsClamp N`, `-noHUD`, `-skipStartScreen`, `-d3d12`, `-gpuFlag FLAG`, `-qualityLevel`, `-benchmark`  

**Fixbox only:** `--launcher-skip`, `-modded`  

Flags with placeholders (`X`, `Y`, `N`, `FLAG`) are not F4 checkboxes — free-text extras.
