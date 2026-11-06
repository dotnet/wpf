"""Trusted configuration and intake rules for WPF issue triage."""

import json
import hashlib
import os
from pathlib import Path
import re
import time
import urllib.error
import urllib.parse
import urllib.request


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


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class GitHub:
    def __init__(self, token: str | None = None):
        self.token = token or os.environ.get("GITHUB_TOKEN")
        if not self.token:
            raise TriageError("GITHUB_TOKEN is required")
        self.opener = urllib.request.build_opener(NoRedirect)

    def get(self, path: str, **params):
        if not re.fullmatch(r"(?:issues(?:/[1-9][0-9]*(?:/(?:comments|timeline|labels))?)?|labels)", path):
            raise TriageError("Unsupported repository API path")
        url = f"https://api.github.com/repos/{REPOSITORY}/{path}"
        if params:
            url += "?" + urllib.parse.urlencode(params)
        request = urllib.request.Request(url, headers={
            "Authorization": f"Bearer {self.token}", "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
        })
        for attempt in range(3):
            try:
                with self.opener.open(request, timeout=30) as response:
                    data = response.read(2 * 1024 * 1024 + 1)
                if len(data) > 2 * 1024 * 1024:
                    raise TriageError("GitHub response exceeds the read limit")
                return json.loads(data)
            except urllib.error.HTTPError as error:
                retryable = error.code in {429, 502, 503, 504} or (
                    error.code == 403 and error.headers.get("X-RateLimit-Remaining") == "0"
                )
                if not retryable or attempt == 2:
                    raise TriageError(f"GitHub read failed with HTTP {error.code}") from None
                time.sleep(2 ** attempt)
            except (urllib.error.URLError, TimeoutError, UnicodeError, json.JSONDecodeError):
                raise TriageError("GitHub read failed") from None

    def pages(self, path: str, max_pages: int = 100, **params) -> list:
        items = []
        for page in range(1, max_pages + 1):
            batch = self.get(path, **params, per_page=100, page=page)
            if not isinstance(batch, list):
                raise TriageError("Expected a paginated GitHub list")
            items.extend(batch)
            if len(batch) < 100:
                return items
        raise TriageError("GitHub pagination limit reached; coverage is incomplete")


def digest(value: object) -> str:
    encoded = json.dumps(value, sort_keys=True, ensure_ascii=True, separators=(",", ":"))
    return hashlib.sha256(encoded.encode("utf-8")).hexdigest()


def policy_digest() -> str:
    directory = AREA_MAP_PATH.parent
    files = sorted(directory.glob("*.py")) + [directory / "area-map.json", directory / "priority-policy.md"]
    workflow = directory.parents[1] / "workflows" / "issue-triage.md"
    if workflow.exists():
        files.append(workflow)
    return digest({path.name: path.read_text(encoding="utf-8") for path in files})


def prepare(client: GitHub, number: int, releases: dict | None = None) -> dict:
    if type(number) is not int or not 0 < number < 1_000_000_000:
        raise TriageError("Invalid issue number")
    issue = client.get(f"issues/{number}")
    if not isinstance(issue, dict) or issue.get("number") != number:
        raise TriageError("GitHub returned an unexpected issue")
    if not eligible(issue):
        return {"should_process": False, "number": number, "reason": "Ineligible issue"}
    if type(issue.get("comments")) is not int or not 0 <= issue["comments"] < 1000:
        raise TriageError("Issue comment coverage requires manual review")
    catalog = {item["name"] for item in client.pages("labels")}
    if not {"Priority:0", "Priority:1"} <= catalog:
        raise TriageError("Required priority labels are missing; labels will not be created")
    areas = load_area_map(catalog)
    comments = client.pages(f"issues/{number}/comments", max_pages=10)
    timeline = client.pages(f"issues/{number}/timeline", max_pages=20)
    followups = []
    for comment in comments:
        author = comment.get("user") or {}
        if author.get("type") == "User" and (
            author.get("id") == issue["user"]["id"]
            or comment.get("author_association") in {"OWNER", "MEMBER", "COLLABORATOR"}
        ) and not str(author.get("login", "")).casefold().endswith(("-bot", "[bot]")):
            if str(author.get("login", "")).casefold() not in SERVICE_ACCOUNTS:
                followups.append({
                    "id": comment["id"], "body": comment.get("body") or "",
                    "author_id": author.get("id"), "association": comment.get("author_association"),
                })
    removed = sorted({
        event["label"]["name"] for event in timeline
        if event.get("event") == "unlabeled" and isinstance(event.get("label"), dict)
        and (event.get("actor") or {}).get("type") == "User"
    })
    source = {
        "issue": issue, "followups": followups, "human_removed_labels": removed,
        "policy_sha256": policy_digest(), "catalog": sorted(catalog), "releases": releases or {},
    }
    selected = followups[:5] + followups[max(5, len(followups) - 5):]
    body = issue.get("body") or ""
    if not isinstance(body, str) or not isinstance(issue.get("title"), str):
        raise TriageError("Issue title and body must be text")
    return {
        "schema": 1, "should_process": True, "repository": REPOSITORY, "number": number,
        "input_sha256": digest(source), "policy_sha256": source["policy_sha256"],
        "issue_kind": issue_kind(issue), "title": issue["title"][:300], "body": body[:16000],
        "existing_labels": sorted(label_names(issue)), "human_removed_labels": removed,
        "allowed_areas": areas, "followups": [{**item, "body": item["body"][:2000]} for item in selected],
        "releases": source["releases"],
        "context_truncated": len(body) > 16000 or len(followups) > 10
        or any(len(item["body"]) > 2000 for item in selected),
    }


