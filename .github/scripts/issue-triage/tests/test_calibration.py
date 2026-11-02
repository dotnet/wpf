import unittest
import time
from unittest.mock import Mock

from backlog import calibration_approval, manifest_for, require_review_environment, validated_assessments
from priority import enforce_limits, projected_counts
from test_intake import human_issue
from test_output import assessment_fixture
from triage import TriageError, label_plan


def reviewed_fixture():
    client, evidence, result = assessment_fixture()
    record = {**label_plan(result, evidence), "evidence": evidence, "assessed_at": int(time.time()), "run_id": "1"}
    manifest = manifest_for([client.issue])
    manifest["entries"]["123"].update(status="assessed", assessment=record)
    return client.issue, manifest


class CalibrationTests(unittest.TestCase):
    def test_complete_current_records_can_be_counted_for_review(self):
        issue, manifest = reviewed_fixture()
        self.assertEqual(list(validated_assessments(manifest, [issue])), [123])
        manifest["entries"]["123"]["assessment"]["assessed_at"] = 0
        with self.assertRaises(TriageError):
            validated_assessments(manifest, [issue])

    def test_exact_limits_pass_and_next_item_blocks(self):
        enforce_limits({"Priority:0": 20, "Priority:1": 50})
        for counts in ({"Priority:0": 21, "Priority:1": 50}, {"Priority:0": 20, "Priority:1": 51}):
            with self.subTest(counts=counts), self.assertRaises(TriageError):
                enforce_limits(counts)

    def test_existing_labels_and_candidates_are_not_double_counted(self):
        issue = {**human_issue(), "labels": [{"name": "Priority:0"}]}
        bot = {**human_issue(), "number": 124, "user": {"type": "Bot"}, "labels": issue["labels"]}
        self.assertEqual(projected_counts([issue, bot], {123: ["Priority:0"]}), {"Priority:0": 1, "Priority:1": 0})

    def test_unprotected_or_unapproved_environment_cannot_authorize(self):
        client = Mock()
        client.get.return_value = {"protection_rules": []}
        with self.assertRaises(TriageError):
            require_review_environment(client, 1)
        client.get.side_effect = [
            {"protection_rules": [{"type": "required_reviewers", "prevent_self_review": True}]},
            [],
        ]
        with self.assertRaises(TriageError):
            require_review_environment(client, 1)

    def test_required_environment_review_is_recognized(self):
        client = Mock()
        client.get.side_effect = [
            {"protection_rules": [{"type": "required_reviewers", "prevent_self_review": True}]},
            [{"state": "approved", "environments": [{"name": "wpf-triage-priority"}]}],
        ]
        require_review_environment(client, 1)
