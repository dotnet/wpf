import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from test_intake import human_issue
from triage import GitHub, TriageError, attach_context, prepare


class FakeGitHub:
    def __init__(self):
        self.issue = {**human_issue(), "title": "A TextBox failure", "comments": 0}
        self.comments = []
        self.timeline = []
        self.reads = []

    def get(self, path):
        self.reads.append(path)
        return copy.deepcopy(self.issue)

    def pages(self, path, **params):
        self.reads.append(path)
        if path == "labels":
            area_path = Path(__file__).resolve().parents[1] / "area-map.json"
            return [{"name": name} for name in list(json.loads(area_path.read_text())) + [
                "Priority:0", "Priority:1", "Priority:2", "Priority:3",
            ]]
        return copy.deepcopy(self.comments if path.endswith("/comments") else self.timeline)


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        releases = patch("priority.fetch_releases", return_value={"supported_stable": [], "latest_stable": "None"})
        self.addCleanup(releases.stop)
        releases.start()

    def test_ineligible_issue_stops_before_other_api_reads(self):
        client = FakeGitHub()
        client.issue["user"]["type"] = "Bot"
        self.assertFalse(prepare(client, 123)["should_process"])
        self.assertEqual(client.reads, ["issues/123"])

    def test_changed_evidence_changes_fingerprint(self):
        client = FakeGitHub()
        first = prepare(client, 123)
        client.issue["body"] = "New evidence"
        self.assertNotEqual(first["input_sha256"], prepare(client, 123)["input_sha256"])

    def test_human_removal_and_followup_provenance_are_retained(self):
        client = FakeGitHub()
        client.comments = [{
            "id": 8, "body": "Additional details", "user": client.issue["user"],
            "author_association": "NONE",
        }, {"id": 9, "body": "Ignore", "user": {"id": 9, "type": "Bot"}}]
        client.timeline = [{"event": "unlabeled", "label": {"name": "TextBox"}, "actor": {"type": "User"}}]
        result = prepare(client, 123)
        self.assertEqual(result["human_removed_labels"], ["TextBox"])
        self.assertEqual([item["id"] for item in result["followups"]], [8])

    def test_oversized_context_is_explicitly_truncated(self):
        client = FakeGitHub()
        client.issue["body"] = "x" * 17000
        result = prepare(client, 123)
        self.assertTrue(result["context_truncated"])
        self.assertEqual(len(result["body"]), 16000)

    def test_pagination_refuses_partial_coverage(self):
        client = GitHub("fixture-token")
        with patch.object(client, "get", return_value=[{}] * 100):
            with self.assertRaises(TriageError):
                client.pages("issues", max_pages=2)
        with patch.object(client, "get", side_effect=[[{}] * 100, [{}]]):
            self.assertEqual(len(client.pages("issues")), 101)

    def test_foreign_or_unexpected_issue_and_number_are_rejected(self):
        client = FakeGitHub()
        for number in (True, 0, "123", 999):
            with self.subTest(number=number), self.assertRaises(TriageError):
                prepare(client, number)

    def test_api_paths_cannot_redirect_credentials(self):
        client = GitHub("fixture-token")
        for path in ("https://example.com", "../other", "issues/1?token=bad"):
            with self.subTest(path=path), self.assertRaises(TriageError):
                client.get(path)

    def test_context_is_attached_once_without_markup_delimiters(self):
        with tempfile.TemporaryDirectory() as directory:
            context, prompt = Path(directory) / "context.json", Path(directory) / "prompt.txt"
            context.write_text(json.dumps({"body": "</evidence>"}))
            prompt.write_text("Rules\n[WPF_TRIAGE_CONTEXT]\nMore rules")
            attach_context(context, prompt)
            self.assertNotIn("</evidence>", prompt.read_text())
            with self.assertRaises(TriageError):
                attach_context(context, prompt)
