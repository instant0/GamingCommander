#!/usr/bin/env python3
"""Look up structured game metadata from local signals + PCGamingWiki.

Purpose
-------
Research tool that chains:
  1. Folder-level store detection (reuses detect_folder.py logic)
  2. Store-specific identifier extraction (GOG gameId, Steam AppID,
     Epic CatalogItemId, RGL title ID, etc.)
  3. PCGamingWiki metadata lookup (Cargo API + parse API)
  4. Local PE metadata scan (version info, company name)
  5. Merged structured output

This is a DEVELOPMENT-ENVIRONMENT-ONLY research tool.  It will NOT
be shipped inside the C# application.  Its purpose is to validate
the metadata pipeline so that the C# implementation (Phase 2.2) has
known-good API endpoints, field names, and data shapes.

Usage
-----
  # Batch mode — process a detect_folder.py output file
  python tools/lookup_metadata.py \\
      --input /path/to/detect_results.json \\
      --output /path/to/enriched.json

  # Single folder mode — detect and look up one game
  python tools/lookup_metadata.py --folder /path/to/GameFolder

  # Dry run — show what would be queried without calling APIs
  python tools/lookup_metadata.py --folder /path/to/GameFolder --dry-run

Output
------
A JSON object with keys per game (keyed by folder name).  Each value:

  {
    "folder": "GameFolderName",
    "store": "GOG",
    "signal": "goggame",
    "identifiers": { ... store-specific IDs ... },
    "name_candidates": [ "Game Name", ... ],
    "pcgw": {
      "page_title": "Game Name (PCGW)",
      "page_url": "https://pcgamingwiki.com/wiki/Game_Name",
      "source": "cargo" | "parse" | "opensearch" | null,
      "metadata": { ... structured fields ... }
    } | null,
    "pe_metadata": { ... PE version info ... } | null,
    "confidence": "high" | "medium" | "low"
  }

Environment Variables
--------------------
  PCGW_USER_AGENT    User-Agent for PCGW API calls
                      (default: "GamingCommander/0.1 (research)")
"""

from __future__ import annotations

import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request
from pathlib import Path

# ── Optional dependencies ─────────────────────────────────────

try:
    import pefile  # type: ignore[import-untyped]
except ImportError:
    pefile = None

# ── Constants ──────────────────────────────────────────────────

PCGW_API = "https://www.pcgamingwiki.com/w/api.php"
EPIC_STORE_API = "https://store.epicgames.com/graphql"

USER_AGENT = os.environ.get("PCGW_USER_AGENT", "GamingCommander/0.1 (research)")

# Delay (seconds) between PCGW API calls to avoid 429 rate limiting.
DEFAULT_RATE_LIMIT_S = 0.6

# Global Epic manifests directory (set via --epic-manifests-dir CLI arg).
# On Windows this is typically:
#   %ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\
EPIC_MANIFESTS_DIR: str | None = None

# ── Logging helper ────────────────────────────────────────────


def _log(msg: str, *args) -> None:
    if args:
        msg = msg % args
    print(msg, file=sys.stderr)


# ── PCGW API helpers (with rate limiting) ──────────────────────


_last_call: float = 0.0


def _rate_limit(min_interval_s: float = DEFAULT_RATE_LIMIT_S) -> None:
    """Pause if needed to respect the minimum interval between calls."""
    global _last_call
    elapsed = time.time() - _last_call
    if elapsed < min_interval_s:
        time.sleep(min_interval_s - elapsed)
    _last_call = time.time()


def _pcgw_call(params: dict) -> dict | None:
    """Make a MediaWiki API call to PCGW and return parsed JSON.

    Returns ``None`` on any network or HTTP error.
    """
    _rate_limit()
    url = PCGW_API + "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except Exception as exc:
        _log("  [PCGW API error] %s: %s", params.get("action", "?"), exc)
        return None


# ── Cargo API ─────────────────────────────────────────────────

# Mapping from Cargo field name → human-friendly output key.
# The Cargo API uses underscores in fields but returns camel-case/spaced keys.
# Cargo fields validated against the live PCGW ``Infobox_game`` table.
# Fields that do not exist in Cargo (``Engine``, ``Modes``, ``Perspectives``,
# ``Series``) are omitted — the Cargo API rejects unknown fields and returns
# an error.  Those fields are available via the Parse API fallback instead.
CARGO_FIELDS = {
    "Developers": "developers",
    "Publishers": "publishers",
    "Released": "release_date",
    "Genres": "genres",
    "Steam_AppID": "steam_appid",
    "GOGcom_ID": "gogcom_id",
    "Cover": "cover_url",
}


def cargo_lookup_by_id(field: str, value: str) -> dict | None:
    """Query Infobox_game by store ID (e.g. Steam_AppID HOLDS "271590").

    Args:
        field: Cargo field name, e.g. ``Steam_AppID``, ``GOGcom_ID``.
        value: The ID value to search for.

    Returns:
        Mapped metadata dict, or ``None`` if no result.
    """
    fields_list = list(CARGO_FIELDS.keys())
    params = {
        "action": "cargoquery",
        "tables": "Infobox_game",
        "fields": ",".join(fields_list),
        "where": f'Infobox_game.{field} HOLDS "{value}"',
        "format": "json",
        "limit": "1",
    }
    data = _pcgw_call(params)
    if not data:
        return None
    rows = data.get("cargoquery", [])
    if not rows:
        return None
    raw = rows[0]["title"]
    return _map_cargo_row(raw)


def cargo_lookup_by_page(page_title: str) -> dict | None:
    """Query Infobox_game by exact PCGW page title.

    Args:
        page_title: PCGW page title (spaces, not underscores).

    Returns:
        Mapped metadata dict, or ``None``.
    """
    fields_list = list(CARGO_FIELDS.keys())
    params = {
        "action": "cargoquery",
        "tables": "Infobox_game",
        "fields": ",".join(fields_list),
        "where": f'Infobox_game._pageName="{page_title}"',
        "format": "json",
        "limit": "1",
    }
    data = _pcgw_call(params)
    if not data:
        return None
    rows = data.get("cargoquery", [])
    if not rows:
        return None
    raw = rows[0]["title"]
    return _map_cargo_row(raw)


def _map_cargo_row(raw: dict) -> dict:
    """Map Cargo API ``title`` dict to our metadata shape."""
    result: dict = {}
    for cargo_key, our_key in CARGO_FIELDS.items():
        # Cargo may return keys with spaces instead of underscores
        # e.g. "Steam AppID" instead of "Steam_AppID"
        api_key = cargo_key.replace("_", " ")
        val = raw.get(api_key, raw.get(cargo_key, ""))
        if val and str(val).strip():
            cleaned = val.strip()
            # Strip "Company:" prefix from developer/publisher names
            if cargo_key in ("Developers", "Publishers"):
                cleaned = re.sub(r"\s*Company:\s*", "", cleaned)
            result[our_key] = cleaned
    # Strip precision suffix from release date
    rp = raw.get("Released__precision")
    if rp and "release_date" in result:
        result["release_precision"] = str(rp)
    return result


