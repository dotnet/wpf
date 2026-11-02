from pathlib import Path
import sys
import unittest


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from triage import TriageError, eligible, issue_kind


def human_issue():
    return {
        "number": 123,
        "repository_url": "https://api.github.com/repos/dotnet/wpf",
        "state": "open",
        "locked": False,
        "user": {"id": 456, "login": "reporter", "type": "User"},
        "labels": [],
        "body": "",
    }


class EligibilityTests(unittest.TestCase):
    def test_unlabeled_human_issue_is_eligible(self):
        issue = human_issue()
        issue["title"] = "A bot integration fails in my application"
        self.assertTrue(eligible(issue))

    def test_bots_and_service_accounts_are_ineligible_even_with_a_human_sender(self):
        authors = [
            {"id": 1, "login": "automation", "type": "Bot"},
            {"id": 1, "login": "automation[bot]", "type": "User"},
            {"id": 1, "login": "automation-bot", "type": "User"},
            {"id": 1, "login": "DotNet-Bot", "type": "User"},
        ]
        for author in authors:
            with self.subTest(author=author):
                issue = human_issue()
                issue["user"] = author
                issue["sender"] = human_issue()["user"]
                self.assertFalse(eligible(issue))

    def test_unknown_or_malformed_author_is_ineligible(self):
        for author in (None, {}, {"login": "reporter"}, {"id": 1, "login": "", "type": "User"},
                       {"id": True, "login": "reporter", "type": "User"}):
            with self.subTest(author=author):
                issue = human_issue()
                issue["user"] = author
                self.assertFalse(eligible(issue))

    def test_non_issue_or_ineligible_state_is_rejected(self):
        for change in (
            {"pull_request": {}}, {"state": "closed"}, {"locked": True},
            {"locked": None}, {"number": 0}, {"number": True},
            {"repository_url": "https://api.github.com/repos/other/repo"},
        ):
            with self.subTest(change=change):
                issue = human_issue()
                issue.update(change)
                self.assertFalse(eligible(issue))
        for value in (None, [], {}):
            with self.subTest(value=value):
                self.assertFalse(eligible(value))


class IssueKindTests(unittest.TestCase):
    def test_existing_type_labels_are_hints(self):
        for name, kind in (
            ("Bug", "BUG"), (":beetle: bug", "BUG"), ("Feature Request", "FEATURE"),
            ("API suggestion", "FEATURE"), ("Question", "OTHER"), ("Untriaged", "OTHER"),
        ):
            with self.subTest(name=name):
                issue = human_issue()
                issue["labels"] = [{"name": name}]
                self.assertEqual(issue_kind(issue), kind)

    def test_conflicting_type_labels_defer(self):
        for names in (["Bug", "Feature Request"], ["Bug", "External Non-WPF Issue"]):
            with self.subTest(names=names):
                issue = human_issue()
                issue["labels"] = [{"name": name} for name in names]
                self.assertEqual(issue_kind(issue), "OTHER")

    def test_unlabeled_wpf_forms_are_recognized(self):
        issue = human_issue()
        issue["body"] = "### Description\nExample\n### Reproduction Steps\nSteps\n### Expected behavior\nA\n### Actual behavior\nB"
        self.assertEqual(issue_kind(issue), "BUG")
        issue["body"] = "### Background and motivation\nExample\n### API Proposal\nAPI\n### API Usage\nUsage"
        self.assertEqual(issue_kind(issue), "FEATURE")
        issue["body"] += "\n### Description\nX\n### Reproduction Steps\nX\n### Expected behavior\nX\n### Actual behavior\nX"
        self.assertEqual(issue_kind(issue), "OTHER")

    def test_existing_type_takes_precedence_over_form_hint(self):
        issue = human_issue()
        issue["labels"] = [{"name": "Question"}]
        issue["body"] = "### Description\nX\n### Reproduction Steps\nX\n### Expected behavior\nX\n### Actual behavior\nX"
        self.assertEqual(issue_kind(issue), "OTHER")

    def test_malformed_labels_fail_instead_of_appearing_unlabeled(self):
        for labels in (None, {}, ["Bug"], [{"name": None}], [{"name": ""}]):
            with self.subTest(labels=labels):
                issue = human_issue()
                issue["labels"] = labels
                with self.assertRaises(TriageError):
                    issue_kind(issue)

    def test_invalid_body_is_not_treated_as_an_empty_report(self):
        for body in (False, 0, [], 42):
            with self.subTest(body=body):
                issue = human_issue()
                issue["body"] = body
                with self.assertRaises(TriageError):
                    issue_kind(issue)


if __name__ == "__main__":
    unittest.main()
