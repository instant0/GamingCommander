#!/usr/bin/env python3
"""Live probe of Steam Store appdetails (Plan 102 Phase 3, priority 1).

Research-only. Not shipped. Does not read the user's game library.
Uses well-known public AppIDs to confirm the endpoint, JSON shape, and
the field mapping proposed in planning/102-tags-metadata-display.md.

  python3 tools/probe_steam_store.py
  python3 tools/probe_steam_store.py --appid 1091500

No C# code is involved. Safe to run from Linux.
"""

from __future__ import annotations

import argparse
import json
import sys
import time
import urllib.error
import urllib.request

ENDPOINT = "https://store.steampowered.com/api/appdetails"
USER_AGENT = "GamingCommander/0.1 (research; steam-store probe)"
RATE_LIMIT_S = 10  # Plan 102: 1 req / 10s

# Public catalog IDs only — not derived from any local library.
DEFAULT_APPIDS = [
    "1091500",  # Cyberpunk 2077 — expected success
    "271590",   # GTA V — expected success
    "480",      # Spacewar (Valve test app)
    "1",        # invalid — expected success=false
]


def fetch(appid: str, timeout: int = 20) -> tuple[int, dict | None, str | None]:
    url = f"{ENDPOINT}?appids={urllib.request.quote(appid)}"
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT, "Accept": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            raw = resp.read().decode("utf-8", errors="replace")
            return resp.status, json.loads(raw), None
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace") if exc.fp else ""
        return exc.code, None, f"HTTP {exc.code}: {body[:200]}"
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
        return 0, None, str(exc)


def map_record(appid: str, payload: dict) -> dict:
    """Map store JSON onto the Plan 102 GameMetadataRecord fields."""
    block = payload.get(appid) or payload.get(str(appid))
    if not isinstance(block, dict):
        return {"ok": False, "reason": "missing appid key", "keys": list(payload.keys())}

    if not block.get("success"):
        return {"ok": False, "reason": "success=false", "success": block.get("success")}

    data = block.get("data") or {}
    genres = data.get("genres") or []
    genre_names = [g.get("description") for g in genres if isinstance(g, dict)]
    metacritic = data.get("metacritic") or {}
    release = data.get("release_date") or {}

    return {
        "ok": True,
        "gameEntryId": "",
        "developer": ", ".join(data.get("developers") or []) or None,
        "publisher": ", ".join(data.get("publishers") or []) or None,
        "releaseDate": release.get("date"),
        "genre": ", ".join(n for n in genre_names if n) or None,
        "description": data.get("short_description"),
        "engine": None,  # not in appdetails
        "metacriticScore": metacritic.get("score"),
        "steamAppId": str(data.get("steam_appid") or appid),
        "coverArtUrl": data.get("header_image"),
        "officialWebsite": data.get("website"),
        "lastMetadataSource": "Steam Store",
        "storeType": data.get("type"),
        "storeName": data.get("name"),
        "isFree": data.get("is_free"),
        "rawTopKeys": sorted(data.keys()),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--appid", action="append", dest="appids", help="AppID to probe (repeatable)")
    parser.add_argument("--delay", type=float, default=RATE_LIMIT_S, help="Seconds between requests")
    args = parser.parse_args()

    appids = args.appids or DEFAULT_APPIDS
    results = []

    for i, appid in enumerate(appids):
        if i:
            time.sleep(max(0.0, args.delay))
        status, payload, err = fetch(appid)
        entry = {"appid": appid, "httpStatus": status, "error": err}
        if payload is not None:
            entry["mapped"] = map_record(appid, payload)
        results.append(entry)
        mapped = entry.get("mapped") or {}
        label = mapped.get("storeName") or mapped.get("reason") or err or "?"
        print(f"appid={appid} http={status} {label}", file=sys.stderr)

    json.dump({"endpoint": ENDPOINT, "results": results}, sys.stdout, indent=2)
    print()
    any_ok = any((r.get("mapped") or {}).get("ok") for r in results)
    return 0 if any_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
