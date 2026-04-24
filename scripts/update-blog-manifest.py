#!/usr/bin/env python3
"""
Scan blog/posts/*.md files and add any whose date has arrived (date <= today)
to blog/manifest.json.  Idempotent: already-published slugs are never re-added.
"""

import json
import os
import sys
from datetime import date

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.join(SCRIPT_DIR, "..")
WWWROOT = os.path.join(REPO_ROOT, "CollabsKus.BlazorWebAssembly", "wwwroot")
POSTS_DIR = os.path.join(WWWROOT, "blog", "posts")
MANIFEST_PATH = os.path.join(WWWROOT, "blog", "manifest.json")


def parse_frontmatter(content: str) -> dict[str, str]:
    """Parse simple key: value YAML frontmatter between --- delimiters."""
    if not content.startswith("---"):
        return {}
    end = content.find("\n---", 3)
    if end < 0:
        return {}
    front: dict[str, str] = {}
    for line in content[3:end].splitlines():
        colon = line.find(":")
        if colon < 0:
            continue
        key = line[:colon].strip()
        val = line[colon + 1:].strip()
        if key:
            front[key] = val
    return front


def main() -> None:
    today = date.today()

    if os.path.exists(MANIFEST_PATH):
        with open(MANIFEST_PATH, "r", encoding="utf-8") as f:
            manifest = json.load(f)
    else:
        manifest = {"posts": []}

    existing_slugs: set[str] = {p["slug"] for p in manifest.get("posts", [])}
    new_posts: list[dict] = []

    for filename in sorted(os.listdir(POSTS_DIR)):
        if not filename.endswith(".md"):
            continue

        slug = filename[:-3]
        if slug in existing_slugs:
            continue

        filepath = os.path.join(POSTS_DIR, filename)
        with open(filepath, "r", encoding="utf-8") as f:
            raw = f.read()

        front = parse_frontmatter(raw)
        if not front:
            print(f"  skip {filename}: no frontmatter", file=sys.stderr)
            continue

        date_str = front.get("date", "")
        try:
            post_date = date.fromisoformat(date_str)
        except ValueError:
            print(f"  skip {filename}: invalid date '{date_str}'", file=sys.stderr)
            continue

        if post_date > today:
            print(f"  future {slug} ({date_str})")
            continue

        new_posts.append({
            "slug": slug,
            "title": front.get("title", slug),
            "date": date_str,
            "author": front.get("author", ""),
            "excerpt": front.get("excerpt", ""),
        })
        print(f"  publish {slug}")

    if not new_posts:
        print("No new posts to publish.")
        return

    manifest["posts"].extend(new_posts)
    manifest["posts"].sort(key=lambda p: p["date"], reverse=True)

    with open(MANIFEST_PATH, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print(f"manifest.json updated ({len(new_posts)} new post(s)).")


if __name__ == "__main__":
    main()
