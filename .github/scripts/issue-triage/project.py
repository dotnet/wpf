"""Private WPF project access. Never log responses, field values, or credentials."""

import argparse
import json
import os
import urllib.request

from triage import FEATURE_LABELS, OTHER_LABELS, GitHub, NoRedirect, REPOSITORY, TriageError, digest, eligible, label_names


FIELD_TYPES = {
    "Selection control": "SINGLE_SELECT", "Selected": "SINGLE_SELECT",
    "Rank": "NUMBER", "Priority": "SINGLE_SELECT", "Selection reason": "TEXT",
}
FIELD_OPTIONS = {
    "Selection control": {"Auto", "Pin", "Exclude"},
    "Selected": {"Yes", "No"}, "Priority": {"P0", "P1"},
}


def issue_items(items: list[dict]) -> dict[int, dict]:
    result = {}
    for item in items:
        content = item.get("content") or {}
        if content.get("repository", {}).get("nameWithOwner") != "dotnet/wpf":
            continue
        number = content["number"]
        if number in result:
            raise TriageError("Duplicate WPF project membership needs private review")
        control = item["values"].get("Selection control", "Auto")
        if control not in FIELD_OPTIONS["Selection control"]:
            raise TriageError("Private selection control is invalid")
        result[number] = item
    return result


def shortlist(issues: list[dict], records: dict[int, dict], items: list[dict]) -> list[dict]:
    """Rank complete, previously validated assessment coverage without API calls."""
    controls = issue_items(items)
    if any(issue["number"] not in records for issue in issues if eligible(issue)):
        raise TriageError("Complete current assessment coverage is required")
    ranked = []
    for issue in issues:
        if not eligible(issue):
            continue
        number = issue["number"]
        control = controls.get(number, {}).get("values", {}).get("Selection control", "Auto")
        record, labels = records[number], label_names(issue)
        if (
            control == "Exclude" or record["result"]["issue_kind"] != "BUG"
            or labels & (FEATURE_LABELS | OTHER_LABELS)
        ):
            continue
        priorities = {name for name in labels if name.startswith("Priority:")}
        if len(priorities) > 1 or priorities - {"Priority:0", "Priority:1", "Priority:2", "Priority:3"}:
            continue
        priority = next(iter(priorities), None)
        if priority is None:
            priority = next((name for name in record["add_labels"] if name in {"Priority:0", "Priority:1"}), None)
        impact = record["result"]["impact"]
        evidenced = record["result"]["confidence"] == "HIGH" and not record["evidence"]["context_truncated"]
        impact_rank = {"CRASH": 3, "REGRESSION": 2, "BROKEN_FUNCTIONALITY": 1}.get(impact, 0) if evidenced else 0
        compatibility = bool(labels & {"tenet-compatibility", "regression"}) or (evidenced and impact == "REGRESSION")
        reactions = issue.get("reactions", {}).get("+1", 0)
        if type(reactions) is not int or reactions < 0:
            raise TriageError("Issue reaction evidence is unavailable")
        key = (control != "Pin", {"Priority:0": 0, "Priority:1": 1}.get(priority, 2),
               -impact_rank, -int(compatibility), -reactions, number)
        ranked.append({
            "number": number, "node_id": issue.get("node_id"), "key": key,
            "priority": {"Priority:0": "P0", "Priority:1": "P1"}.get(priority),
            "reason": f"{'Pinned; ' if control == 'Pin' else ''}{priority or 'Unprioritized'}; "
                      f"{impact if evidenced else 'impact deferred'}; thumbs-up {reactions}",
        })
    if sum(not item["key"][0] for item in ranked) > 10:
        raise TriageError("More than ten eligible pins require private review")
    return [
        {name: value for name, value in item.items() if name != "key"} | {"rank": rank}
        for rank, item in enumerate(sorted(ranked, key=lambda item: item["key"])[:10], 1)
    ]


def control_snapshot(items: list[dict]) -> list[tuple]:
    return sorted((
        item["id"], item["isArchived"], (item.get("content") or {}).get("id"),
        (item.get("content") or {}).get("number"),
        ((item.get("content") or {}).get("repository") or {}).get("nameWithOwner"),
        item["values"].get("Selection control", "Auto"),
    ) for item in items)