# ── Parse API (raw wikitext extraction) ───────────────────────


def parse_infobox(page_title: str) -> dict | None:
    """Extract all Infobox game fields via the parse API (wikitext).

    This is the **fallback** when Cargo returns nothing for a known
    page.  It extracts field values from the raw wiki markup.

    Returns:
        Metadata dict or ``None`` on failure.
    """
    params = {
        "action": "parse",
        "page": page_title,
        "prop": "wikitext",
        "format": "json",
    }
    data = _pcgw_call(params)
    if not data:
        return None
    wikitext = data.get("parse", {}).get("wikitext", {}).get("*", "")
    if not wikitext:
        return None
    return _extract_infobox_fields(wikitext)


def _extract_infobox_fields(wikitext: str) -> dict:
    """Parse key=value pairs and templates from an Infobox game block.

    Handles:
    - Raw ``|key = value`` lines
    - ``{{Infobox game/row/developer|...}}`` templates
    - Taxonomy templates (genre, mode, perspective, etc.)
    """
    fields: dict = {}

    # 1. Direct key=value lines
    for m in re.finditer(r"^\|(\w[\w ]*?)\s*=\s*(.+?)\s*$", wikitext, re.MULTILINE):
        key = m.group(1).strip().lower().replace(" ", "_")
        val = m.group(2).strip()
        val = _clean_wikitext(val)
        if val:
            fields[key] = val

    # 2. Developer / Publisher / Engine templates
    for m in re.finditer(
        r'{{Infobox game/row/developer\|(.+?)(?:\||}})', wikitext
    ):
        _add_list(fields, "developers", _clean_wikitext(m.group(1)))
    for m in re.finditer(
        r'{{Infobox game/row/publisher\|(.+?)(?:\||}})', wikitext
    ):
        _add_list(fields, "publishers", _clean_wikitext(m.group(1)))
    for m in re.finditer(
        r'{{Infobox game/row/engine\|(.+?)(?:\||}})', wikitext
    ):
        _add_list(fields, "engine", _clean_wikitext(m.group(1)))
    for m in re.finditer(
        r'{{Infobox game/row/date\|Windows\|(.+?)(?:\||}})', wikitext
    ):
        fields.setdefault("release_dates", []).append(
            _clean_wikitext(m.group(1))
        )

    # 3. Taxonomy templates
    for m in re.finditer(
        r'{{Infobox game/row/taxonomy/genres\|(.+?)(?:\||}})', wikitext
    ):
        for g in m.group(1).split(","):
            cleaned = _clean_wikitext(g)
            if cleaned:
                _add_list(fields, "genres", cleaned)
    for m in re.finditer(
        r'{{Infobox game/row/taxonomy/modes\|(.+?)(?:\||}})', wikitext
    ):
        _add_list(fields, "modes", _clean_wikitext(m.group(1)))
    for m in re.finditer(
        r'{{Infobox game/row/taxonomy/perspectives\|(.+?)(?:\||}})', wikitext
    ):
        _add_list(fields, "perspectives", _clean_wikitext(m.group(1)))
    for m in re.finditer(
        r'{{Infobox game/row/taxonomy/themes\|(.+?)(?:\||}})', wikitext
    ):
        _add_list(fields, "themes", _clean_wikitext(m.group(1)))

    return fields


def _add_list(d: dict, key: str, value: str) -> None:
    if value:
        d.setdefault(key, []).append(value)


