"""Evidence-based candidate priorities; calibration is required before writes."""

import json
import re
import urllib.request

from triage import NoRedirect, TriageError, digest, eligible, label_names


RELEASE_URL = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json"


def release_summary(metadata: dict) -> dict:
    versions = []
    for channel in metadata.get("releases-index", []):
        version = channel.get("latest-runtime", channel.get("latest-release", ""))
        if channel.get("support-phase") in {"active", "maintenance"} and re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version):
            versions.append(version)
    if not versions:
        raise TriageError("Trusted release metadata contains no supported stable runtime")
    versions = sorted(set(versions), key=lambda value: tuple(map(int, value.split("."))))
    return {"source": RELEASE_URL, "supported_stable": versions, "latest_stable": versions[-1]}


def fetch_releases() -> dict:
    opener = urllib.request.build_opener(NoRedirect)
    try:
        with opener.open(RELEASE_URL, timeout=30) as response:
            data = response.read(1024 * 1024 + 1)
        if len(data) > 1024 * 1024:
            raise TriageError("Release metadata exceeds the read limit")
        return release_summary(json.loads(data))
    except (OSError, UnicodeError, json.JSONDecodeError):
        raise TriageError("Cannot read trusted runtime release metadata") from None


def candidate_priority(result: dict, evidence: dict) -> str | None:
    if (
        result.get("issue_kind") != "BUG"
        or result.get("confidence") != "HIGH"
        or evidence.get("context_truncated")
        or evidence.get("issue_kind") == "FEATURE"
        or result.get("scope") not in {"BROAD", "CORE"}
        or result.get("workaround") in {"VIABLE", "UNKNOWN"}
    ):
        return None
    version = result.get("affected_version")
    releases = evidence.get("releases", {})
    if version not in releases.get("supported_stable", []):
        return None
    impact = result.get("impact")
    if version == releases.get("latest_stable") and impact in {"CRASH", "REGRESSION"}:
        return "Priority:0"
    if impact in {"CRASH", "REGRESSION", "BROKEN_FUNCTIONALITY"}:
        return "Priority:1"
    return None


def release_fingerprint(summary: dict) -> str:
    return digest(summary)


def projected_counts(issues: list[dict], additions: dict[int, list[str]]) -> dict[str, int]:
    totals = {"Priority:0": set(), "Priority:1": set()}
    for issue in issues:
        if not eligible(issue):
            continue
        labels = label_names(issue) | set(additions.get(issue["number"], []))
        for priority in totals:
            if priority in labels:
                totals[priority].add(issue["number"])
    return {priority: len(numbers) for priority, numbers in totals.items()}


def enforce_limits(counts: dict[str, int]) -> None:
    if counts["Priority:0"] > 20 or counts["Priority:1"] > 50:
        raise TriageError("Projected P0/P1 totals exceed 20/50; review criteria before publication")
