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
    folder_name = os.path.basename(install_location.rstrip('/\\'))
    
    display_name = epic_meta.get('display_name', folder_name)
    vault_thumb = epic_meta.get('vault_thumbnail', '')
    folder_name = os.path.basename(install_location.rstrip('/\\'))
    
    item = {
        "FormatVersion": 0,
        "bIsIncompleteInstall": False,
        "LaunchCommand": "",
        "LaunchExecutable": meta['launch_exe'],
        "ManifestLocation": install_location.replace('/', '\\') + "/.egstore",
        "ManifestHash": "",  # SHA of manifest file - cannot derive
        "bIsApplication": True,
        "bIsExecutable": True,
        "bIsManaged": False,
        "bNeedsValidation": False,
        "bRequiresAuth": True,
        "bAllowMultipleInstances": False,
        "bCanRunOffline": True,
        "bAllowUriCmdArgs": False,
        "bLaunchElevated": False,
        "BaseURLs": [],  # CDN URLs - cannot derive
        "BuildLabel": "Live",
        "AppCategories": ["public", "games", "applications"],
        "ChunkDbs": [],
        "CompatibleApps": [],
        "DisplayName": display_name,
        "InstallationGuid": os.path.splitext(os.path.basename(manifest_path))[0],
        "InstallLocation": install_location.replace('/', '\\'),
        "InstallSessionId": "",
        "InstallTags": [],
        "InstallComponents": [],
        "HostInstallationGuid": "00000000000000000000000000000000",
        "PrereqIds": [],
        "PrereqSHA1Hash": "",
        "LastPrereqSucceededSHA1Hash": "",
        "StagingLocation": install_location.replace('/', '\\') + '\\.egstore\\bps',
        "TechnicalType": "public,games,applications",
        "VaultThumbnailUrl": vault_thumb,
        "VaultTitleText": "",
        "InstallSize": 0,
        "MainWindowProcessName": "",
        "ProcessNames": [],
        "BackgroundProcessNames": [],
        "IgnoredProcessNames": [],
        "DlcProcessNames": [],
        "ExpectingDLCInstalled": {},
        "MandatoryAppFolderName": folder_name,
        "OwnershipToken": "true",
        "SidecarConfigRevision": 0,
        "PreloadState": 0,
        "CatalogNamespace": epic_meta.get('namespace', ''),
        "CatalogItemId": epic_meta.get('catalog_id', ''),
        "AppName": normalize_app_name(meta['app_name']),
        "AppVersionString": meta['build_version'],
        "MainGameCatalogNamespace": epic_meta.get('namespace', ''),
        "MainGameCatalogItemId": epic_meta.get('catalog_id', ''),
        "MainGameAppName": normalize_app_name(meta['app_name']),
        "AllowedUriEnvVars": []
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
    
    if game_query:
        print(f"Querying Epic API for: {game_query}", file=sys.stderr)
        epic_meta = search_epic_namespace(game_query)
        if epic_meta.get('namespace') and epic_meta.get('catalog_id'):
            print(f"Found: Namespace={epic_meta['namespace']}, CatalogItemId={epic_meta['catalog_id']}", file=sys.stderr)
            print(f"DisplayName: {epic_meta.get('display_name', 'N/A')}", file=sys.stderr)
            print(f"VaultThumbnail: {'Yes' if epic_meta.get('vault_thumbnail') else 'No'}", file=sys.stderr)
        else:
            print("Could not find game in Epic store", file=sys.stderr)
            epic_meta = {}
    else:
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