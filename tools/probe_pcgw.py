#!/usr/bin/env python3
"""Live probe of PCGamingWiki (Plan 102 Phase 3, priority 2).

Research-only. Public titles / Steam AppIDs only.

Cargo (Infobox_game HOLDS) is **blocked** as of 2026-08-22
(`permissiondenied` — arbitrary Cargo queries). Working path:

  OpenSearch by name  →  action=parse wikitext
  optional: /api/appid.php?appid=  → HTML title (not JSON)

  python3 tools/probe_pcgw.py
  python3 tools/probe_pcgw.py --title "Cyberpunk 2077"
  python3 tools/probe_pcgw.py --appid 1091500
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.parse
import urllib.request

PCGW_API = "https://www.pcgamingwiki.com/w/api.php"
APPID_PHP = "https://www.pcgamingwiki.com/api/appid.php"
USER_AGENT = "GamingCommander/0.1 (research; pcgw probe)"


def _get(params: dict) -> tuple[int, object | None, str | None]:
    url = PCGW_API + "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT, "Accept": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=20) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8", errors="replace")), None
    except Exception as exc:
        return 0, None, str(exc)


def cargo_by_steam_appid(appid: str) -> dict:
    status, payload, err = _get({
        "action": "cargoquery",
        "tables": "Infobox_game",
        "fields": "Developers,Publishers,Released,Genres,Steam_AppID,GOGcom_ID,Cover",
        "where": f'Infobox_game.Steam_AppID HOLDS "{appid}"',
        "format": "json",
        "limit": "1",
    })
    error = None
    if isinstance(payload, dict):
        error = payload.get("error")
    return {"httpStatus": status, "error": err or error, "ok": False}


def opensearch(title: str) -> dict:
    status, payload, err = _get({
        "action": "opensearch",
        "search": title,
        "limit": "3",
        "format": "json",
    })
    pages = []
    if isinstance(payload, list) and len(payload) >= 4:
        names, urls = payload[1], payload[3]
        pages = [{"title": n, "url": u} for n, u in zip(names, urls)]
    return {"httpStatus": status, "error": err, "ok": bool(pages), "pages": pages}


def parse_wikitext(page: str) -> dict:
    status, payload, err = _get({
        "action": "parse",
        "page": page,
        "prop": "wikitext",
        "format": "json",
    })
    if not isinstance(payload, dict) or "parse" not in payload:
        return {"httpStatus": status, "error": err or (payload or {}).get("error"), "ok": False}
    wt = payload["parse"].get("wikitext") or {}
    text = wt.get("*", "") if isinstance(wt, dict) else str(wt)
    infobox = "Infobox game" in text
    return {
        "httpStatus": status,
        "ok": infobox,
        "wikitextChars": len(text),
        "hasInfobox": infobox,
        "developers": re.findall(r"Infobox game/row/developer\|([^}|]+)", text)[:5],
    }


def appid_php(appid: str) -> dict:
    url = f"{APPID_PHP}?appid={urllib.parse.quote(appid)}"
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=20) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            title = None
            m = re.search(r"<title>([^<]+)</title>", body)
            if m:
                title = m.group(1).split(" - PCGamingWiki")[0].strip()
            return {"httpStatus": resp.status, "ok": bool(title), "htmlTitle": title, "contentType": resp.headers.get("Content-Type")}
    except Exception as exc:
        return {"httpStatus": 0, "ok": False, "error": str(exc)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--title", default="Cyberpunk 2077")
    parser.add_argument("--appid", default="1091500")
    args = parser.parse_args()

    out = {
        "cargo": cargo_by_steam_appid(args.appid),
        "appidPhp": appid_php(args.appid),
        "opensearch": opensearch(args.title),
        "parse": parse_wikitext(args.title),
    }
    json.dump(out, sys.stdout, indent=2)
    print()
    cargo_err = out["cargo"].get("error")
    print(
        f"cargo_ok={out['cargo']['ok']} cargo_err={cargo_err} "
        f"opensearch_ok={out['opensearch']['ok']} parse_ok={out['parse']['ok']}",
        file=sys.stderr,
    )
    return 0 if out["opensearch"]["ok"] and out["parse"]["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
