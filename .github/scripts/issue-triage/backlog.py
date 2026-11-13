"""Paginated human-issue inventory and resumable assessment manifests."""

import argparse
import time
from pathlib import Path

from triage import GitHub, REPOSITORY, TriageError, digest, eligible, policy_digest, read_json, write_json


RETENTION_SECONDS = 90 * 24 * 60 * 60


def issue_stamp(issue: dict) -> str:
    return digest({name: issue.get(name) for name in (
        "number", "title", "body", "user", "labels", "state", "locked", "updated_at", "comments",
    )})


def inventory(client: GitHub) -> list[dict]:
    issues = client.pages("issues", state="open", sort="created", direction="asc")
    numbers = [item["number"] for item in issues if eligible(item)]
    if len(numbers) != len(set(numbers)):
        raise TriageError("Inventory changed during pagination; retry a read-only inventory")
    return sorted((item for item in issues if eligible(item)), key=lambda issue: issue["number"])


def manifest_for(issues: list[dict], now: int | None = None, previous: dict | None = None) -> dict:
    now = int(time.time()) if now is None else now
    policy = policy_digest()
    previous_entries = {}
    if previous is not None:
        validate_manifest(previous, now)
        if previous["policy_sha256"] == policy:
            previous_entries = previous["entries"]
    entries = {}
    for issue in issues:
        if not eligible(issue):
            continue
        key, stamp = str(issue["number"]), issue_stamp(issue)
        old = previous_entries.get(key, {})
        entries[key] = old if old.get("issue_sha256") == stamp else {
            "issue_sha256": stamp, "status": "pending",
        }
    return {
        "schema": 1, "repository": REPOSITORY, "policy_sha256": policy,
        "created_at": now, "expires_at": now + RETENTION_SECONDS, "entries": entries,
    }


def validate_manifest(manifest: dict, now: int | None = None) -> None:
    now = int(time.time()) if now is None else now
    if (
        not isinstance(manifest, dict) or manifest.get("schema") != 1
        or manifest.get("repository") != REPOSITORY or not isinstance(manifest.get("entries"), dict)
        or type(manifest.get("created_at")) is not int or type(manifest.get("expires_at")) is not int
        or not manifest["created_at"] <= now < manifest["expires_at"]
        or manifest["expires_at"] - manifest["created_at"] > RETENTION_SECONDS
    ):
        raise TriageError("Invalid or expired assessment manifest")
    for number, entry in manifest["entries"].items():
        if (
            not number.isdigit() or int(number) <= 0 or not isinstance(entry, dict)
            or entry.get("status") not in {"pending", "assessed", "error", "stale"}
            or not isinstance(entry.get("issue_sha256"), str)
        ):
            raise TriageError("Invalid assessment checkpoint")


def complete_coverage(manifest: dict, issues: list[dict], now: int | None = None) -> None:
    validate_manifest(manifest, now)
    if manifest["policy_sha256"] != policy_digest():
        raise TriageError("Assessment policy changed")
    for issue in issues:
        if not eligible(issue):
            continue
        entry = manifest["entries"].get(str(issue["number"]), {})
        if entry.get("status") != "assessed" or entry.get("issue_sha256") != issue_stamp(issue):
            raise TriageError("Assessment coverage is incomplete or stale")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="WPF backlog inventory")
    parser.add_argument("destination", type=Path)
    parser.add_argument("--resume", type=Path)
    args = parser.parse_args(argv)
    previous = read_json(args.resume, 64 * 1024 * 1024) if args.resume else None
    write_json(args.destination, manifest_for(inventory(GitHub()), previous=previous))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, KeyError, TypeError, OSError):
        raise SystemExit("Backlog inventory stopped; coverage is not complete.")
