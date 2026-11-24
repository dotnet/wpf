import copy
import unittest
from unittest.mock import Mock, patch

from backlog import resolve_coverage
from test_calibration import reviewed_fixture
from test_priority import priority_evidence
from triage import TriageError


class RegistryTests(unittest.TestCase):
    def setUp(self):
        releases = patch("priority.fetch_releases", return_value=priority_evidence()["releases"])
        self.addCleanup(releases.stop)
        releases.start()
        self.issue, self.manifest = reviewed_fixture()
        self.client = Mock()
        self.client.get.return_value = {"workflow_runs": []}

    def test_current_baseline_needs_no_delta_reads(self):
        records = resolve_coverage(self.client, {"manifest": self.manifest}, [self.issue])
        self.assertEqual(list(records), [123])
        self.client.get.assert_not_called()

    def test_trusted_delta_repairs_a_changed_baseline(self):
        record = copy.deepcopy(self.manifest["entries"]["123"]["assessment"])
        record["run_id"] = "9"
        self.manifest["entries"]["123"]["issue_sha256"] = "old"
        self.client.get.return_value = {"workflow_runs": [{"id": 9}]}
        with patch("backlog.artifact_json", return_value=record):
            self.assertIn(123, resolve_coverage(self.client, {"manifest": self.manifest}, [self.issue]))

    def test_stale_expired_or_foreign_deltas_cannot_fill_coverage(self):
        record = copy.deepcopy(self.manifest["entries"]["123"]["assessment"])
        self.manifest["entries"]["123"]["issue_sha256"] = "old"
        self.client.get.return_value = {"workflow_runs": [{"id": 9}]}
        for change in ({"run_id": "8"}, {"run_id": "9", "assessed_at": 0}):
            with self.subTest(change=change), patch("backlog.artifact_json", return_value={**record, **change}):
                with self.assertRaises(TriageError):
                    resolve_coverage(self.client, {"manifest": self.manifest}, [self.issue])

    def test_publisher_does_not_make_old_assessments_immortal(self):
        self.manifest["expires_at"] = self.manifest["created_at"] - 1
        with self.assertRaises(TriageError):
            resolve_coverage(self.client, {"manifest": self.manifest}, [self.issue])
