#!/usr/bin/env python3
"""Fast Epic Games .manifest decoder with .item generation."""

import struct, zlib, sys, json, os, re, requests


def read_fstrings(data, off, count):
    strings = []
    for _ in range(count):
        size = struct.unpack('<I', data[off:off+4])[0]
        if size == 0:
            off += 4
            continue
        s = data[off+4:off+4+size-1].decode('utf-8', errors='replace')
        strings.append(s)
        off += 4 + size
    return strings, off


def folder_leaf(path):
    """Last path segment; works for Windows paths on Linux."""
    p = path.replace("\\", "/").rstrip("/")
    return p.split("/")[-1] if p else path


def read_local_catalog_ids(game_folder):
    """Prefer .mancpn, then .ovt JWT. These belong to THIS install — do not replace with GraphQL."""
    import glob
    for pat in (os.path.join(game_folder, ".egstore", "*.mancpn"),
                os.path.join(game_folder, ".egsstore", "*.mancpn")):
        for f in glob.glob(pat):
            try:
                d = json.load(open(f, encoding="utf-8"))
                ns, cid, app = d.get("CatalogNamespace"), d.get("CatalogItemId"), d.get("AppName")
                if ns and cid:
                    return {"namespace": ns, "catalog_id": cid, "app_name": app or "", "source": "mancpn"}
            except Exception:
                pass
    for root, _, files in os.walk(os.path.join(game_folder, ".egstore")):
        for name in files:
            if not name.endswith(".ovt"):
                continue
            try:
                tok = json.load(open(os.path.join(root, name), encoding="utf-8")).get("token") or ""
                if tok.startswith("egoc1~"):
                    tok = tok[6:]
                payload = tok.split(".")[1]
                payload += "=" * ((4 - len(payload) % 4) % 4)
                import base64
                jwt = json.loads(base64.urlsafe_b64decode(payload.replace("-", "+").replace("_", "/")))
                ent = (jwt.get("ent") or [{}])[0]
                ns, cid = ent.get("namespace"), ent.get("catalogItemId")
                app = jwt.get("sub") or os.path.basename(root)
                if ns and cid:
                    return {"namespace": ns, "catalog_id": cid, "app_name": app or "", "source": "ovt"}
            except Exception:
                pass
    return {}


def normalize_app_name(app_name):
    """Strip 'Staging' suffix from AppName for .item format."""
    if app_name.lower().endswith('staging'):
        return app_name[:-7] if app_name[-7:] == 'Staging' else app_name[:-7].lower()
    return app_name


def normalize_path(path):
    """Convert Windows path to EGS format: lowercase with forward slashes."""
    return path.lower().replace('\\', '/')


def extract_game_name(manifest_data):
    app_name = manifest_data['metadata']['app_name']
    launch_exe = manifest_data['metadata']['launch_exe']
    exe_name = os.path.splitext(os.path.basename(launch_exe))[0]
    exe_name = re.sub(r'Game(_x64|_rwdi)?$', '', exe_name)
    exe_name = re.sub(r'([a-z])([A-Z])', r'\1 \2', exe_name)
    exe_name = re.sub(r'([a-zA-Z])(\d)', r'\1 \2', exe_name)
    if exe_name.strip():
        return exe_name.strip()
    name = app_name
    if name.lower().endswith('staging'):
        name = name[:-7]
    return name


def search_epic_by_ids(namespace, catalog_id):
    """searchStore(namespace=...) then pick the offer whose id == catalog_id.

    IDs stay as passed in. This only returns display_name / thumbnail.
    Dev namespaces may return nothing useful.
    """
    if not namespace:
        return {}
    endpoint = "https://store.epicgames.com/graphql"
    headers = {
        "Content-Type": "application/json",
        "Accept": "*/*",
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    }
    q = (
        '{ Catalog { searchStore(start: 0, count: 10, namespace: "%s") '
        "{ elements { title id namespace keyImages { url type } } } } }"
        % namespace
    )
    try:
        response = requests.post(endpoint, json={"query": q}, headers=headers, timeout=15)
        if response.status_code != 200:
            return {}
        els = (
            response.json()
            .get("data", {})
            .get("Catalog", {})
            .get("searchStore", {})
            .get("elements")
            or []
        )
        picked = None
        for r in els:
            if catalog_id and r.get("id") == catalog_id:
                picked = r
                break
        if picked is None and els:
            picked = els[0]
        if not picked:
            return {}
        thumb = ""
        for img in picked.get("keyImages") or []:
            if img.get("type") == "Thumbnail":
                thumb = img.get("url") or ""
                break
        return {
            "display_name": picked.get("title") or "",
            "vault_thumbnail": thumb,
            "source": "namespace+id" if catalog_id and picked.get("id") == catalog_id else "namespace",
        }
    except Exception as e:
        print(f"Warning: Epic API id lookup failed: {e}", file=sys.stderr)
        return {}


