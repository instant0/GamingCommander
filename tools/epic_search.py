#!/usr/bin/env python3
"""Query Epic Games Store API to find game namespace and catalog item ID."""

import requests
import json
import sys


def search_epic_games(query):
    """Search Epic Games Store for a game."""
    
    # Try different endpoints
    endpoints = [
        "https://store.epicgames.com/graphql",
        "https://www.epicgames.com/graphql", 
        "https://graphql.epicgames.com/graphql",
    ]
    
    for url in endpoints:
        print(f"Trying: {url}")
        
        headers = {
            "Content-Type": "application/json",
            "Accept": "*/*",
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        }
        
        try:
            response = requests.post(url, json={"query": f"""
            {{
                Catalog {{
                    searchStore(start: 0, count: 10, keywords: "{query}") {{
                        elements {{
                            title
                            id
                            namespace
                            productSlug
                        }}
                    }}
                }}
            }}
            """}, headers=headers, timeout=10)
            print(f"Status: {response.status_code}")
            
            if response.status_code == 200:
                data = response.json()
                results = data.get('data', {}).get('Catalog', {}).get('searchStore', {}).get('elements', [])
                
                if results:
                    print(f"\nFound {len(results)} results for '{query}':")
                    for i, r in enumerate(results, 1):
                        print(f"\n{i}. {r.get('title')}")
                        print(f"   Namespace: {r.get('namespace')}")
                        print(f"   CatalogItemId: {r.get('id')}")
                        print(f"   Slug: {r.get('productSlug', 'N/A')}")
                    return results
                else:
                    print("No results found")
            else:
                print(f"Error: {response.text[:200]}")
                
        except Exception as e:
            print(f"Exception: {e}")
    
    return []


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 epic_search.py <game_name>")
        print("Example: python3 epic_search.py 'Dying Light 2'")
        sys.exit(1)
    
    query = sys.argv[1]
    search_epic_games(query)