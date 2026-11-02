import copy
import unittest

from project import reconcile
from test_project import project_fixture
from test_ranking import controlled, ranking_fixture
from triage import TriageError


class FakeProject:
    def __init__(self):
        self.members = [controlled(1, "Exclude"), controlled(2, "Pin")]
        self.members[0]["values"]["Selected"] = "Yes"
        self.writes = []
        self.fail_once = False

    def preflight(self):
        value = project_fixture()["organization"]["projectV2"]
        return {"id": value["id"], "fields": {field["name"]: field for field in value["fields"]["nodes"]}}

    def items(self, project_id):
        return copy.deepcopy(self.members)

    def add_item(self, project_id, content_id):
        number = int(content_id.split("-")[-1])
        item = controlled(number, "Auto")
        item["content"]["id"] = content_id
        self.members.append(item)
        self.writes.append(("add", number))
        return item["id"]

    def set_field(self, layout, item_id, name, value):
        item = next(item for item in self.members if item["id"] == item_id)
        self.assert_managed(name)
        if value is None:
            item["values"].pop(name, None)
        else:
            item["values"][name] = value
        self.writes.append((item_id, name))
        if self.fail_once:
            self.fail_once = False
            raise TriageError("Ambiguous write")

    @staticmethod
    def assert_managed(name):
        if name == "Selection control":
            raise AssertionError("Maintainer controls must survive")


class ReconcileTests(unittest.TestCase):
    def test_preview_has_no_writes_and_publication_preserves_overrides(self):
        issues, records = ranking_fixture(12)
        project = FakeProject()
        reconcile(project, issues, records)
        self.assertEqual(project.writes, [])
        reconcile(project, issues, records, True, lambda: None)
        selected = [item for item in project.members if item["values"].get("Selected") == "Yes"]
        self.assertEqual(len(selected), 10)
        self.assertEqual(project.members[0]["values"]["Selection control"], "Exclude")
        self.assertEqual(project.members[1]["values"]["Selection control"], "Pin")
        self.assertEqual(project.members[1]["values"]["Rank"], 1)

    def test_partial_write_is_not_retried_and_fresh_run_recovers(self):
        issues, records = ranking_fixture(3)
        project = FakeProject()
        project.fail_once = True
        with self.assertRaises(TriageError):
            reconcile(project, issues, records, True, lambda: None)
        self.assertEqual(len(project.writes), 1)
        reconcile(project, issues, records, True, lambda: None)
        self.assertEqual(sum(item["values"].get("Selected") == "Yes" for item in project.members), 2)

    def test_changed_controls_or_missing_live_check_prevent_writes(self):
        issues, records = ranking_fixture(3)
        project = FakeProject()
        with self.assertRaises(TriageError):
            reconcile(project, issues, records, True)

        def changed():
            project.members[1]["values"]["Selection control"] = "Exclude"

        with self.assertRaises(TriageError):
            reconcile(project, issues, records, True, changed)
        self.assertEqual(project.writes, [])

    def test_archived_selected_candidate_stops_before_writes(self):
        issues, records = ranking_fixture(3)
        project = FakeProject()
        project.members[1]["isArchived"] = True
        with self.assertRaises(TriageError):
            reconcile(project, issues, records, True, lambda: None)
        self.assertEqual(project.writes, [])