def search_epic_namespace(query):
    """Query Epic API to find namespace, catalog item ID, and display metadata for a game."""
    
    endpoint = "https://store.epicgames.com/graphql"
    headers = {
        "Content-Type": "application/json",
        "Accept": "*/*",
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
    }
    
    q = """
    {
        Catalog {
            searchStore(start: 0, count: 1, keywords: "%s") {
                elements {
                    title
                    id
                    namespace
                    productSlug
                    keyImages { url type }
                    developerDisplayName
                    publisherDisplayName
                    releaseDate
                }
            }
        }
    }
    """ % query
    
    try:
        response = requests.post(endpoint, json={"query": q}, headers=headers, timeout=15)
        
        if response.status_code == 200:
            data = response.json()
            results = data.get('data', {}).get('Catalog', {}).get('searchStore', {}).get('elements', [])
            
            if results:
                r = results[0]
                # Extract thumbnail URL
                thumb_url = ""
                for img in r.get('keyImages', []):
                    if img.get('type') == 'Thumbnail':
                        thumb_url = img.get('url', '')
                        break
                
                return {
                    'namespace': r.get('namespace', ''),
                    'catalog_id': r.get('id', ''),
                    'display_name': r.get('title', ''),
                    'vault_thumbnail': thumb_url,
                    'developer': r.get('developerDisplayName', ''),
                    'publisher': r.get('publisherDisplayName', ''),
                    'release_date': r.get('releaseDate', ''),
                }
    except Exception as e:
        print(f"Warning: Epic API query failed: {e}", file=sys.stderr)
    
    return {}


def normalize_path(path):
    p = path.replace('/', '\\')
    if len(p) > 1 and p[1] == ':':
        return p[0].lower() + p[2:]
    return p


def generate_item(manifest_path, manifest_data, install_location, epic_meta=None):
    if epic_meta is None:
        epic_meta = {}
    
    meta = manifest_data['metadata']
    manifest_folder = os.path.dirname(manifest_path)
    if not install_location:
        install_location = os.path.dirname(manifest_folder)
    folder_name = folder_leaf(install_location)
    
    display_name = epic_meta.get('display_name') or folder_name
    vault_thumb = epic_meta.get('vault_thumbnail', '')
    
    # Shape confirmed 2026-08-23: user edit of our regen showed as Update in Epic.
    # AppName = Boga from .manifest (not .ovt sub). No AppVersionString. No MainGame*.
    # Catalog ids from local .ovt/.mancpn. Epic schema extras present but empty.
    loc = install_location.replace('/', '\\')
    item = {
        "FormatVersion": 0,
        "EoshRevision": "",
        "bIsIncompleteInstall": False,
        "LaunchCommand": "",
        "LaunchExecutable": meta['launch_exe'],
        "ManifestLocation": loc + "/.egstore",
        "CompleteManifestPath": "",
        "PendingManifestPath": "",
        "ManifestHash": "",
        "SDMetaHash": "",
        "SDMetaLocation": "",
        "bIsApplication": True,
        "bIsExecutable": True,
        "bIsManaged": False,
        "bNeedsValidation": False,
        "bSDMetaMigrated": False,
        "bRequiresAuth": True,
        "bAllowMultipleInstances": False,
        "bCanRunOffline": True,
        "bAllowUriCmdArgs": False,
        "bLaunchElevated": False,
        "BaseURLs": [],
        "BuildLabel": "Live",
        "AppCategories": ["public", "games", "applications"],
        "ChunkDbs": [],
        "CompatibleApps": [],
        "DisplayName": display_name,
        "InstallationGuid": os.path.splitext(os.path.basename(manifest_path))[0],
        "InstallLocation": loc,
        "InstallSessionId": "00000000000000000000000000000000",
        "InstallTags": [],
        "InstallComponents": [],
        "HostInstallationGuid": "00000000000000000000000000000000",
        "PrereqSHA1Hash": "",
        "LastPrereqSucceededSHA1Hash": "",
        "StagingLocation": loc + "\\.egstore\\bps",
        "TechnicalType": "public,games,applications",
        "VaultThumbnailUrl": vault_thumb,
        "VaultTitleText": "",
        "InstallSize": 0,
        "MainWindowProcessName": "",
        "ProcessNames": [],
        "BackgroundProcessNames": [],
        "IgnoredProcessNames": [],
        "DlcProcessNames": [],
        "MandatoryAppFolderName": folder_name,
        "OwnershipToken": "true",
        "SidecarConfigRevision": 0,
        "SidecarDeploymentId": "",
        "PreloadState": 0,
        "CatalogNamespace": epic_meta.get('namespace', ''),
        "CatalogItemId": epic_meta.get('catalog_id', ''),
        "AppName": normalize_app_name(meta['app_name']),
        "AllowedUriEnvVars": [],
    }
    
    return item


