from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

from backlog import assess_batch, manifest_for, trusted_run
from test_intake import human_issue
from triage import TriageError


class BatchTests(unittest.TestCase):
    def test_inventory_only_spends_no_credits_and_dispatches_nothing(self):
        client = Mock()
        manifest = manifest_for([human_issue()])
        with tempfile.TemporaryDirectory() as directory:
            assess_batch(client, manifest, 0, 0, Path(directory) / "manifest.json")
        client.dispatch.assert_not_called()
        self.assertEqual(manifest["credits_reserved"], 0)

    def test_batch_budget_includes_previous_reservations(self):
        manifest = manifest_for([human_issue()])
        manifest["credits_reserved"] = 30
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(TriageError):
                assess_batch(Mock(), manifest, 1, 59, Path(directory) / "manifest.json")

    def test_ambiguous_dispatch_records_error_without_retry(self):
        client = Mock()
        client.dispatch.side_effect = TriageError("Ambiguous dispatch")
        manifest = manifest_for([human_issue()])
        with tempfile.TemporaryDirectory() as directory:
            assess_batch(client, manifest, 1, 30, Path(directory) / "manifest.json")
            assess_batch(client, manifest, 1, 30, Path(directory) / "manifest.json")
        self.assertEqual(client.dispatch.call_count, 1)
        self.assertEqual(manifest["entries"]["123"]["status"], "error")

    def test_foreign_failed_or_non_main_runs_are_not_trusted(self):
        client = Mock()
        base = {
            "head_branch": "main", "conclusion": "success", "event": "workflow_dispatch",
            "head_repository": {"full_name": "dotnet/wpf"},
            "path": ".github/workflows/issue-triage.lock.yml",
        }
        for change in ({"head_branch": "topic"}, {"conclusion": "failure"}, {"event": "pull_request"}):
            client.get.return_value = {**base, **change}
            with self.subTest(change=change), self.assertRaises(TriageError):
                trusted_run(client, 1, "issue-triage.lock.yml")
