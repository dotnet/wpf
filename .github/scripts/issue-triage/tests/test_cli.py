import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from test_output import assessment_fixture
from triage import TriageError, agent_result, event_number, main, read_json


class CommandTests(unittest.TestCase):
    def test_native_output_has_exactly_one_allowed_operation(self):
        self.assertEqual(agent_result({"items": [{"type": "assess_wpf_issue", "area": "None"}]}), {"area": "None"})
        for value in ({"items": []}, {"items": [{"type": "comment"}]}, {"items": [{}, {}]}):
            with self.subTest(value=value), self.assertRaises(TriageError):
                agent_result(value)
    def test_preview_uses_no_api_client_and_writes_only_the_requested_file(self):
        _, evidence, result = assessment_fixture()
        with tempfile.TemporaryDirectory() as directory:
            paths = [Path(directory) / name for name in ("context.json", "result.json", "plan.json")]
            paths[0].write_text(json.dumps(evidence))
            paths[1].write_text(json.dumps(result))
            with patch("triage.GitHub", side_effect=AssertionError("No API in offline preview")):
                self.assertEqual(main(["preview", *map(str, paths)]), 0)
            self.assertFalse(read_json(paths[2])["published"])

    def test_manual_number_and_repository_are_checked(self):
        for value in ("0", "-1", "1.5", "1; command", True):
            event = {"repository": {"full_name": "dotnet/wpf"}, "inputs": {"issue_number": value}}
            with self.subTest(value=value), self.assertRaises(TriageError):
                event_number(event)
        with self.assertRaises(TriageError):
            event_number({"repository": {"full_name": "other/repo"}, "inputs": {"issue_number": "1"}})

    def test_bounded_file_read_rejects_excess_data(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "data.json"
            path.write_text('"long input"')
            with self.assertRaises(TriageError):
                read_json(path, limit=3)