def read_fstrings(data, off, count):
    strings = []
    for _ in range(count):
        size = struct.unpack('<I', data[off:off+4])[0]
        if size == 0:
            off += 4
            continue
        s = data[off+4:off+4+size-1].decode('utf-8', errors='replace')
        strings.append(s)
        off += 4 + size
    return strings, off


def parse(path):
    with open(path, 'rb') as f:
        raw = f.read()
    
    res = {'file': path}
    res['magic'] = f"0x{struct.unpack('<I', raw[0:4])[0]:08X}"
    res['stored_as'] = raw[36]
    res['version'] = struct.unpack('<I', raw[37:41])[0]
    
    body = zlib.decompress(raw[41:]) if res['stored_as'] & 1 else raw[41:]
    res['body_size'] = len(body)
    
    meta_data_size = struct.unpack('<I', body[0:4])[0]
    data_version = body[4]
    feature_level = struct.unpack('<i', body[5:9])[0]
    is_file_data = body[9]
    app_id = struct.unpack('<i', body[10:14])[0]
    
    off = 14
    app_name_len = struct.unpack('<I', body[off:off+4])[0]
    res['app_name'] = body[off+4:off+4+app_name_len-1].decode('utf-8', errors='replace')
    off += 4 + app_name_len
    
    build_ver_len = struct.unpack('<I', body[off:off+4])[0]
    res['build_version'] = body[off+4:off+4+build_ver_len-1].decode('utf-8', errors='replace')
    off += 4 + build_ver_len
    
    launch_exe_len = struct.unpack('<I', body[off:off+4])[0]
    res['launch_exe'] = body[off+4:off+4+launch_exe_len-1].decode('utf-8', errors='replace')
    off += 4 + launch_exe_len
    
    launch_cmd_len = struct.unpack('<I', body[off:off+4])[0]
    off += 4 + launch_cmd_len
    
    prereq_count = struct.unpack('<I', body[off:off+4])[0]
    off += 4
    for _ in range(prereq_count):
        s = struct.unpack('<I', body[off:off+4])[0]
        off += 4 + s
    
    for _ in range(3):  # prereq_name, path, args
        s = struct.unpack('<I', body[off:off+4])[0]
        off += 4 + s
    
    if data_version >= 1:
        s = struct.unpack('<I', body[off:off+4])[0]
        off += 4 + s
    
    res['metadata'] = {'app_name': res['app_name'], 'build_version': res['build_version'], 'launch_exe': res['launch_exe']}
    