def _clean_wikitext(text: str) -> str:
    """Strip MediaWiki markup from a value string."""
    text = re.sub(r"<!--.*?-->", "", text, flags=re.DOTALL)
    text = re.sub(r"<ref[^>]*/>", "", text)
    text = re.sub(r"<ref>.*?</ref>", "", text, flags=re.DOTALL)
    text = re.sub(r"{{Refurl\|.*?}}", "", text, flags=re.DOTALL)
    text = re.sub(r"\[\[[^|\]]*\|([^]]*)\]\]", r"\1", text)
    text = re.sub(r"\[\[([^]]*)\]\]", r"\1", text)
    text = re.sub(r"{{[^}]*}}", "", text)
    text = re.sub(r"<[^>]+>", "", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text.strip(",| ")


# ── Epic Games Store API helpers ──────────────────────────────
# These resolve Epic internal IDs (namespace + catalogItemId) to
# human-readable game titles via the Epic Store GraphQL API.
# They are OPTIONAL — only used when the game is detected as Epic
# and no .item file cross-reference is available.


def epic_search_by_namespace(namespace: str) -> list[dict]:
    """Query Epic ``searchStore`` filtered by ``namespace``.

    This resolves an Epic internal namespace UUID (e.g.
    ``caca23a0954f4c1aba1fdd7e277b81e2``) to one or more
    store offers (base game + editions).  Returns the ``elements``
    list from the GraphQL response.

    The response includes **rich metadata** for game identification:
      - ``title`` — display name
      - ``publisherDisplayName``, ``developerDisplayName``
      - ``productSlug`` — Epic store URL path
      - ``releaseDate`` — ISO date string
      - ``keyImages`` — array of {type, url} for cover art (multiple sizes)
      - ``description`` — short game description
      - ``customAttributes`` — structured key/value pairs including
        ``developerName``, ``publisherName``, ``productSlug``

    WARNING
    -------
    The ``CatalogNamespace`` in ``.mancpn`` may be a **dev/testing**
    namespace (not the public game namespace).  For example, Death
    Stranding's ``.mancpn`` has namespace ``f4a904…`` which resolves
    to ``BogaDevAudience``, an internal testing tool, not the actual
    game.  In those cases the API returns misleading results.

    The ``CatalogNamespace`` in an ``.item`` file (from the global
    manifests directory) is always the **correct** public namespace.
    Prefer ``.item`` cross-reference over this API call.
    """
    if not namespace or not isinstance(namespace, str) or len(namespace) < 10:
        return []
    query = (
        '{ Catalog { searchStore(start: 0, count: 5, '
        'namespace: "%s") { elements { title id namespace '
        'productSlug publisherDisplayName developerDisplayName '
        'releaseDate offerType status description '
        'keyImages { type url } '
        'customAttributes { key value } } } } }' % namespace
    )
    payload = json.dumps({"query": query}).encode("utf-8")
    req = urllib.request.Request(
        EPIC_STORE_API,
        data=payload,
        headers={
            "Content-Type": "application/json",
            "User-Agent": USER_AGENT,
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            result = json.loads(resp.read())
        elements = (
            result.get("data", {})
            .get("Catalog", {})
            .get("searchStore", {})
            .get("elements", [])
        )
        return elements
    except Exception:
        return []


def epic_crossref_item_manifests(folder_path: Path) -> dict | None:
    """Cross-reference an Epic game folder against the global
    ``.item`` manifest directory.

    The global manifests directory (set via ``--epic-manifests-dir``
    or ``EPIC_MANIFESTS_DIR`` env var) contains ``.item`` files that
    the Epic Games Launcher writes when installing games.  Each
    ``.item`` has an ``InstallLocation`` field that matches the game
    folder, plus a human-readable ``DisplayName``.

    Returns the parsed ``.item`` dict, or ``None`` if:
    - ``EPIC_MANIFESTS_DIR`` is not configured
    - The directory does not exist
    - No ``.item`` file's ``InstallLocation`` matches ``folder_path``

    On Windows the manifests directory is typically:
      ``%ProgramData%\\Epic\\EpicGamesLauncher\\Data\\Manifests\\``
    """
    global EPIC_MANIFESTS_DIR
    manifests_path = (
        Path(EPIC_MANIFESTS_DIR)
        if EPIC_MANIFESTS_DIR
        else None
    )
    if not manifests_path or not manifests_path.is_dir():
        return None

    target = str(folder_path.resolve()).lower()
    for item_file in manifests_path.glob("*.item"):
        try:
            data = json.loads(
                item_file.read_text(encoding="utf-8", errors="ignore")
            )
        except (json.JSONDecodeError, OSError):
            continue
        install_loc = data.get("InstallLocation", "")
        if install_loc and install_loc.lower().rstrip("\\/") == target.rstrip("\\/"):
            return data
    return None


# ── OpenSearch (name-based page discovery) ───────────────────


def opensearch_find_pages(search_term: str) -> list[dict]:
    """Find PCGW pages by game name.

    Returns a list of ``{"title": ..., "url": ...}`` dicts, ordered
    by PCGW search relevance.  Empty list if no matches.
    """
    params = {
        "action": "opensearch",
        "search": search_term,
        "limit": "5",
        "namespace": "0",
        "format": "json",
    }
    data = _pcgw_call(params)
    if not data:
        return []
    results: list[dict] = []
    if len(data) >= 4 and data[1]:
        for i, title in enumerate(data[1]):
            url = data[3][i] if i < len(data[3]) else ""
            results.append({"title": title, "url": url or ""})
    return results


# ── Store-specific identifier extraction ──────────────────────


def _extract_gog_identifiers(folder: Path) -> dict:
    """Extract gameId and game name from ``goggame-*.info`` files.

    When multiple .info files exist (base game + DLCs), prefer the
    entry whose ``gameId == rootGameId`` (the main game).
    """
    ids: dict = {}
    main_entry: dict | None = None

    for info_file in sorted(folder.glob("goggame-*.info")):
        try:
            data = json.loads(info_file.read_text(encoding="utf-8", errors="ignore"))
        except (json.JSONDecodeError, OSError):
            continue
        is_main = data.get("gameId") == data.get("rootGameId")
        if is_main:
            main_entry = data
        if not ids:
            ids["gog_game_id"] = data.get("gameId", "")
            ids["name"] = data.get("name", "")
            if data.get("rootGameId"):
                ids["gog_root_game_id"] = data["rootGameId"]
            for task in data.get("playTasks", []):
                if task.get("isPrimary") and task.get("path"):
                    ids["exe"] = task["path"]

    # Overwrite with main game data if found
    if main_entry:
        ids["gog_game_id"] = main_entry.get("gameId", ids.get("gog_game_id", ""))
        ids["name"] = main_entry.get("name", ids.get("name", ""))
        if main_entry.get("rootGameId"):
            ids["gog_root_game_id"] = main_entry["rootGameId"]
        for task in main_entry.get("playTasks", []):
            if task.get("isPrimary") and task.get("path"):
                ids["exe"] = task["path"]

    return ids


def _extract_steam_identifiers(folder: Path) -> dict:
    """Extract Steam AppID from ``steam_appid.txt`` at root."""
    ids: dict = {}
    appid_file = folder / "steam_appid.txt"
    if appid_file.exists():
        try:
            text = appid_file.read_text(encoding="utf-8", errors="ignore").strip()
            ids["steam_appid"] = text.split("\n")[0].strip()
        except OSError:
            pass
    return ids


def _extract_epic_identifiers(folder: Path) -> dict:
    """Extract Epic catalog info from ``.egstore/`` or ``.egsstore/`` manifests.

    Two file types may exist:
    - ``*.mancpn`` (JSON) — inside the game's .egstore/ dir.
      Contains ``AppName``, ``CatalogItemId``, ``CatalogNamespace``.
    - ``*.item`` (JSON) — inside the ``Manifests/`` subdir or .egstore/ root.
      Contains ``DisplayName``, ``CatalogItemId``, ``CatalogNamespace``,
      ``LaunchExecutable``.

    Prefer .item when present (richer fields), fall back to .mancpn.
    """
    ids: dict = {}
    for store_dir_name in (".egstore", ".egsstore"):
        store_dir = folder / store_dir_name
        if not store_dir.is_dir():
            continue
        # Check .item files first (richer schema)
        search_dirs = [store_dir / "manifests", store_dir]
        for sd in search_dirs:
            if not sd.is_dir():
                continue
            for item_file in sd.glob("*.item"):
                try:
                    data = json.loads(
                        item_file.read_text(encoding="utf-8", errors="ignore")
                    )
                except (json.JSONDecodeError, OSError):
                    continue
                ids["epic_display_name"] = data.get("DisplayName", "")
                ids["epic_catalog_item_id"] = data.get("CatalogItemId", "")
                ids["epic_catalog_namespace"] = data.get("CatalogNamespace", "")
                ids["epic_app_name"] = data.get("AppName", "")
                ids["exe"] = data.get("LaunchExecutable", "")
                break
            if ids.get("epic_catalog_item_id"):
                break
        if ids.get("epic_catalog_item_id"):
            break

        # Fall back to .mancpn files at store_dir root
        for mancpn_file in store_dir.glob("*.mancpn"):
            try:
                data = json.loads(
                    mancpn_file.read_text(encoding="utf-8", errors="ignore")
                )
            except (json.JSONDecodeError, OSError):
                continue
            ids["epic_catalog_item_id"] = data.get("CatalogItemId", "")
            ids["epic_catalog_namespace"] = data.get("CatalogNamespace", "")
            ids["epic_app_name"] = data.get("AppName", "")
            break
        if ids.get("epic_catalog_item_id"):
            break
    return ids


def _extract_rockstar_identifiers(folder: Path) -> dict:
    """Extract RGL title ID from ``title.rgl``."""
    ids: dict = {}
    rgl_file = folder / "title.rgl"
    if rgl_file.exists():
        try:
            text = rgl_file.read_text(encoding="utf-8", errors="ignore").strip()
            ids["rgl_title_id"] = text
        except OSError:
            pass
    return ids


def extract_identifiers(store: str, folder: Path) -> dict:
    """Route to the right identifier extractor for the given store."""
    extractors = {
        "GOG": _extract_gog_identifiers,
        "Steam": _extract_steam_identifiers,
        "Epic": _extract_epic_identifiers,
        "Rockstar": _extract_rockstar_identifiers,
    }
    extractor = extractors.get(store)
    if extractor:
        return extractor(folder)
    return {}


# ── PE metadata ────────────────────────────────────────────────


# Shared non-game executable pattern list.
# Tier 1: Universal noise (installers, redistributables)
# Tier 2: Launcher stubs
# Tier 3: Anti-cheat / DRM
# Tier 4: Store bootstraps
# Tier 5: Unreal Engine build/debug tools
NOISE_EXE_PARTS: tuple[str, ...] = (
    # Tier 1 — Universal noise
    "cleanup",
    "touchup",
    "crash",
    "installer",
    "unins",
    "uninstall",
    "setup",
    "redist",
    "vcredist",
    "dxsetup",
    "oalinst",
    "dotnet",
    "directx",
    "physx",
    "eos",
    "msi",
    "msiexec",
    "xna",
    "ndp",
    "dotnetfx",
    # Tier 2 — Launcher stubs
    "launcher",
    "updater",
    "patcher",
    "startup",
    "bootstrapper",
    # Tier 3 — Store bootstraps & integration stubs
    "galaxy",
    "gog",
    "epic",
    "steam",
    "uplay",
    "ubisoft",
    # Tier 4 — Anti-cheat / DRM
    "easyanticheat",
    "battleye",
    "beclient",
    "beservice",
    "equ8",
    "punkbuster",
    "nprotect",
    "xigncode",
    "denuvo",
    "vmprotect",
    # Tier 5 — Unreal build/debug tools
    "crashreportclient",
    "unrealcefsubprocess",
    "symboldump",
    "ubiquitous",
    # Tier 6 — Crash reporting infrastructure
    "crs-",
    "bugsplat",
    # Tier 7 — DRM wrappers & compatibility shims
    "xlive",
    # Tier 8 — Installer/patch utilities shipped alongside games
    "autorun",
    "7za",
    "xdelta",
    # Tier 9 — Dedicated servers, loaders, stubs, updaters
    "server",
    "stub",
    "update",
    "loader",
    "browser",
    "dowser",
    # Tier 10 — Stardock distribution tools (SDCR, Tachyon)
    "sdcr",
    "tachyon",
    # Tier 11 — Dev/content editor tools
    "datacompiler",
    "editor",
    "modmanager",
    "packagemanager",
    "reminder",
    "contented",
    "leveled",
    "resourceed",
    # Tier 12 — Utilities & debug builds
    "install",
    "debug",
    "utils",
    "sndrpt",
    "exception",
    "explorer",
    "brwc",
    "activation",
    "ccmini",
    "acpc",
    # Tier 13 — Trial/stub/demo exes
    "trial",
    "_upp",
    # Tier 14 — Media/codec/streaming tools
    "ffmpeg",
    "ffplay",
    "ffprobe",
    # Tier 15 — Installer/update frameworks
    "squirrel",
    "wininst",
    "w9xpopen",
    # Tier 16 — Runtime interpreters (not games)
    "python",
    "blender",
    # Tier 17 — Web UI / overlay frameworks
    "coherentui",
    "cefhost",
    "awesomium",
    "webview",
    "overlay",
    "scummvm",
    # Tier 18 — Repair/service/helper processes
    "repair",
    "service",
    "helper",
    # Tier 19 — Unreal engine build tools
    "unrealpak",
    # Tier 20 — Patch/update executables
    "patch",
    # Tier 21 — Utility tools that ship alongside games
    "winscp",
    "activate",
)

# Non-game directories to skip entirely during exe scanning
_NOISE_DIR_PARTS: tuple[str, ...] = (
    "__redist",
    "_commonredist",
    "redist",
    "directx",
    "vcredist",
    "dotnet",
    "physx",
    "support",
    "_installer",
    "install",
    "installer",
)


def _find_game_exes(folder: Path) -> list[Path]:
    """Find likely game executables (shallow walk, excludes noise)."""
    noise_parts = NOISE_EXE_PARTS
    noise_dirs = _NOISE_DIR_PARTS
    candidates: list[Path] = []

    def _is_noise_dir(name: str) -> bool:
        lower = name.lower()
        return any(n in lower for n in noise_dirs)

    def walk(d: Path, depth: int = 0):
        if depth > 2:
            return
        try:
            for child in d.iterdir():
                if child.is_file() and child.suffix.lower() == ".exe":
                    name_lower = child.name.lower()
                    if not any(n in name_lower for n in noise_parts):
                        candidates.append(child)
                elif child.is_dir() and depth == 0:
                    if _is_noise_dir(child.name):
                        continue
                    walk(child, depth + 1)
                    b64 = child / "Binaries" / "Win64"
                    if b64.is_dir():
                        walk(b64, depth + 1)
                    bgdk = child / "Binaries" / "WinGDK"
                    if bgdk.is_dir():
                        walk(bgdk, depth + 1)
        except PermissionError:
            pass

    walk(folder)
    return candidates


def scan_pe_metadata(folder: Path) -> dict | None:
    """Extract version-info metadata from game executables.

    Returns a dict with ``FileDescription``, ``ProductName``,
    ``CompanyName``, etc. or ``None`` if pefile is unavailable
    or no exe found.
    """
    if pefile is None:
        return None
    exes = _find_game_exes(folder)
    if not exes:
        return None

    # Score candidates to find the "main" game exe
    folder_name = folder.name.lower()
    folder_tokens = set(
        re.sub(r"[_-]", " ", folder_name).split()
    )
    scored = []
    for exe in exes:
        score = 0
        name_lower = exe.stem.lower()
        if "launcher" in name_lower:
            score -= 20
        if "shipping" in name_lower or "win64" in name_lower:
            score += 5
        if any(t in name_lower for t in folder_tokens):
            score += 10
        try:
            score += min(exe.stat().st_size // 10_000_000, 10)
        except OSError:
            pass
        scored.append((score, exe))

    scored.sort(key=lambda x: x[0], reverse=True)

    # Try up to 3 best candidates for PE metadata
    for _, exe in scored[:3]:
        try:
            pe = pefile.PE(str(exe), fast_load=False)
        except Exception:
            continue
        meta: dict = {}
        # pe.FileInfo may be missing entirely in very old PEs (e.g. Arx Fatalis 2002).
        # When present it is a list; entries may be lists themselves.
        # Normalise to a flat list of entries before iterating.
        raw_infos = getattr(pe, "FileInfo", None)
        if not raw_infos:
            continue
        flat_infos: list = []
        for entry in raw_infos:
            if isinstance(entry, list):
                flat_infos.extend(entry)
            else:
                flat_infos.append(entry)
        for entry in flat_infos:
            tables = getattr(entry, "StringTable", None)
            if tables is None:
                continue
            if not isinstance(tables, list):
                tables = [tables]
            for table in tables:
                for raw_key, raw_value in table.entries.items():
                    key = (
                        raw_key.decode("utf-8", errors="ignore")
                        if isinstance(raw_key, bytes)
                        else str(raw_key)
                    )
                    value = (
                        raw_value.decode("utf-8", errors="ignore")
                        if isinstance(raw_value, bytes)
                        else str(raw_value)
                    )
                    if key in (
                        "FileDescription",
                        "ProductName",
                        "OriginalFilename",
                        "CompanyName",
                        "FileVersion",
                        "LegalCopyright",
                    ):
                        if value.strip():
                            meta[key] = value.strip()
        if meta:
            meta["_exe"] = str(exe.relative_to(folder) if exe.is_relative_to(folder) else exe.name)
            return meta
    return None


# ── Epic name resolution ──────────────────────────────────────
# Epic games have no PCGW Cargo-field ID mapping, so we resolve
# their internal IDs to a human-readable name via these strategies:
#   1. .item file cross-reference (most reliable — Windows-only)
#   2. Epic searchStore by namespace (fallback — may hit dev namespace)
# The resolved name is added to name_candidates for PCGW lookup.
# The returned dict includes publisher info used later for PCGW verification.


def epic_resolve_metadata(folder: Path, identifiers: dict) -> dict | None:
    """Resolve an Epic game's internal IDs to a rich metadata dict.

    The Epic ``searchStore`` API returns **everything needed to identify
    the game**: title, developer, publisher, cover art URLs, description,
    slug, and release year.  PCGW is only needed afterward for
    *enrichment* (engine, save paths, genres, taxonomy).

    Returns ``None`` if resolution fails, or a dict with keys:
      - ``title`` — human-readable game name
      - ``developer`` — developer name (from customAttributes or developerDisplayName)
      - ``publisher`` — publisher display name (from Epic store)
      - ``slug`` — Epic store product slug
      - ``release_year`` — Epic store release year (int or 0)
      - ``description`` — short game description
      - ``cover_url`` — largest cover art URL (OfferImageWide or DieselStoreFrontWide)
    """
    result: dict = {
        "title": "", "developer": "", "publisher": "",
        "slug": "", "release_year": 0,
        "description": "", "cover_url": "",
    }

    # Strategy 1: .item file cross-reference (DisplayName only)
    item = epic_crossref_item_manifests(folder)
    if item:
        name = (item.get("DisplayName") or "").strip()
        if name:
            result["title"] = name

    # Strategy 2: Epic searchStore by namespace — rich data
    ns = identifiers.get("epic_catalog_namespace", "")
    if ns:
        try:
            offers = epic_search_by_namespace(ns)
        except Exception:
            offers = []
        for offer in offers:
            if offer.get("offerType") != "BASE_GAME":
                continue

            # ── Title ──
            title = (offer.get("title") or "").strip()
            if title and not result["title"]:
                result["title"] = title

            # ── Publisher ──
            pub = (offer.get("publisherDisplayName") or "").strip()
            if pub and not result["publisher"]:
                result["publisher"] = pub

            # ── Developer (from multiple sources) ──
            dev_display = (offer.get("developerDisplayName") or "").strip()
            if dev_display:
                result["developer"] = dev_display
            # customAttributes.developerName is often more specific
            for attr in offer.get("customAttributes") or []:
                if attr.get("key") == "developerName":
                    val = (attr.get("value") or "").strip()
                    if val and not result["developer"]:
                        result["developer"] = val

            # ── Slug ──
            slug = (offer.get("productSlug") or "").strip()
            if slug and not result["slug"]:
                result["slug"] = slug
            # Fallback: customAttributes.productSlug
            for attr in offer.get("customAttributes") or []:
                if attr.get("key") == "com.epicgames.app.productSlug":
                    val = (attr.get("value") or "").strip()
                    if val and not result["slug"]:
                        result["slug"] = val

            # ── Release year ──
            rdate = (offer.get("releaseDate") or "").strip()
            if rdate and not result["release_year"]:
                try:
                    result["release_year"] = int(rdate[:4])
                except (ValueError, IndexError):
                    pass

            # ── Description ──
            desc = (offer.get("description") or "").strip()
            if desc:
                result["description"] = desc[:500]

            # ── Cover art (largest wide image) ──
            for img in offer.get("keyImages") or []:
                img_type = img.get("type", "")
                if img_type in ("OfferImageWide", "DieselStoreFrontWide"):
                    url = (img.get("url") or "").strip()
                    if url and not result["cover_url"]:
                        result["cover_url"] = url
                        break
            # Fallback: any wide image
            if not result["cover_url"]:
                for img in offer.get("keyImages") or []:
                    url = (img.get("url") or "").strip()
                    if url and "wide" in img.get("type", "").lower():
                        result["cover_url"] = url
                        break

            break  # Only the BASE_GAME offer

    return result if result.get("title") else None


def _folder_timestamp_year(folder: Path) -> int:
    """Return the latest mtime year from game executables under ``folder``.

    Scans up to depth 2 for ``.exe`` files and returns the most recent
    modification year, or 0 if no exe found.
    """
    latest = 0
    for exe in _find_game_exes(folder):
        try:
            mtime = exe.stat().st_mtime
            if mtime > latest:
                latest = mtime
        except OSError:
            continue
    if latest:
        import datetime
        return datetime.datetime.fromtimestamp(latest).year
    return 0


# ── Name candidates ────────────────────────────────────────────

# PE metadata (FileDescription / ProductName) can be unreliable or generic:
# - Unreal Engine default: "AppName" (very common)
# - Generic placeholders: "Application", "My Project", "Game", "Launcher"
# - Build identifiers: "UE4-xxx", "Shipping", "Development"
# - These must NOT be used as internet search terms (same rule as Epic codenames)
_PE_NAME_BLACKLIST: tuple[str, ...] = (
    "appname", "application", "my application", "my project",
    "game", "launcher", "unreal", "unreal engine",
    "shipping", "development", "debug", "test",
    "ue4", "ue5", "unrealengine",
)


def _find_exe_pe_names(folder: Path) -> list[str]:
    """Extract FileDescription and ProductName from the best PE exe.

    These are far more informative than folder names or stem names.
    For example, ``SRTTR.exe`` has ``ProductName = "Saints Row: The
    Third Remastered"`` — a perfect search term for any store or wiki.

    WARNING
    -------
    PE metadata can contain generic values (e.g. Unreal Engine defaults
    like ``"AppName"``).  They are filtered through ``_PE_NAME_BLACKLIST``
    to prevent them from being used as internet search terms.  This is
    the same safety rule applied to Epic internal codenames.
    """
    if pefile is None:
        return []
    pe_data = scan_pe_metadata(folder)
    if not pe_data:
        return []
    names: list[str] = []
    for key in ("FileDescription", "ProductName"):
        val = (pe_data.get(key) or "").strip()
        if val and val not in names:
            # Reject generic/default PE values that would pollute searches
            val_lower = val.lower().strip()
            if any(bl in val_lower for bl in _PE_NAME_BLACKLIST):
                continue
            names.append(val)
    return names


def _find_exe_stem_names(folder: Path) -> list[str]:
    """Collect executable base names (no extension) as name candidates."""
    names: list[str] = []
    for exe in _find_game_exes(folder):
        stem = exe.stem
        cleaned = re.sub(r"[_-]", " ", stem).strip()
        if cleaned and cleaned not in names:
            names.append(cleaned)
    return names[:5]


def _normalise_name(name: str) -> str:
    """Strip trademark symbols, Unicode variants, and collapse whitespace."""
    cleaned = name
    # Remove common Unicode trade/servicemark symbols
    for ch in ("\u2122", "\u00ae", "\u2120", "\u00a9"):
        cleaned = cleaned.replace(ch, "")
    # Normalize Unicode (NFKC collapses things like ™, ﬁ, etc.)
    import unicodedata
    cleaned = unicodedata.normalize("NFKC", cleaned)
    # Trim whitespace
    cleaned = re.sub(r"\s+", " ", cleaned).strip()
    return cleaned


def _add_spaced_variants(name: str, seen: set, candidates: list) -> None:
    """Add split-at-PascalCase / camelCase variants.

    e.g. ``DeathStranding`` → ``Death Stranding``
         ``TombRaiderGOTYE`` → ``Tomb Raider GOTYE``
         ``ES2Win64Shipping`` → ``ES2 Win64 Shipping``
    """
    # Split on transitions: uppercase→lowercase, lowercase→uppercase,
    # digit→letter, letter→digit.  Preserve digit groups as tokens.
    parts = re.findall(
        r"[A-Z][a-z]+|[A-Z]+(?=[A-Z][a-z]|\d|$)|[a-z]+|\d+|.",
        name,
    )
    # Filter out empty and single-char noise tokens
    parts = [p for p in parts if len(p) > 1 or (p.isalnum() and not p.isdigit())]
    if len(parts) > 1:
        joined = " ".join(parts).strip()
        if joined != name and joined.lower() not in seen:
            seen.add(joined.lower())
            candidates.append(joined)


def build_name_candidates(folder: Path, entry: dict | None, identifiers: dict) -> list[str]:
    """Build ordered list of name candidates from all available sources."""
    candidates: list[str] = []
    seen: set[str] = set()

    def add(name: str):
        if not name:
            return
        normal = _normalise_name(name)
        # Skip very short names and generic terms
        if len(normal) < 3:
            return
        normal_lower = normal.lower()
        if normal_lower in ("", "game", "launcher", "application"):
            return
        # Reject PE-generic defaults that pollute searches (same rule as Epic codenames)
        if any(bl in normal_lower for bl in _PE_NAME_BLACKLIST):
            return
        if normal_lower not in seen:
            seen.add(normal_lower)
            candidates.append(normal)

    # From identifiers (most reliable)
    # NOTE: epic_app_name is deliberately excluded — it's an internal
    # codename (e.g. "Boga" for Death Stranding) that can cause false
    # PCGW matches.
    for key in ("name", "epic_display_name", "DisplayName"):
        val = identifiers.get(key)
        if val:
            add(val)

    # From PE metadata — FileDescription/ProductName is more informative
    # than folder names.  For example, SRTTR.exe has ProductName =
    # "Saints Row: The Third Remastered", while the folder is just "sr3rmx".
    for pe_name in _find_exe_pe_names(folder):
        add(pe_name)

    # From folder name
    folder_name = folder.name
    add(folder_name)
    cleaned = re.sub(r"[_-]", " ", folder_name).strip()
    if cleaned != folder_name:
        add(cleaned)

    # Add CamelCase/PascalCase split variants of the folder name
    _add_spaced_variants(folder_name, seen, candidates)

    # From PE executables
    for exe_name in _find_exe_stem_names(folder):
        add(exe_name)
        _add_spaced_variants(exe_name, seen, candidates)

    # From detect result entry
    if entry:
        for key in ("name", "folder"):
            val = entry.get(key, "")
            if val:
                add(val)

    return candidates


# Page titles containing these substrings are unlikely to be actual games
_PAGE_TITLE_NOISE = (
    "digital book", "soundtrack", "demo", "benchmark",
    "tool", "sdk", "editor", "launcher", "art book",
    "artbook", "bundle", "beta", "prototype", "original soundtrack",
    "comic", "comic book", "making of", "behind the scenes",
    "strategy guide", "manual", "instruction manual",
    "trailer", "teaser", "wallpaper", "avatar",
)

# ── Multi-source lookup pipeline ──────────────────────────────


def lookup_game(
    folder: Path,
    store: str | None,
    identifiers: dict,
    name_candidates: list[str],
    dry_run: bool = False,
) -> dict:
    """Run the full metadata lookup pipeline for one game.

    Pipeline:
        1. Store-specific ID → Cargo query
        2. Name candidates → OpenSearch → Cargo or Parse
        3. PE metadata scan

    Args:
        folder: The game folder path (for PE scan).
        store: The detected store (``None`` if unrecognised).
        identifiers: Store-specific IDs.
        name_candidates: Ordered list of name guesses.
        dry_run: If ``True``, log actions without calling APIs.

    Returns:
        Metadata result dict (see module docstring).
    """
    result: dict = {
        "identifiers": identifiers,
        "name_candidates": name_candidates,
        "pcgw": None,
        "pe_metadata": None,
    }

    # ── Step 1: Try store-specific ID → Cargo ──────────────
    id_to_field = {
        "steam_appid": "Steam_AppID",
        "gog_game_id": "GOGcom_ID",
        "gog_root_game_id": "GOGcom_ID",
    }
    for id_key, cargo_field in id_to_field.items():
        id_val = identifiers.get(id_key, "")
        if id_val:
            if dry_run:
                _log(
                    "  [dry-run] cargo_lookup_by_id(%s, %s) → skip",
                    cargo_field,
                    id_val,
                )
                continue
            _log(
                "  Querying PCGW Cargo by %s = %s ...", cargo_field, id_val
            )
            metadata = cargo_lookup_by_id(cargo_field, id_val)
            if metadata:
                _log("  ✓ Found via Cargo (%s)", cargo_field)
                result["pcgw"] = {
                    "source": "cargo",
                    "metadata": metadata,
                }
                return _finalise(result, folder)

    # ── Step 1b: Epic-specific name resolution ──────────────
    # Epic games have no Cargo field for their store ID, so we try
    # to resolve the internal IDs to a human-readable name and
    # also capture publisher/slug/year for PCGW verification.
    epic_meta: dict | None = None
    if store == "Epic" and not result.get("pcgw"):
        epic_meta = epic_resolve_metadata(folder, identifiers)
        if epic_meta:
            epic_title = epic_meta.get("title", "")
            # Normalize before adding to candidates (strip ™®©, collapse spaces)
            epic_normalised = _normalise_name(epic_title)
            name_candidates = [epic_normalised] + name_candidates
            result["name_candidates"] = name_candidates
            result["epic_metadata"] = epic_meta
            if dry_run:
                _log('  [dry-run] epic_resolve_metadata → "%s"', epic_meta.get("title", ""))
            else:
                dev = epic_meta.get("developer", "")
                pub = epic_meta.get("publisher", "")
                cover = epic_meta.get("cover_url", "")
                _log('  Epic metadata: title="%s"  developer="%s"  publisher="%s"  slug="%s"  year=%d',
                     epic_meta.get("title", ""),
                     dev if dev else "(none)",
                     pub if pub else "(none)",
                     epic_meta.get("slug", ""),
                     epic_meta.get("release_year", 0))
                if epic_meta.get("description"):
                    _log('    description: %s…', epic_meta["description"][:80])
                if cover:
                    _log('    cover: %s', cover[:100])
        elif dry_run:
            _log('  [dry-run] epic_resolve_metadata → no match')

    # ── Step 2: Name candidate → OpenSearch → Cargo/Parse ──
    for candidate in name_candidates:
        if dry_run:
            _log('  [dry-run] opensearch("%s") → skip', candidate)
            continue
        _log('  Searching PCGW OpenSearch: "%s" ...', candidate)
        pages = opensearch_find_pages(candidate)
        if not pages:
            _log('  → no results')
            continue
        _log('  → Found %d candidate page(s)', len(pages))

        # Collect metadata from all pages to find the best match
        page_metadata: list[tuple[dict, str, str]] = []  # (metadata, source, page_title)

        for page in pages:
            metadata = cargo_lookup_by_page(page["title"])
            if metadata:
                page_metadata.append((metadata, "cargo", page["title"], page["url"]))
            else:
                infobox = parse_infobox(page["title"])
                if infobox:
                    page_metadata.append((infobox, "parse", page["title"], page["url"]))

        if not page_metadata:
            _log('  → No infobox data on any page for "%s"', candidate)
            continue

        # If only one page has infobox data, use it
        if len(page_metadata) == 1:
            md, src, ptitle, purl = page_metadata[0]
            result["pcgw"] = {
                "page_title": ptitle,
                "page_url": purl,
                "source": src,
                "metadata": md,
            }
            _log('  ✓ Only match: "%s" (%s)', ptitle, src)
            break

        # Multiple pages have infobox data — score to find best match
        candidate_lower = candidate.lower().strip()
        best: tuple[int, int, dict, str, str, str] | None = None

        # Gather extra context for cross-verification
        epic_publisher = (epic_meta or {}).get("publisher", "").lower().strip()
        epic_developer = (epic_meta or {}).get("developer", "").lower().strip()
        epic_release_year = (epic_meta or {}).get("release_year", 0)
        exe_year = _folder_timestamp_year(folder)
        pe_company = ""
        pe_data = scan_pe_metadata(folder)
        if pe_data:
            pe_company = (pe_data.get("CompanyName") or "").lower().strip()

        def _extract_year(title: str) -> int:
            """Extract a 4-digit year from a parenthetical in the title, e.g. (2013)."""
            m = re.search(r'\((\d{4})\)', title)
            return int(m.group(1)) if m else 0

        for md, src, ptitle, purl in page_metadata:
            ptitle_lower = ptitle.lower().strip()
            score = 0

            # ═══ Title-noise penalty ═══
            if any(noise in ptitle_lower for noise in _PAGE_TITLE_NOISE):
                score -= 50

            # ═══ Name similarity ═══
            if ptitle_lower == candidate_lower:
                score += 30
            else:
                base_name = re.split(r"\s*[\(–\-:]", ptitle_lower, maxsplit=1)[0].strip()
                base_name = base_name.rstrip("™©®")
                if base_name == candidate_lower:
                    score += 25
                elif base_name.startswith(candidate_lower) or candidate_lower.startswith(base_name):
                    score += 18
                elif candidate_lower in base_name:
                    score += 12
                elif candidate_lower in ptitle_lower or ptitle_lower in candidate_lower:
                    score += 6

            # ═══ Store-ID match ═══
            for id_key, cargo_field in id_to_field.items():
                cargo_key = cargo_field.replace("_", " ")
                ids_meta = str(md.get(cargo_field, md.get(cargo_key, "")))
                ids_ident = str(identifiers.get(id_key, ""))
                if ids_ident and ids_ident in ids_meta:
                    score += 25

            # ═══ Epic publisher/developer → PCGW developer cross-check ═══
            # Epic's ``publisherDisplayName`` (e.g. "Crystal Dynamics" for
            # Tomb Raider 2013) and ``developerName`` (e.g. "Sperasoft" for
            # Saints Row Remastered) reliably identify the correct PCGW page
            # when multiple entries share a name.
            for epic_name, label in ((epic_publisher, "publisher"), (epic_developer, "developer")):
                if epic_name:
                    pcgw_devs = (md.get("developers") or "").lower()
                    if epic_name in pcgw_devs or any(
                        d.strip() in epic_name for d in pcgw_devs.split(",")
                    ):
                        score += 30
                        _log('      ✓ Epic %s "%s" matches PCGW developer', label, epic_name)

            # ═══ PE CompanyName → PCGW developer cross-check ═══
            if pe_company:
                pcgw_devs = (md.get("developers") or "").lower()
                if pe_company in pcgw_devs or any(d.strip() in pe_company for d in pcgw_devs.split(",")):
                    score += 20
                    _log('      ✓ PE CompanyName "%s" matches PCGW developer', pe_company)

            # ═══ Release year proximity ═══
            pcgw_year = _extract_year(ptitle)
            if pcgw_year and epic_release_year and abs(pcgw_year - epic_release_year) <= 1:
                score += 10
            if pcgw_year and exe_year and abs(pcgw_year - exe_year) <= 1:
                score += 8

            # ═══ Content quality ═══
            has_dev = bool(md.get("developers"))
            has_genre = bool(md.get("genres") or md.get("Genres"))
            has_date = bool(md.get("release_date") or md.get("Released"))
            if has_dev:
                score += 5
            if has_genre:
                score += 3
            if has_date:
                score += 3

            # Tiebreaker: prefer pages with a recognized year in the title
            tiebreaker = _extract_year(ptitle)

            if best is None or (score, tiebreaker) > (best[0], best[1]):
                best = (score, tiebreaker, md, src, ptitle, purl)

        if best:
            score, tiebreaker, md, src, ptitle, purl = best
            result["pcgw"] = {
                "page_title": ptitle,
                "page_url": purl,
                "source": src,
                "metadata": md,
            }
            _log('  ✓ Best match: "%s" (score=%d, year=%d, source=%s)', ptitle, score, tiebreaker, src)
            break
        else:
            _log('  → No suitable match among %d candidate(s)', len(page_metadata))

    # ── Step 3: PE metadata scan (always, no rate limit) ──
    return _finalise(result, folder)


def _finalise(result: dict, folder: Path) -> dict:
    """Run PE scan and set confidence level."""
    pe = scan_pe_metadata(folder)
    if pe:
        result["pe_metadata"] = pe

    # Confidence
    pcgw = result.get("pcgw")
    if pcgw and pcgw.get("metadata"):
        has_ident = any(
            k in result.get("identifiers", {})
            for k in ("steam_appid", "gog_game_id", "gog_root_game_id")
        )
        has_dev = bool(
            pcgw.get("metadata", {}).get("developers")
            or pcgw.get("metadata", {}).get("Developers")
        )
        if has_ident and has_dev:
            result["confidence"] = "high"
        elif has_dev or pcgw.get("source") == "cargo":
            result["confidence"] = "medium"
        else:
            result["confidence"] = "low"
    elif pe:
        result["confidence"] = "low"
    else:
        result["confidence"] = "none"

    return result


# ── Batch processing ──────────────────────────────────────────


def process_single_folder(folder_path: str, dry_run: bool = False) -> dict:
    """Detect store, extract IDs, look up metadata for one folder.

    Returns a keyed dict ``{ folder_name: result }``.
    """
    folder = Path(folder_path).resolve()
    if not folder.is_dir():
        _log("Error: folder not found: %s", folder)
        return {}

    # Reuse detect_folder logic via a simplified inline detect
    store, signal, entry = _detect_and_build_entry(folder)
    identifiers = extract_identifiers(store, folder) if store else {}
    name_candidates = build_name_candidates(folder, entry, identifiers)
    result = lookup_game(folder, store, identifiers, name_candidates, dry_run=dry_run)

    result["folder"] = folder.name
    result["store"] = store
    result["signal"] = signal

    return {folder.name: result}


def process_batch(
    detect_file: str, dry_run: bool = False
) -> dict:
    """Process a detect_folder.py JSON output file.

    For each recognised entry, extract identifiers from the actual
    folder, look up metadata, and return an enriched result dict.
    """
    path = Path(detect_file)
    try:
        detect_data = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        _log("Error reading detection file: %s", exc)
        return {}

    base_dir = Path(detect_data.get("input", "."))
    results: dict = {}

    for entry in detect_data.get("recognized", []):
        folder_name = entry.get("folder", "")
        folder_path = base_dir / folder_name
        if not folder_path.is_dir():
            # Maybe the entry already has an absolute-like path
            folder_path = Path(folder_name)
            if not folder_path.is_dir():
                _log("Warning: folder not found: %s", folder_name)
                continue

        store = entry.get("store", "")
        identifiers = extract_identifiers(store, folder_path) if store else {}
        name_candidates = build_name_candidates(folder_path, entry, identifiers)
        result = lookup_game(
            folder_path, store, identifiers, name_candidates, dry_run=dry_run
        )

        result["folder"] = folder_name
        result["store"] = store
        result["signal"] = entry.get("signal")
        # Preserve the original detection engine guess
        result["engine_detected"] = entry.get("engine")

        results[folder_name] = result

    return results


# ── Simplified inline detection ───────────────────────────────
# (Minimal duplication of detect_folder.py for standalone mode.)


def _detect_and_build_entry(folder: Path):
    """Run a minimal detection pass and return (store, signal, entry_dict)."""
    checks = [
        ("GOG", "goggame", lambda d: bool(list(d.glob("goggame*")))),
        ("EA", "ea_installer", lambda d: (d / "__Installer").is_dir()),
        (
            "Ubisoft",
            "uplay",
            lambda d: (d / "uplay_install.manifest").exists()
            or bool(list(d.glob("uplay_r*_loader*.dll"))),
        ),
        ("Epic", "egstore", lambda d: (d / ".egstore").is_dir() or (d / ".egsstore").is_dir()),
        ("Blizzard", "battle_net", lambda d: (d / ".battle.net").is_dir()),
        ("Xbox", "default_metadata", lambda d: (d / "default-metadata.json").exists()),
        (
            "Steam Emulator",
            "steam_api",
            lambda d: (d / "steam_api64.dll").exists() or (d / "steam_api.dll").exists(),
        ),
        ("Rockstar", "rgl", lambda d: (d / "title.rgl").exists()),
    ]

    for store, signal, check in checks:
        if check(folder):
            return store, signal, {"name": folder.name}

    # Deep checks
    if _has_steam_emu_ini(folder):
        return "Steam Emulator", "emu_ini", {}
    if _has_unreal_game_layout(folder):
        return "Standalone", "unreal_binaries", {}
    if _has_root_exe(folder):
        return "Standalone", "root_exe", {}

    return None, None, {}


def _has_steam_emu_ini(d: Path) -> bool:
    if (d / "steam_emu.ini").exists():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "steam_emu.ini").exists():
            return True
    return False


def _has_unreal_game_layout(d: Path) -> bool:
    if not (d / "Engine").is_dir():
        return False
    for child in d.iterdir():
        if not child.is_dir() or child.name == "Engine":
            continue
        binaries = child / "Binaries" / "Win64"
        if binaries.is_dir():
            return True
    return False


def _has_root_exe(d: Path) -> bool:
    for child in d.iterdir():
        if child.is_file() and child.suffix.lower() == ".exe":
            if not any(n in child.name.lower() for n in NOISE_EXE_PARTS):
                return True
    return False


# ── CLI ───────────────────────────────────────────────────────


def main():
    import argparse

    parser = argparse.ArgumentParser(
        description="Look up structured game metadata using local signals + PCGamingWiki."
    )
    parser.add_argument(
        "--folder",
        help="Single game folder to detect and look up",
    )
    parser.add_argument(
        "--input",
        help="Path to detect_folder.py JSON output file (batch mode)",
    )
    parser.add_argument(
        "--output",
        help="Write results to this JSON file (default: stdout)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Log actions without making API calls",
    )
    parser.add_argument(
        "--epic-manifests-dir",
        default=None,
        help="Path to Epic Games Launcher Manifests/ directory "
        "(Windows: %%ProgramData%%\\Epic\\EpicGamesLauncher\\Data\\Manifests\\)",
    )

    args = parser.parse_args()

    # Set global Epic manifests dir
    global EPIC_MANIFESTS_DIR
    EPIC_MANIFESTS_DIR = args.epic_manifests_dir or os.environ.get("EPIC_MANIFESTS_DIR")

    if args.dry_run:
        _log("[DRY RUN mode — no API calls will be made]")

    if args.folder:
        results = process_single_folder(args.folder, dry_run=args.dry_run)
    elif args.input:
        results = process_batch(args.input, dry_run=args.dry_run)
    else:
        parser.print_help()
        sys.exit(1)

    output = json.dumps(results, indent=2, ensure_ascii=False)
    if args.output:
        Path(args.output).write_text(output, encoding="utf-8")
        _log("Wrote %d game(s) to %s", len(results), args.output)
    else:
        print(output)

    if results:
        _log("Done. %d game(s) processed.", len(results))
    else:
        _log("No results.")


if __name__ == "__main__":
    main()