def reconcile(project, issues: list[dict], records: dict[int, dict], publish: bool = False, verify_public=None) -> None:
    layout = project.preflight()
    items = project.items(layout["id"])
    members = issue_items(items)
    chosen = shortlist(issues, records, items)
    desired = {item["number"]: item for item in chosen}
    if any(item["isArchived"] for number, item in members.items() if number in desired):
        raise TriageError("An archived selection needs private review")
    if any(item["values"].get("Selected") == "Yes" and item["id"] not in {
        member["id"] for member in members.values()
    } for item in items):
        raise TriageError("Unexpected selected project content needs private review")
    if not publish:
        return
    if verify_public is None:
        raise TriageError("Live public evidence verification is required")
    expected = control_snapshot(items)

    def guard():
        verify_public()
        if project.preflight() != layout or control_snapshot(project.items(layout["id"])) != expected:
            raise TriageError("Private controls changed; inspect partial writes")

    for number, item in members.items():
        if number not in desired and item["values"].get("Selected") == "Yes":
            guard()
            project.set_field(layout, item["id"], "Selected", "No")
    for number, choice in desired.items():
        item = members.get(number)
        if item is None:
            if not isinstance(choice["node_id"], str) or not choice["node_id"]:
                raise TriageError("Public issue node identity is unavailable")
            guard()
            item_id = project.add_item(layout["id"], choice["node_id"])
            item = {
                "id": item_id, "isArchived": False, "values": {},
                "content": {"id": choice["node_id"], "number": number,
                            "repository": {"nameWithOwner": "dotnet/wpf"}},
            }
            items.append(item)
            expected = control_snapshot(items)
        for field, value in (
            ("Rank", choice["rank"]), ("Priority", choice["priority"]),
            ("Selection reason", choice["reason"]), ("Selected", "Yes"),
        ):
            if item["values"].get(field) != value:
                guard()
                project.set_field(layout, item["id"], field, value)
    guard()
    final = project.items(layout["id"])
    selected = issue_items([item for item in final if item["values"].get("Selected") == "Yes"])
    if set(selected) != set(desired):
        raise TriageError("Private shortlist membership read-back failed")
    for number, choice in desired.items():
        values = selected[number]["values"]
        if (
            values.get("Rank") != choice["rank"] or values.get("Priority") != choice["priority"]
            or values.get("Selection reason") != choice["reason"] or selected[number]["isArchived"]
        ):
            raise TriageError("Private shortlist field read-back failed")