# Chunks with bounds check
    chunk_off = meta_data_size
    if chunk_off + 9 > len(body):
        res['chunks'] = []
    else:
        chunk_count = struct.unpack('<I', body[chunk_off+5:chunk_off+9])[0]
        
        chunks = []
        c_off = chunk_off + 9
        chunk_size_bytes = 48
        for _ in range(chunk_count):
            if c_off + chunk_size_bytes > len(body):
                break
            guid = f"{body[c_off:c_off+4].hex()}-{body[c_off+4:c_off+6].hex()}-{body[c_off+6:c_off+8].hex()}-{body[c_off+8:c_off+10].hex()}-{body[c_off+10:c_off+16].hex()}"
            file_size = struct.unpack('<Q', body[c_off+44:c_off+52])[0]
            chunks.append({'guid': guid, 'file_size': file_size})
            c_off += chunk_size_bytes
        
        res['chunks'] = chunks
    
    res['chunks'] = chunks
    
    # Files - check bounds first
    file_off = meta_data_size + struct.unpack('<I', body[chunk_off:chunk_off+4])[0]
    if file_off + 9 > len(body):
        res['files'] = []
        res['chunks'] = []
        return res
    
    file_count = struct.unpack('<I', body[file_off+5:file_off+9])[0]
    file_off += 9
    
    # File list - compute offset from chunk list
    chunk_data_size = struct.unpack('<I', body[chunk_off:chunk_off+4])[0]
    file_off = meta_data_size + chunk_data_size + 9  # +9 to skip file list header
    
    file_count = struct.unpack('<I', body[chunk_off+5:chunk_off+9])[0]
    
    # Read all file names
    file_names = []
    for _ in range(file_count):
        if file_off + 4 > len(body):
            break
        size = struct.unpack('<I', body[file_off:file_off+4])[0]
        if size > 0 and file_off + 4 + size <= len(body):
            name = body[file_off+4:file_off+4+size-1].decode('utf-8', errors='replace')
            file_names.append(name)
        else:
            file_names.append('')
        file_off += 4 + size
    
    res['files'] = [{'file_name': n} for n in file_names]
    
    return res


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: decode_manifest.py <manifest.manifest> [--item <install_path>] [--game <name>]")
        print("  --item <path>   Generate .item file for given install location")
        print("  --game <name>  Query Epic API to auto-discover CatalogNamespace/CatalogItemId")
        sys.exit(1)
    
    manifest_path = sys.argv[1]
    result = parse(manifest_path)
    
    install_location = None
    game_query = None
    
    for i, arg in enumerate(sys.argv):
        if arg == '--item' and i + 1 < len(sys.argv):
            install_location = sys.argv[i + 1]
        if arg == '--game' and i + 1 < len(sys.argv):
            game_query = sys.argv[i + 1]
    
    if not game_query:
        if install_location:
            folder_name = os.path.basename(install_location.rstrip('/\\'))
            game_query = re.sub(r'([a-zA-Z])(\d)', r'\1 \2', folder_name)
            game_query = re.sub(r'([a-z])([A-Z])', r'\1 \2', game_query)
        else:
            game_query = extract_game_name(result)
        print(f"Extracted game name: {game_query}", file=sys.stderr)
    
    local_ids = {}
    if install_location:
        local_ids = read_local_catalog_ids(install_location)
        if not local_ids and len(install_location) >= 3 and install_location[1] == ":":
            mapped = "/mnt/" + install_location[0].lower() + install_location[2:].replace("\\", "/")
            local_ids = read_local_catalog_ids(mapped)

    epic_meta = {}
    if local_ids:
        epic_meta = dict(local_ids)
        print(
            f"Using local {local_ids.get('source')} ids: "
            f"Namespace={local_ids.get('namespace')}, CatalogItemId={local_ids.get('catalog_id')}",
            file=sys.stderr,
        )
        if game_query:
            epic_meta["display_name"] = game_query
        # Enrich title/art only. Never replace local namespace / catalog id.
        extra = search_epic_by_ids(local_ids.get("namespace"), local_ids.get("catalog_id"))
        extra_name = extra.get("display_name") or ""
        want = (epic_meta.get("display_name") or game_query or "").lower()
        if extra_name and want and want.split()[0] in extra_name.lower():
            epic_meta["display_name"] = extra_name
        if extra.get("vault_thumbnail") and extra_name and want and want.split()[0] in extra_name.lower():
            epic_meta["vault_thumbnail"] = extra["vault_thumbnail"]
        print(
            f"GraphQL enrich ({extra.get('source', 'none')}): {extra_name or '(no title)'} "
            f"(ids unchanged; display kept if title does not match --game)",
            file=sys.stderr,
        )
    elif game_query:
        print(f"Querying Epic API for: {game_query}", file=sys.stderr)
        epic_meta = search_epic_namespace(game_query)
        if epic_meta.get('namespace') and epic_meta.get('catalog_id'):
            print(f"Found: Namespace={epic_meta['namespace']}, CatalogItemId={epic_meta['catalog_id']}", file=sys.stderr)
            print(f"DisplayName: {epic_meta.get('display_name', 'N/A')}", file=sys.stderr)
            print(f"VaultThumbnail: {'Yes' if epic_meta.get('vault_thumbnail') else 'No'}", file=sys.stderr)
        else:
            print("Could not find game in Epic store", file=sys.stderr)
            epic_meta = {}
    
    if install_location:
        item = generate_item(manifest_path, result, install_location, epic_meta)
        print(json.dumps(item, indent=2))
    else:
        install_location = os.path.dirname(os.path.dirname(manifest_path))
        item = generate_item(manifest_path, result, install_location, epic_meta)
        print(json.dumps(item, indent=2))
        print(f"Magic: {result['magic']}", file=sys.stderr)
        print(f"App: {result['metadata']['app_name']}", file=sys.stderr)
        print(f"App (normalized): {normalize_app_name(result['metadata']['app_name'])}", file=sys.stderr)
        print(f"Build: {result['metadata']['build_version']}", file=sys.stderr)
        print(f"Launch: {result['metadata']['launch_exe']}", file=sys.stderr)
        print(f"Version: {result['version']}", file=sys.stderr)
        print(f"Chunks: {len(result['chunks'])}", file=sys.stderr)
        print(f"Files: {len(result['files'])}", file=sys.stderr)
        print(file=sys.stderr)
        print("=== FIRST 10 FILES ===", file=sys.stderr)
        for f in result['files'][:10]:
            print(f"  {f['file_name']}", file=sys.stderr)
        print(file=sys.stderr)
        print("=== FIRST 10 CHUNKS ===", file=sys.stderr)
        for c in result['chunks'][:10]:
            print(f"  {c['guid']} - {c['file_size']:,} bytes", file=sys.stderr)