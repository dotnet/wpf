"""Paginated human-issue inventory and resumable assessment manifests."""

import argparse
import io
import json
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zipfile
from pathlib import Path

from triage import GitHub, REPOSITORY, TriageError, digest, eligible, label_plan, policy_digest, read_json, write_json


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
        "credits_reserved": previous.get("credits_reserved", 0) if previous else 0,
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


def trusted_run(client: GitHub, run_id: int, workflow: str) -> dict:
    if type(run_id) is not int or run_id <= 0:
        raise TriageError("Invalid workflow run")
    run = client.get(f"actions/runs/{run_id}")
    if (
        run.get("head_branch") != "main" or run.get("conclusion") != "success"
        or run.get("event") not in {"workflow_dispatch", "issues", "issue_comment"}
        or run.get("head_repository", {}).get("full_name") != REPOSITORY
        or run.get("path") != f".github/workflows/{workflow}"
    ):
        raise TriageError("Assessment artifact does not come from a trusted successful main run")
    return run


def artifact_json(client: GitHub, run_id: int, workflow: str, prefix: str, filename: str) -> dict:
    trusted_run(client, run_id, workflow)
    artifacts = client.get(f"actions/runs/{run_id}/artifacts", per_page=100)
    if artifacts.get("total_count", 101) > 100:
        raise TriageError("Artifact enumeration is incomplete")
    matches = [
        item for item in artifacts.get("artifacts", [])
        if item.get("name") == f"{prefix}-{run_id}" and not item.get("expired")
    ]
    if len(matches) != 1:
        raise TriageError("Required assessment artifact is missing or expired")
    url = f"https://api.github.com/repos/{REPOSITORY}/actions/artifacts/{matches[0]['id']}/zip"
    request = urllib.request.Request(url, headers={"Authorization": f"Bearer {client.token}"})
    try:
        client.opener.open(request, timeout=30).close()
        raise TriageError("Expected an artifact download redirect")
    except urllib.error.HTTPError as error:
        if error.code != 302:
            raise TriageError("Artifact download unavailable") from None
        location = error.headers.get("Location", "")
    parsed = urllib.parse.urlparse(location)
    if parsed.scheme != "https" or parsed.username or parsed.password or not (
        (parsed.hostname or "").endswith(".blob.core.windows.net")
        or (parsed.hostname or "").endswith(".actions.githubusercontent.com")
    ):
        raise TriageError("Unexpected artifact storage destination")
    # The signed storage request deliberately has no GitHub authorization header.
    with client.opener.open(urllib.request.Request(location), timeout=30) as response:
        archive = response.read(64 * 1024 * 1024 + 1)
    if len(archive) > 64 * 1024 * 1024:
        raise TriageError("Artifact exceeds the download limit")
    with zipfile.ZipFile(io.BytesIO(archive)) as bundle:
        if bundle.namelist() != [filename] or bundle.getinfo(filename).file_size > 64 * 1024 * 1024:
            raise TriageError("Unexpected artifact contents")
        return json.loads(bundle.read(filename))


def wait_assessment(client: GitHub, request_id: str, timeout: int = 1800) -> int:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        runs = client.get("actions/workflows/issue-triage.lock.yml/runs", event="workflow_dispatch", per_page=100)
        matches = [run for run in runs.get("workflow_runs", []) if run.get("display_title") == f"WPF assessment {request_id}"]
        if len(matches) > 1:
            raise TriageError("Ambiguous assessment dispatch")
        if matches and matches[0].get("status") == "completed":
            trusted_run(client, matches[0]["id"], "issue-triage.lock.yml")
            return matches[0]["id"]
        time.sleep(10)
    raise TriageError("Assessment did not finish; inspect the run before retrying")


def assess_batch(client: GitHub, manifest: dict, size: int, credit_budget: int, destination: Path, retry_errors: bool = False) -> dict:
    validate_manifest(manifest)
    if type(size) is not int or not 0 <= size <= 10 or type(credit_budget) is not int or credit_budget < 0:
        raise TriageError("Batch size must be 0..10 and the aggregate credit budget explicit")
    pending = [
        int(number) for number, entry in manifest["entries"].items()
        if entry["status"] in {"pending", "stale"} or (retry_errors and entry["status"] == "error")
    ][:size]
    reserved = manifest.get("credits_reserved", 0)
    if type(reserved) is not int or reserved < 0 or reserved + 30 * len(pending) > credit_budget:
        raise TriageError("Aggregate assessment budget would be exceeded")
    write_json(destination, manifest)
    for number in pending:
        entry = manifest["entries"][str(number)]
        manifest["credits_reserved"] += 30
        write_json(destination, manifest)
        try:
            request_id = f"wpf-{number}-{uuid.uuid4().hex}"
            entry["request_id"] = request_id
            write_json(destination, manifest)
            client.dispatch(number, request_id)
            run_id = wait_assessment(client, request_id)
            record = artifact_json(client, run_id, "issue-triage.lock.yml", "wpf-assessment", "wpf-assessment.json")
            current = client.get(f"issues/{number}")
            evidence = record["evidence"]
            if (
                record.get("run_id") != str(run_id) or record.get("number") != number
                or record.get("policy_sha256") != manifest["policy_sha256"]
                or not eligible(current) or issue_stamp(current) != entry["issue_sha256"]
                or evidence.get("number") != number or evidence.get("repository") != REPOSITORY
                or label_plan(record["result"], evidence)["add_labels"] != record.get("add_labels")
            ):
                raise TriageError("Assessment became stale or has invalid provenance")
            entry.update(status="assessed", assessment=record)
        except (ValueError, KeyError, TypeError, OSError):
            entry["status"] = "error"
        write_json(destination, manifest)
    return manifest


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="WPF backlog inventory")
    parser.add_argument("destination", type=Path)
    parser.add_argument("--resume", type=Path)
    parser.add_argument("--resume-run", type=int)
    parser.add_argument("--batch-size", type=int, default=0)
    parser.add_argument("--credit-budget", type=int, default=0)
    parser.add_argument("--retry-errors", choices=("true", "false"), default="false")
    args = parser.parse_args(argv)
    client = GitHub()
    previous = read_json(args.resume, 64 * 1024 * 1024) if args.resume else None
    if args.resume_run:
        previous = artifact_json(client, args.resume_run, "issue-triage-backlog.yml", "wpf-backlog", "manifest.json")
    manifest = manifest_for(inventory(client), previous=previous)
    assess_batch(client, manifest, args.batch_size, args.credit_budget, args.destination, args.retry_errors == "true")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, KeyError, TypeError, OSError):
        raise SystemExit("Backlog inventory stopped; coverage is not complete.")
