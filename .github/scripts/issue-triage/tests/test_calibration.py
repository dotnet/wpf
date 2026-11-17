import copy
import json
import unittest
import time
from unittest.mock import Mock, patch

from backlog import RETENTION_SECONDS, calibration_approval, load_approval, manifest_for, require_review_environment, validated_assessments
from priority import enforce_limits, projected_counts
from test_intake import human_issue
from test_output import assessment_fixture
from triage import TriageError, digest, label_plan


def reviewed_fixture():
    client, evidence, result = assessment_fixture()
    record = {**label_plan(result, evidence), "evidence": evidence, "assessed_at": int(time.time()), "run_id": "1"}
    manifest = manifest_for([client.issue])
    manifest["entries"]["123"].update(status="assessed", assessment=record)
    return client.issue, manifest


def approved_client():
    client = Mock()
    client.get.side_effect = [
        {"protection_rules": [{"type": "required_reviewers", "prevent_self_review": True}]},
        [{"state": "approved", "environments": [{"name": "wpf-triage-priority"}]}],
    ] * 2
    return client


class CalibrationTests(unittest.TestCase):
    def test_approval_creation_and_loading_preserve_the_reviewed_manifest(self):
        issue, manifest = reviewed_fixture()
        client = approved_client()
        approval = calibration_approval(client, manifest, [issue], 42)
        artifact = json.loads(json.dumps(approval))
        with patch("backlog.artifact_json", return_value=artifact) as download:
            loaded = load_approval(client, 42)
        download.assert_called_once_with(
            client, 42, "issue-triage-calibrate.yml", "wpf-approval", "approval.json",
        )
        self.assertEqual(loaded, approval)
        self.assertEqual(loaded["manifest"], manifest)
        self.assertEqual(loaded["manifest_sha256"], digest(manifest))
        self.assertEqual(loaded["projected_counts"], {"Priority:0": 1, "Priority:1": 0})
        self.assertEqual(client.get.call_args_list, [
            unittest.mock.call("environments/wpf-triage-priority"),
            unittest.mock.call("actions/runs/42/approvals"),
        ] * 2)

    def test_loading_rejects_altered_manifest_hash_policy_or_run(self):
        issue, manifest = reviewed_fixture()
        approval = calibration_approval(approved_client(), manifest, [issue], 42)
        altered_manifest = copy.deepcopy(manifest)
        altered_manifest["entries"]["123"]["status"] = "pending"
        for change in (
            {"manifest": altered_manifest},
            {"manifest_sha256": "unreviewed"},
            {"policy_sha256": "old-policy"},
            {"approval_run_id": 43},
        ):
            with self.subTest(change=change), \
                    patch("backlog.artifact_json", return_value={**approval, **change}), \
                    self.assertRaisesRegex(TriageError, "does not match"):
                load_approval(approved_client(), 42)

    def test_loading_rejects_an_expired_manifest_even_with_a_matching_hash(self):
        issue, manifest = reviewed_fixture()
        approval = calibration_approval(approved_client(), manifest, [issue], 42)
        manifest["created_at"] -= RETENTION_SECONDS + 1
        manifest["expires_at"] -= RETENTION_SECONDS + 1
        approval["manifest_sha256"] = digest(manifest)
        with patch("backlog.artifact_json", return_value=approval), \
                self.assertRaisesRegex(TriageError, "Invalid or expired"):
            load_approval(approved_client(), 42)

    def test_creation_and_loading_require_environment_approval(self):
        issue, manifest = reviewed_fixture()
        approval = calibration_approval(approved_client(), manifest, [issue], 42)
        for operation in ("create", "load"):
            client = Mock()
            client.get.side_effect = [
                {"protection_rules": [{"type": "required_reviewers", "prevent_self_review": True}]},
                [],
            ]
            with self.subTest(operation=operation), \
                    patch("backlog.artifact_json", return_value=approval), \
                    self.assertRaisesRegex(TriageError, "lacks an environment approval"):
                if operation == "create":
                    calibration_approval(client, manifest, [issue], 42)
                else:
                    load_approval(client, 42)

    def test_creation_enforces_projected_limits_before_requesting_approval(self):
        for priority, count in (("Priority:0", 21), ("Priority:1", 51)):
            issues = [{**human_issue(), "number": number} for number in range(1, count + 1)]
            records = {issue["number"]: {"add_labels": [priority]} for issue in issues}
            client = Mock()
            with self.subTest(priority=priority), \
                    patch("backlog.validated_assessments", return_value=records), \
                    self.assertRaisesRegex(TriageError, "exceed 20/50"):
                calibration_approval(client, manifest_for(issues), issues, 42)
            client.get.assert_not_called()

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
