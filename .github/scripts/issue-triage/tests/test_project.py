import unittest
from unittest.mock import patch

from project import FIELD_OPTIONS, FIELD_TYPES, Project
from triage import TriageError


def project_fixture():
    fields = [{
        "id": name, "name": name, "dataType": kind,
        "options": [{"id": option, "name": option} for option in sorted(FIELD_OPTIONS.get(name, set()))],
    } for name, kind in FIELD_TYPES.items()]
    return {"organization": {"projectV2": {
        "id": "project-fixture", "public": False, "closed": False,
        "fields": {"nodes": fields, "pageInfo": {"hasNextPage": False, "endCursor": None}},
    }}}


class ProjectAccessTests(unittest.TestCase):
    def test_exact_private_schema_passes(self):
        project = Project(1, "fixture-token")
        with patch.object(project, "query", return_value=project_fixture()):
            self.assertEqual(len(project.preflight()["fields"]), 5)

    def test_public_closed_missing_or_ambiguous_schema_fails(self):
        project = Project(1, "fixture-token")
        for mutation in ("public", "closed", "missing", "duplicate", "option"):
            data = project_fixture()
            value = data["organization"]["projectV2"]
            if mutation in {"public", "closed"}:
                value[mutation] = True
            elif mutation == "missing":
                value["fields"]["nodes"].pop()
            elif mutation == "duplicate":
                value["fields"]["nodes"].append(value["fields"]["nodes"][0])
            else:
                value["fields"]["nodes"][0]["options"].append({"name": "Unexpected", "id": "bad"})
            with self.subTest(mutation=mutation), patch.object(project, "query", return_value=data):
                with self.assertRaises(TriageError):
                    project.preflight()

    def test_missing_credential_does_not_fall_back_to_issue_token(self):
        with patch.dict("os.environ", {"GITHUB_TOKEN": "fixture-token"}, clear=True):
            with self.assertRaises(TriageError):
                Project(1)

    def test_items_paginate_without_returning_partial_fields(self):
        project = Project(1, "fixture-token")
        first = {"node": {"public": False, "closed": False, "items": {
            "nodes": [], "pageInfo": {"hasNextPage": True, "endCursor": "next"},
        }}}
        last = {"node": {"public": False, "closed": False, "items": {
            "nodes": [], "pageInfo": {"hasNextPage": False, "endCursor": None},
        }}}
        with patch.object(project, "query", side_effect=[first, last]) as query:
            self.assertEqual(project.items("project-fixture"), [])
            self.assertEqual(query.call_args_list[1].args[1]["cursor"], "next")
        first["node"]["items"]["nodes"] = [{
            "id": "item", "fieldValues": {"nodes": [], "pageInfo": {"hasNextPage": True}},
        }]
        with patch.object(project, "query", return_value=first), self.assertRaises(TriageError):
            project.items("project-fixture")

    def test_transport_errors_do_not_expose_private_details(self):
        project = Project(1, "fixture-token")
        with patch.object(project.opener, "open", side_effect=ValueError("private selection")):
            with self.assertRaises(TriageError) as error:
                project.query("query { viewer { login } }", {})
        self.assertNotIn("private selection", str(error.exception))
