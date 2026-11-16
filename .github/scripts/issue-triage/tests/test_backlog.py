import copy
import unittest
from unittest.mock import Mock

from backlog import complete_coverage, inventory, issue_stamp, manifest_for, validate_manifest
from test_intake import human_issue
from triage import TriageError


class BacklogTests(unittest.TestCase):
    def test_inventory_is_not_limited_to_search_results(self):
        client = Mock()
        client.pages.return_value = [{**human_issue(), "number": number} for number in range(1, 1202)]
        client.pages.return_value.append({**human_issue(), "number": 1203, "user": {"type": "Bot"}})
        self.assertEqual(len(inventory(client)), 1201)
        self.assertEqual(client.pages.call_args.args, ("issues",))

    def test_duplicate_pages_do_not_claim_complete_inventory(self):
        client = Mock()
        client.pages.return_value = [human_issue(), human_issue()]
        with self.assertRaises(TriageError):
            inventory(client)

    def test_resume_keeps_only_unchanged_assessments(self):
        issue = human_issue()
        previous = manifest_for([issue], now=10)
        previous["entries"]["123"]["status"] = "assessed"
        resumed = manifest_for([issue], now=20, previous=previous)
        self.assertEqual(resumed["entries"]["123"]["status"], "assessed")
        changed = {**issue, "body": "Changed report"}
        self.assertEqual(manifest_for([changed], now=20, previous=previous)["entries"]["123"]["status"], "pending")

    def test_missing_failed_or_changed_issues_block_coverage(self):
        issue = human_issue()
        manifest = manifest_for([issue], now=10)
        for status in ("pending", "error", "stale"):
            manifest["entries"]["123"]["status"] = status
            with self.subTest(status=status), self.assertRaises(TriageError):
                complete_coverage(manifest, [issue], now=20)
        manifest["entries"]["123"]["status"] = "assessed"
        complete_coverage(manifest, [issue], now=20)
        with self.assertRaises(TriageError):
            complete_coverage(manifest, [issue, {**issue, "number": 124}], now=20)

    def test_expired_or_foreign_checkpoints_fail(self):
        manifest = manifest_for([human_issue()], now=10)
        for change in ({"expires_at": 11}, {"repository": "other/repo"}, {"schema": 2}):
            with self.subTest(change=change), self.assertRaises(TriageError):
                validate_manifest({**manifest, **change}, now=20)
