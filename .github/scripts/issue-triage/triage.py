"""Trusted configuration and intake rules for WPF issue triage."""

import json
from pathlib import Path
import re


AREA_MAP_PATH = Path(__file__).with_name("area-map.json")
REPOSITORY = "dotnet/wpf"
SERVICE_ACCOUNTS = frozenset({"dotnet-bot"})
BUG_LABELS = frozenset({"Bug", ":beetle: bug"})
FEATURE_LABELS = frozenset({"Feature Request", "Enhancement Requested", "API suggestion"})
OTHER_LABELS = frozenset({"Question", "Documentation", "External Non-WPF Issue", "issue-type-test-bug"})


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


def eligible(issue: object) -> bool:
    if not isinstance(issue, dict):
        return False
    author = issue.get("user")
    if not isinstance(author, dict) or author.get("type") != "User":
        return False
    login = author.get("login")
    if not isinstance(login, str) or not re.fullmatch(r"[A-Za-z0-9-]{1,39}", login):
        return False
    if login.casefold() in SERVICE_ACCOUNTS or login.casefold().endswith("-bot"):
        return False
    return (
        issue.get("repository_url") == f"https://api.github.com/repos/{REPOSITORY}"
        and "pull_request" not in issue
        and issue.get("state") == "open"
        and issue.get("locked") is False
        and type(issue.get("number")) is int
        and issue["number"] > 0
        and type(author.get("id")) is int
        and author["id"] > 0
    )


def label_names(issue: dict) -> set[str]:
    labels = issue.get("labels")
    if not isinstance(labels, list):
        raise TriageError("Issue labels must be supplied as a list")
    names = set()
    for label in labels:
        if (
            not isinstance(label, dict)
            or not isinstance(label.get("name"), str)
            or not label["name"].strip()
        ):
            raise TriageError("Each issue label must have a name")
        names.add(label["name"])
    return names


def issue_kind(issue: dict) -> str:
    """Return an intake hint, not a verified classification of the report."""
    labels = label_names(issue)
    signals = set()
    for kind, known_labels in (("BUG", BUG_LABELS), ("FEATURE", FEATURE_LABELS), ("OTHER", OTHER_LABELS)):
        if labels & known_labels:
            signals.add(kind)
    if signals:
        return next(iter(signals)) if len(signals) == 1 else "OTHER"

    body = issue.get("body")
    if body is None:
        body = ""
    if not isinstance(body, str):
        raise TriageError("Issue body must be text")
    headings = {
        heading.casefold()
        for heading in re.findall(r"^#{1,6}[ \t]+(.+?)[ \t]*#*[ \t]*$", body, re.MULTILINE)
    }
    if {"description", "reproduction steps", "expected behavior", "actual behavior"} <= headings:
        signals.add("BUG")
    if {"background and motivation", "api proposal", "api usage"} <= headings:
        signals.add("FEATURE")
    return next(iter(signals)) if len(signals) == 1 else "OTHER"
