#!/usr/bin/env python3
"""
Generate a triage report from GitHub issues.

This script fetches open issues created in the past 7 days from the repository
and generates two outputs:
  1. triage-report.md  — Human-readable markdown table
  2. triage-data.json  — Machine-readable JSON for applying labels

Usage:
  python generate-triage-report.py [--days 7] [--output-dir .]

Requires: gh CLI authenticated and available on PATH.
"""

import json
import subprocess
import sys
import argparse
from datetime import datetime, timedelta, timezone


def fetch_recent_issues(days: int) -> list[dict]:
    """Fetch open issues created within the last N days using gh CLI."""
    since_date = (datetime.now(timezone.utc) - timedelta(days=days)).strftime("%Y-%m-%dT%H:%M:%SZ")

    result = subprocess.run(
        [
            "gh", "issue", "list",
            "--state", "open",
            "--json", "number,title,body,labels,createdAt",
            "--limit", "200",
        ],
        capture_output=True,
        text=True,
        check=True,
    )

    all_issues = json.loads(result.stdout)

    # Filter to issues created within the time window
    recent_issues = []
    for issue in all_issues:
        created = issue.get("createdAt", "")
        if created >= since_date:
            recent_issues.append(issue)

    return recent_issues


def classify_issue(issue: dict) -> dict:
    """
    Basic heuristic classification of an issue.

    This provides a starting-point classification. When used with the Copilot
    agent (issue-triage.md), the agent provides more accurate AI-driven
    classification. This script serves as the fallback for automated runs.
    """
    title = (issue.get("title") or "").lower()
    body = (issue.get("body") or "").lower()
    text = f"{title} {body}"

    # Determine type
    issue_type = "Bug"  # default
    if any(kw in text for kw in ["api proposal", "api suggestion", "[api proposal]", "public api"]):
        issue_type = "api-suggestion"
    elif any(kw in text for kw in ["feature request", "enhancement", "would be nice", "please add", "suggestion"]):
        issue_type = "Enhancement"
    elif any(kw in text for kw in ["regression", "used to work", "worked before", "broke after", "no longer works"]):
        issue_type = "regression"
    elif any(kw in text for kw in ["documentation", "docs", "typo in doc", "readme"]):
        issue_type = "Documentation"
    elif any(kw in text for kw in ["question", "how to", "how do i", "is it possible"]):
        issue_type = "Question"

    # Determine component
    component = "area/Controls"  # default
    component_map = {
        "area/Text": ["textbox", "richtextbox", "ime", "directwrite", "flowdocument", "textstore",
                       "font", "caret", "spell", "textblock", "textedit", "textview"],
        "area/Rendering": ["render", "visual", "layout", "directx", "d3d", "gpu", "blank output",
                           "rendertargetbitmap", "drawingcontext", "visual tree"],
        "area/Input": ["keyboard", "mouse", "touch", "stylus", "pen", "focus", "gesture",
                       "manipulation", "penImc", "wisp"],
        "area/Controls": ["datagrid", "treeview", "listview", "combobox", "button", "menu",
                          "tabcontrol", "scrollviewer", "expander", "slider", "checkbox", "tooltip"],
        "area/XAML": ["xaml", "markup", "resource", "template", "staticresource",
                      "dynamicresource", "binding", "x:bind", "x:type"],
        "area/DataBinding": ["binding", "converter", "inotifypropertychanged", "collectionview",
                             "observablecollection", "validationrule", "multibinding", "prioritybinding"],
        "area/Printing": ["print", "xps", "system.printing", "printdialog", "printqueue"],
        "area/Accessibility": ["automation", "narrator", "screen reader", "automationpeer",
                               "uiautomation", "accessible"],
        "area/Theming": ["theme", "style", "highcontrast", "systemcolors", "dynamicresource"],
        "area/Interop": ["winforms", "hwndhost", "windowsformshost", "interop", "com "],
        "area/Media": ["image", "video", "animation", "storyboard", "transform", "effect",
                        "bitmapimage", "mediaelement"],
        "area/Window": ["window", "chrome", "dpi", "scaling", "monitor", "dialog",
                         "windowchrome", "fullscreen", "minimize", "maximize"],
    }

    for area, keywords in component_map.items():
        if any(kw in text for kw in keywords):
            component = area
            break

    # Determine priority
    priority = "—"
    if any(kw in text for kw in ["crash", "nullreferenceexception", "accessviolation",
                                  "hang", "freeze", "data loss", "security",
                                  "system.executionengineexception", "unhandled exception"]):
        priority = "priority/high"
    elif any(kw in text for kw in ["cosmetic", "minor", "typo", "nice to have"]):
        priority = "priority/low"

    # Determine action
    action = "acknowledge"
    has_repro = any(kw in text for kw in ["steps to reproduce", "repro steps", "reproduction",
                                           "1.", "step 1"])
    has_expected = any(kw in text for kw in ["expected", "actual behavior"])
    has_version = any(kw in text for kw in [".net ", "dotnet", "net8", "net9", "net10"])

    if not has_repro and issue_type == "Bug":
        action = "needs-info"
    elif not has_expected and issue_type == "Bug":
        action = "needs-info"

    # Build reasoning
    reasoning_parts = []
    if issue_type == "regression":
        reasoning_parts.append("Author mentions previous working behavior")
    if priority == "priority/high":
        reasoning_parts.append("crash/hang indicates high severity")
    if action == "needs-info":
        missing = []
        if not has_repro:
            missing.append("repro steps")
        if not has_expected:
            missing.append("expected behavior")
        if not has_version:
            missing.append(".NET version")
        reasoning_parts.append(f"Missing: {', '.join(missing)}")

    reasoning = "; ".join(reasoning_parts) if reasoning_parts else f"Classified as {issue_type} in {component}"

    return {
        "number": issue["number"],
        "title": issue.get("title", ""),
        "proposed_type": issue_type,
        "proposed_component": component,
        "proposed_priority": priority,
        "proposed_action": action,
        "reasoning": reasoning,
        "existing_labels": [l.get("name", "") for l in issue.get("labels", [])],
    }


