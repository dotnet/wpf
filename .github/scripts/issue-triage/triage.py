"""Trusted configuration for WPF issue triage."""

import json
from pathlib import Path


AREA_MAP_PATH = Path(__file__).with_name("area-map.json")


class TriageError(ValueError):
    """A triage configuration or assessment cannot be used safely."""


def unique_keys(pairs: list[tuple[str, object]]) -> dict:
    result = {}
    for name, value in pairs:
        if name in result:
            raise TriageError(f"Duplicate area label: {name}")
        result[name] = value
    return result


def validate_area_map(mapping: object, catalog: set[str]) -> None:
    if not isinstance(mapping, dict) or not mapping:
        raise TriageError("Area map must be a nonempty object")
    for name, guidance in mapping.items():
        if not isinstance(name, str) or not name.strip():
            raise TriageError("Area labels must be nonempty strings")
        if name not in catalog:
            raise TriageError(f"Area label is not in the repository catalog: {name}")
        if not isinstance(guidance, str) or not guidance.strip():
            raise TriageError(f"Area guidance must be nonempty text: {name}")


def load_area_map(catalog: set[str], path: Path = AREA_MAP_PATH) -> dict[str, str]:
    try:
        mapping = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_keys)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise TriageError("Cannot read the area map as UTF-8 JSON") from error
    validate_area_map(mapping, catalog)
    return mapping