class Project:
    def __init__(self, number: int, token: str | None = None):
        self.token = token or os.environ.get("WPF_TRIAGE_PROJECT_TOKEN")
        if type(number) is not int or number <= 0 or not self.token:
            raise TriageError("Private project setup is required")
        self.number = number
        self.opener = urllib.request.build_opener(NoRedirect)

    def query(self, query: str, variables: dict) -> dict:
        request = urllib.request.Request(
            "https://api.github.com/graphql", method="POST",
            data=json.dumps({"query": query, "variables": variables}).encode(),
            headers={"Authorization": f"Bearer {self.token}", "Content-Type": "application/json"},
        )
        try:
            with self.opener.open(request, timeout=30) as response:
                body = response.read(8 * 1024 * 1024 + 1)
            if len(body) > 8 * 1024 * 1024:
                raise TriageError("Private project response exceeds the read limit")
            result = json.loads(body)
            if result.get("errors") or not isinstance(result.get("data"), dict):
                raise TriageError("Private project operation was not confirmed")
            return result["data"]
        except (OSError, ValueError):
            raise TriageError("Private project operation failed; inspect privately before retrying") from None

    def preflight(self) -> dict:
        query = """
        query($number:Int!,$cursor:String) {
          organization(login:"dotnet") { projectV2(number:$number) {
            id public closed fields(first:100,after:$cursor) {
              pageInfo { hasNextPage endCursor }
              nodes {
                ... on ProjectV2Field { id name dataType }
                ... on ProjectV2SingleSelectField { id name dataType options { id name } }
              }
            }
          } }
        }"""
        fields, cursor, seen = {}, None, set()
        for _ in range(20):
            data = self.query(query, {"number": self.number, "cursor": cursor})
            project = (data.get("organization") or {}).get("projectV2") or {}
            if project.get("public") is not False or project.get("closed") is not False:
                raise TriageError("Project must be accessible, open, and private")
            for field in project["fields"]["nodes"]:
                name = field.get("name")
                if name in FIELD_TYPES:
                    if name in fields:
                        raise TriageError("Private project field names are ambiguous")
                    fields[name] = field
            page = project["fields"]["pageInfo"]
            if not page["hasNextPage"]:
                break
            cursor = page["endCursor"]
            if not cursor or cursor in seen:
                raise TriageError("Private project field pagination is incomplete")
            seen.add(cursor)
        else:
            raise TriageError("Private project field limit reached")
        for name, kind in FIELD_TYPES.items():
            field = fields.get(name, {})
            if field.get("dataType") != kind or not field.get("id"):
                raise TriageError("Required private project schema is missing")
            if name in FIELD_OPTIONS:
                options = field.get("options", [])
                if {item.get("name") for item in options} != FIELD_OPTIONS[name] or len(options) != len(FIELD_OPTIONS[name]):
                    raise TriageError("Private project field options do not match")
        return {"id": project["id"], "fields": fields}

    def items(self, project_id: str) -> list[dict]:
        query = """
        query($id:ID!,$cursor:String) { node(id:$id) { ... on ProjectV2 {
          public closed items(first:100,after:$cursor) {
            pageInfo { hasNextPage endCursor }
            nodes { id isArchived
              content { ... on Issue { id number state repository { nameWithOwner } } }
              fieldValues(first:100) {
                pageInfo { hasNextPage }
                nodes {
                  ... on ProjectV2ItemFieldSingleSelectValue { name field { ... on ProjectV2SingleSelectField { name } } }
                  ... on ProjectV2ItemFieldNumberValue { number field { ... on ProjectV2Field { name } } }
                  ... on ProjectV2ItemFieldTextValue { text field { ... on ProjectV2Field { name } } }
                }
              }
            }
          }
        } } }"""
        items, cursor, seen = [], None, set()
        for _ in range(100):
            project = self.query(query, {"id": project_id, "cursor": cursor}).get("node") or {}
            if project.get("public") is not False or project.get("closed") is not False:
                raise TriageError("Private project visibility changed")
            for item in project["items"]["nodes"]:
                if item["fieldValues"]["pageInfo"]["hasNextPage"]:
                    raise TriageError("Private item field coverage is incomplete")
                values = {}
                for value in item["fieldValues"]["nodes"]:
                    field = (value.get("field") or {}).get("name")
                    if field in FIELD_TYPES:
                        values[field] = value.get("name", value.get("number", value.get("text")))
                items.append({**item, "values": values})
            page = project["items"]["pageInfo"]
            if not page["hasNextPage"]:
                return items
            cursor = page["endCursor"]
            if not cursor or cursor in seen:
                raise TriageError("Private item pagination is incomplete")
            seen.add(cursor)
        raise TriageError("Private project item limit reached")

    def add_item(self, project_id: str, content_id: str) -> str:
        data = self.query("""
        mutation($project:ID!,$content:ID!) {
          addProjectV2ItemById(input:{projectId:$project,contentId:$content}) { item { id } }
        }""", {"project": project_id, "content": content_id})
        item_id = data.get("addProjectV2ItemById", {}).get("item", {}).get("id")
        if not isinstance(item_id, str) or not item_id:
            raise TriageError("Private membership mutation was not confirmed")
        return item_id

    def set_field(self, layout: dict, item_id: str, name: str, value) -> None:
        if name not in {"Selected", "Rank", "Priority", "Selection reason"}:
            raise TriageError("Maintainer controls cannot be changed by the publisher")
        variables = {"project": layout["id"], "item": item_id, "field": layout["fields"][name]["id"]}
        if value is None:
            operation = "clearProjectV2ItemFieldValue"
            query = """
            mutation($project:ID!,$item:ID!,$field:ID!) {
              clearProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$field}) {
                projectV2Item { id }
              }
            }"""
        else:
            field = layout["fields"][name]
            if name in FIELD_OPTIONS:
                matches = [option["id"] for option in field["options"] if option["name"] == value]
                if len(matches) != 1:
                    raise TriageError("Private selection option is unavailable")
                payload = {"singleSelectOptionId": matches[0]}
            elif name == "Rank":
                if type(value) is not int or not 1 <= value <= 10:
                    raise TriageError("Private rank must be within the shortlist")
                payload = {"number": value}
            else:
                payload = {"text": value}
            variables["value"] = payload
            operation = "updateProjectV2ItemFieldValue"
            query = """
            mutation($project:ID!,$item:ID!,$field:ID!,$value:ProjectV2FieldValue!) {
              updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$field,value:$value}) {
                projectV2Item { id }
              }
            }"""
        data = self.query(query, variables)
        if data.get(operation, {}).get("projectV2Item", {}).get("id") != item_id:
            raise TriageError("Private field mutation was not confirmed")


def main(argv: list[str] | None = None) -> int:
    from backlog import inventory, load_approval, resolve_coverage

    parser = argparse.ArgumentParser(description="Private WPF project refresh")
    parser.add_argument("--publish", choices=("true", "false"), default="false")
    args = parser.parse_args(argv)
    if os.environ.get("GITHUB_REPOSITORY") != REPOSITORY or os.environ.get("GITHUB_REF") != "refs/heads/main":
        raise TriageError("Private refresh must use trusted main")
    client = GitHub()
    approval = load_approval(client, int(os.environ.get("WPF_TRIAGE_APPROVAL_RUN", "0")))
    issues = inventory(client)
    records = resolve_coverage(client, approval, issues)
    expected = digest(issues)

    def verify_public():
        if digest(inventory(client)) != expected:
            raise TriageError("Public issue evidence changed during private refresh")

    project = Project(int(os.environ.get("WPF_TRIAGE_PROJECT_NUMBER", "0")))
    reconcile(project, issues, records, args.publish == "true", verify_public)
    print("Private WPF project refresh passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, KeyError, TypeError, OSError):
        raise SystemExit("Private WPF project refresh stopped; review setup, coverage, or partial writes privately.")
