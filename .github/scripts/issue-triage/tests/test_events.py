import unittest

from test_evidence import FakeGitHub
from triage import event_eligible


class EventTests(unittest.TestCase):
    def test_manual_dispatch_cannot_enroll_a_bot_authored_issue(self):
        client = FakeGitHub()
        client.issue["user"]["type"] = "Bot"
        event = {"repository": {"full_name": "dotnet/wpf"}, "inputs": {"issue_number": "123"}}
        self.assertFalse(event_eligible(client, event))
        self.assertEqual(client.reads, ["issues/123"])

    def test_only_human_author_comments_retrigger_analysis(self):
        client = FakeGitHub()
        event = {"repository": {"full_name": "dotnet/wpf"}, "issue": client.issue}
        for author, expected in (
            (client.issue["user"], True),
            ({"id": 999, "type": "User"}, False),
            ({"id": 456, "type": "Bot"}, False),
        ):
            with self.subTest(author=author):
                self.assertEqual(event_eligible(client, {**event, "comment": {"user": author}}), expected)
