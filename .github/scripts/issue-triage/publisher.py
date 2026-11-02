"""Publish only validated additive labels; never write issue comments."""

import argparse
import os
from pathlib import Path
import time

from backlog import RETENTION_SECONDS, artifact_json, inventory, load_approval, resolve_coverage
from priority import enforce_limits, projected_counts
from triage import GitHub, REPOSITORY, TriageError, label_plan, prepare, write_json


def publish_record(client: GitHub, record: dict, publish: bool, area_enabled: bool, priority_enabled: bool, approval_run: int = 0) -> dict:
    if type(record.get("assessed_at")) is not int or not int(time.time()) - RETENTION_SECONDS <= record["assessed_at"] <= int(time.time()):
        raise TriageError("Assessment is expired")
    evidence = prepare(client, record["number"])
    if not evidence.get("should_process") or record.get("input_sha256") != evidence["input_sha256"]:
        raise TriageError("Issue changed before publication")
    plan = label_plan(record["result"], evidence)
    labels = [name for name in plan["add_labels"] if area_enabled or name.startswith("Priority:")]
    labels = [name for name in labels if priority_enabled or not name.startswith("Priority:")]
    if any(name.startswith("Priority:") for name in labels):
        approval = load_approval(client, approval_run)
        issues = inventory(client)
        records = resolve_coverage(client, approval, issues, (record,))
        proposed = {number: item["add_labels"] for number, item in records.items()}
        proposed[record["number"]] = labels
        enforce_limits(projected_counts(issues, proposed))
    plan.update(evidence=evidence, enabled_labels=labels, assessed_at=record["assessed_at"], run_id=os.environ.get("GITHUB_RUN_ID"))
    if not publish or not labels:
        return plan
    latest = prepare(client, record["number"])
    if latest != evidence:
        raise TriageError("Issue changed immediately before label publication")
    client.add_labels(record["number"], labels)
    refreshed = prepare(client, record["number"])
    for field in ("title", "body", "followups", "human_removed_labels", "releases", "context_truncated", "policy_sha256"):
        if refreshed.get(field) != evidence.get(field):
            raise TriageError("Evidence changed during publication; inspect partial writes")
    if set(refreshed["existing_labels"]) != set(evidence["existing_labels"]) | set(labels):
        raise TriageError("Label read-back changed concurrently; inspect partial writes")
    result = {**record["result"], "input_sha256": refreshed["input_sha256"]}
    updated = label_plan(result, refreshed)
    updated.update(
        evidence=refreshed, published=True, published_labels=labels,
        assessed_at=record["assessed_at"], run_id=os.environ.get("GITHUB_RUN_ID"),
    )
    return updated


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="WPF label publication")
    parser.add_argument("assessment_run", type=int)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--publish", choices=("true", "false"), default="false")
    parser.add_argument("--area-enabled", choices=("true", "false"), default="false")
    parser.add_argument("--priority-enabled", choices=("true", "false"), default="false")
    parser.add_argument("--approval-run", type=int, default=0)
    args = parser.parse_args(argv)
    if os.environ.get("GITHUB_REPOSITORY") != REPOSITORY or os.environ.get("GITHUB_REF") != "refs/heads/main":
        raise TriageError("Publication must use trusted main")
    client = GitHub()
    record = artifact_json(client, args.assessment_run, "issue-triage.lock.yml", "wpf-assessment", "wpf-assessment.json")
    if record.get("run_id") != str(args.assessment_run):
        raise TriageError("Assessment run provenance mismatch")
    plan = publish_record(
        client, record, args.publish == "true", args.area_enabled == "true",
        args.priority_enabled == "true", args.approval_run,
    )
    write_json(args.destination, plan)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, KeyError, TypeError, OSError):
        raise SystemExit("Label publication stopped; inspect trusted inputs and any partial writes.")
