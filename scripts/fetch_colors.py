"""Vendor the wardrobe color taxonomy from an authoritative GitHub dataset.

Single source of truth for colors: a small curated taxonomy (main colors + a few shades each).
Hex values are resolved from meodai/color-names (https://github.com/meodai/color-names); any name
not found there falls back to the hand value below (logged). The resulting hierarchical JSON is
written identically to both consumers:
  - ml_api/colors.json                          (ML flattens the names into its zero-shot vocabulary)
  - wardrobe_web/src/constants/colors.json      (frontend derives the name->hex map + grouped UI)

Run once and commit the outputs — there is no runtime fetch:
    python3 scripts/fetch_colors.py
"""
import json
import re
import sys
import urllib.request
from pathlib import Path

# meodai/color-names, served via the npm CDN (the GitHub raw path moves between releases).
DATASET_URL = "https://unpkg.com/color-name-list/dist/colornames.json"

# Curated taxonomy: main color -> [shades]. Edit HERE to change the palette, then re-run.
# The fallback hex (used only if a name is missing from the dataset) lives in FALLBACK below.
TAXONOMY = {
    "black":  [],
    "white":  [],
    "gray":   [],
    "beige":  [],
    "brown":  [],
    "red":    ["dark red", "burgundy"],
    "orange": [],
    "yellow": ["mustard"],
    "green":  ["olive"],
    "blue":   ["navy blue", "denim blue", "teal"],
    "purple": ["violet"],
    "pink":   ["rose"],
}

# Last-resort hex if a name isn't in the dataset (kept from the previous hand-picked palette).
FALLBACK = {
    "black": "#1a1a1a",
    "white": "#f7f7f7",
    "gray": "#8a8d91",
    "beige": "#d8c4a3",
    "brown": "#6b4a2b",
    "red": "#d11a3a", "dark red": "#8b0000", "burgundy": "#6d213c",
    "orange": "#f5821f",
    "yellow": "#f5cb2e", "mustard": "#d6a516",
    "green": "#2f9e44", "olive": "#6b7d3a",
    "blue": "#2a4bd7", "navy blue": "#1f2d50", "denim blue": "#3b5e8c", "teal": "#128f8b",
    "purple": "#7d3cc9", "violet": "#8a4fdb",
    "pink": "#f06fa3", "rose": "#e36b8a",
}

REPO_ROOT = Path(__file__).resolve().parent.parent
OUTPUTS = [
    REPO_ROOT / "ml_api" / "colors.json",
    REPO_ROOT / "wardrobe_web" / "src" / "constants" / "colors.json",
]


def norm(name: str) -> str:
    return re.sub(r"[^a-z0-9]", "", name.lower())


def main() -> int:
    print(f"Downloading dataset: {DATASET_URL}")
    with urllib.request.urlopen(DATASET_URL, timeout=60) as resp:
        dataset = json.load(resp)
    # First occurrence of each normalized name wins.
    index = {}
    for entry in dataset:
        key = norm(entry["name"])
        index.setdefault(key, entry["hex"])
    print(f"Dataset entries: {len(dataset)} ({len(index)} unique names)")

    resolved, fell_back = 0, []

    def hex_for(name: str) -> str:
        nonlocal resolved
        hit = index.get(norm(name))
        if hit:
            resolved += 1
            return hit.lower()
        fell_back.append(name)
        return FALLBACK[name]

    taxonomy = {}
    for main_name, shades in TAXONOMY.items():
        taxonomy[main_name] = {
            "hex": hex_for(main_name),
            "shades": {s: hex_for(s) for s in shades},
        }

    total = sum(1 + len(s) for s in TAXONOMY.values())
    blob = json.dumps(taxonomy, indent=2, ensure_ascii=False) + "\n"
    for path in OUTPUTS:
        path.write_text(blob, encoding="utf-8")
        print(f"Wrote {path.relative_to(REPO_ROOT)}")

    print(f"\nResolved {resolved}/{total} from dataset; {len(fell_back)} fell back: {fell_back}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
