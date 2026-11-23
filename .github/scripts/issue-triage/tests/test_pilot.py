import copy
import time
import unittest
from unittest.mock import MagicMock, patch

from backlog import manifest_for, resolve_coverage
from project import Project, reconcile
from publisher import publish_record
from test_output import assessment_fixture
from test_priority import priority_evidence
from test_reconcile import FakeProject
from triage import GitHub, TriageError, label_plan, prepare


class PilotTests(unittest.TestCase):
    def setUp(self):
        releases = patch("priority.fetch_releases", return_value=priority_evidence()["releases"])
        self.addCleanup(releases.stop)
        releases.start()

    def test_assessment_area_write_delta_and_private_selection(self):
        client, _, result = assessment_fixture()
        client.issue["node_id"] = "issue-123"
        evidence = prepare(client, 123)
        result["input_sha256"] = evidence["input_sha256"]
        record = {**label_plan(result, evidence), "evidence": evidence, "assessed_at": int(time.time())}
        baseline = manifest_for([client.issue])
        baseline["entries"]["123"].update(status="assessed", assessment=copy.deepcopy(record))

        def add(number, labels):
            client.issue["labels"].extend({"name": name} for name in labels)

        with patch.object(client, "add_labels", side_effect=add, create=True):
            published = publish_record(client, record, True, True, False)
        records = resolve_coverage(client, {"manifest": baseline}, [client.issue], (published,))
        project = FakeProject()
        project.members = []
        reconcile(project, [client.issue], records, True, lambda: None)
        self.assertEqual(project.members[0]["values"]["Selected"], "Yes")
        self.assertEqual(project.members[0]["values"]["Priority"], "P0")
        self.assertEqual([label["name"] for label in client.issue["labels"]], ["TextBox"])

    def test_full_projected_overflow_blocks_both_label_axes(self):
        client, _, template = assessment_fixture()
        issues, records = [], {}
        for number in range(1, 22):
            client.issue["number"] = number
            evidence = prepare(client, number)
            result = {**template, "input_sha256": evidence["input_sha256"]}
            records[number] = {**label_plan(result, evidence), "evidence": evidence, "assessed_at": int(time.time())}
            issues.append(copy.deepcopy(client.issue))
        baseline = manifest_for(issues)
        for number, record in records.items():
            baseline["entries"][str(number)].update(status="assessed", assessment=record)
        with patch("publisher.load_approval", return_value={"manifest": baseline}), \
                patch("publisher.inventory", return_value=issues), \
                patch.object(client, "add_labels", create=True) as write:
            with self.assertRaises(TriageError):
                publish_record(client, records[21], True, True, True, 1)
            write.assert_not_called()

    def test_human_removals_survive_a_high_confidence_reassessment(self):
        client, _, template = assessment_fixture()
        client.timeline = [
            {"event": "unlabeled", "actor": {"type": "User"}, "label": {"name": name}}
            for name in ("TextBox", "Priority:0")
        ]
        evidence = prepare(client, 123)
        record = {
            **label_plan({**template, "input_sha256": evidence["input_sha256"]}, evidence),
            "evidence": evidence, "assessed_at": int(time.time()),
        }
        with patch.object(client, "add_labels", create=True) as write:
            self.assertEqual(publish_record(client, record, True, True, True)["add_labels"], [])
            write.assert_not_called()

    def test_last_moment_drift_and_expiration_stop_before_mutation(self):
        client, evidence, result = assessment_fixture()
        record = {**label_plan(result, evidence), "evidence": evidence, "assessed_at": int(time.time())}
        with patch("publisher.prepare", side_effect=[evidence, {**evidence, "title": "Changed"}]), \
                patch.object(client, "add_labels", create=True) as write:
            with self.assertRaises(TriageError):
                publish_record(client, record, True, True, False)
            write.assert_not_called()
        with self.assertRaises(TriageError):
            publish_record(client, {**record, "assessed_at": 0}, True, True, False)

    def test_project_writer_cannot_change_private_maintainer_control(self):
        project = Project(1, "fixture-token")
        with patch.object(project, "query") as query, self.assertRaises(TriageError):
            project.set_field({}, "item", "Selection control", "Exclude")
        query.assert_not_called()

    def test_public_transport_uses_the_supplied_job_credential(self):
        client = GitHub("fixture-token")
        response = MagicMock()
        response.__enter__.return_value.read.return_value = b"[]"
        with patch.object(client.opener, "open", return_value=response) as request:
            client.get("labels")
        self.assertEqual(request.call_args.args[0].get_header("Authorization"), "Bearer fixture-token")