def generate_markdown_table(triage_results: list[dict]) -> str:
    """Generate a human-readable markdown triage table."""
    lines = [
        "# WPF Weekly Issue Triage Report",
        "",
        f"**Generated**: {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}",
        f"**Issues analyzed**: {len(triage_results)}",
        "",
        "| # | Title | Type | Component | Priority | Action | Reasoning |",
        "|---|-------|------|-----------|----------|--------|-----------|",
    ]

    for item in triage_results:
        num = item["number"]
        title = item["title"][:60] + ("..." if len(item["title"]) > 60 else "")
        lines.append(
            f"| [{num}](https://github.com/dotnet/wpf/issues/{num}) "
            f"| {title} "
            f"| {item['proposed_type']} "
            f"| {item['proposed_component']} "
            f"| {item['proposed_priority']} "
            f"| {item['proposed_action']} "
            f"| {item['reasoning']} |"
        )

    if not triage_results:
        lines.append("| — | No new issues in the past 7 days | — | — | — | — | — |")

    return "\n".join(lines)


def generate_json_block(triage_results: list[dict]) -> str:
    """Generate the JSON block for the email (editable by the team)."""
    # Strip unnecessary fields for the editable version
    editable = []
    for item in triage_results:
        editable.append({
            "number": item["number"],
            "title": item["title"],
            "proposed_type": item["proposed_type"],
            "proposed_component": item["proposed_component"],
            "proposed_priority": item["proposed_priority"],
            "proposed_action": item["proposed_action"],
        })
    return json.dumps(editable, indent=2)


def main():
    parser = argparse.ArgumentParser(description="Generate WPF issue triage report")
    parser.add_argument("--days", type=int, default=7, help="Number of days to look back (default: 7)")
    parser.add_argument("--output-dir", type=str, default=".", help="Output directory (default: current dir)")
    args = parser.parse_args()

    print(f"Fetching issues from the past {args.days} days...")
    issues = fetch_recent_issues(args.days)
    print(f"Found {len(issues)} issues")

    print("Classifying issues...")
    triage_results = [classify_issue(issue) for issue in issues]

    # Generate markdown report
    md_report = generate_markdown_table(triage_results)
    md_path = f"{args.output_dir}/triage-report.md"
    with open(md_path, "w", encoding="utf-8") as f:
        f.write(md_report)
    print(f"Written: {md_path}")

    # Generate JSON data
    json_data = generate_json_block(triage_results)
    json_path = f"{args.output_dir}/triage-data.json"
    with open(json_path, "w", encoding="utf-8") as f:
        f.write(json_data)
    print(f"Written: {json_path}")

    # Generate email body (table + JSON block)
    email_body_lines = [
        md_report,
        "",
        "---",
        "",
        "## Editable Triage Data",
        "",
        "To apply these labels, **reply to this email** with the JSON block below.",
        "Edit labels/priorities or remove rows for issues you want to skip.",
        "",
        "```json",
        json_data,
        "```",
    ]
    email_path = f"{args.output_dir}/triage-email-body.md"
    with open(email_path, "w", encoding="utf-8") as f:
        f.write("\n".join(email_body_lines))
    print(f"Written: {email_path}")

    # Print summary
    print(f"\nTriage summary: {len(triage_results)} issues classified")
    for item in triage_results:
        print(f"  #{item['number']}: {item['proposed_type']} / {item['proposed_component']} / {item['proposed_priority']}")


if __name__ == "__main__":
    main()