def attach_context(context_path: Path, prompt_path: Path) -> None:
    evidence = json.loads(context_path.read_text(encoding="utf-8"))
    prompt = prompt_path.read_text(encoding="utf-8")
    marker = "[WPF_TRIAGE_CONTEXT]"
    if prompt.count(marker) != 1:
        raise TriageError("Prompt must have exactly one evidence slot")
    encoded = json.dumps(evidence, ensure_ascii=True).replace("<", "\\u003c").replace(">", "\\u003e")
    prompt_path.write_text(prompt.replace(marker, encoded), encoding="utf-8")


RESULT_FIELDS = {
    "input_sha256", "issue_kind", "area", "area_confidence", "area_quote",
    "confidence", "impact", "scope", "workaround", "affected_version", "evidence_quote",
}


def validate_result(result: object, evidence: dict) -> dict:
    if not evidence.get("should_process") or not isinstance(result, dict) or set(result) != RESULT_FIELDS:
        raise TriageError("Unexpected assessment schema")
    if result["input_sha256"] != evidence["input_sha256"]:
        raise TriageError("Assessment does not match the prepared evidence")
    enums = {
        "issue_kind": {"BUG", "FEATURE", "OTHER"},
        "area_confidence": {"HIGH", "MEDIUM", "LOW", "NONE"},
        "confidence": {"HIGH", "MEDIUM", "LOW", "NONE"},
        "impact": {"CRASH", "REGRESSION", "BROKEN_FUNCTIONALITY", "OTHER"},
        "scope": {"BROAD", "CORE", "NARROW", "UNKNOWN"},
        "workaround": {"NONE_REPORTED", "VIABLE", "UNKNOWN"},
    }
    for field, values in enums.items():
        if not isinstance(result[field], str) or result[field] not in values:
            raise TriageError(f"Invalid assessment field: {field}")
    for field in ("area", "area_quote", "affected_version", "evidence_quote"):
        if not isinstance(result[field], str) or not 1 <= len(result[field]) <= 700:
            raise TriageError(f"Invalid assessment text: {field}")
    corpus = "\n".join([evidence["title"], evidence["body"]] + [
        item["body"] for item in evidence["followups"]
    ])
    if result["area"] == "None":
        if result["area_confidence"] != "NONE" or result["area_quote"] != "None":
            raise TriageError("Unknown area must not claim routing confidence")
    elif result["area"] not in evidence["allowed_areas"]:
        raise TriageError("Area is not allowlisted")
    elif result["area_confidence"] == "HIGH" and (
        result["area_quote"] == "None" or result["area_quote"] not in corpus
    ):
        raise TriageError("High-confidence area requires a report quote")
    if result["confidence"] == "HIGH" and result["impact"] != "OTHER":
        if result["evidence_quote"] == "None" or result["evidence_quote"] not in corpus:
            raise TriageError("High-confidence impact requires a report quote")
    version = result["affected_version"]
    if version != "None" and (
        not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version)
        or not re.search(r"(?<![0-9.])" + re.escape(version) + r"(?![0-9.])", corpus)
    ):
        raise TriageError("Affected version must be quoted in the report")
    return dict(result)


def label_plan(result: dict, evidence: dict) -> dict:
    from priority import candidate_priority

    result = validate_result(result, evidence)
    existing = set(evidence["existing_labels"])
    removed = set(evidence["human_removed_labels"])
    areas = set(evidence["allowed_areas"])
    additions = []
    if (
        not evidence["context_truncated"] and result["area"] in areas
        and result["area_confidence"] == "HIGH" and not existing & areas and not removed & areas
    ):
        additions.append(result["area"])
    priority = candidate_priority(result, evidence)
    if (
        priority and not any(name.startswith("Priority:") for name in existing | removed)
        and not existing & (FEATURE_LABELS | OTHER_LABELS)
    ):
        additions.append(priority)
    return {
        "schema": 1, "repository": REPOSITORY, "number": evidence["number"],
        "input_sha256": evidence["input_sha256"], "policy_sha256": evidence["policy_sha256"],
        "result": result, "add_labels": additions, "published": False,
    }
