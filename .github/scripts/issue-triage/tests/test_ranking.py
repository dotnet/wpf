import unittest

from project import shortlist
from test_intake import human_issue
from triage import TriageError


def ranking_fixture(count=12):
    issues = [{**human_issue(), "number": n, "node_id": f"issue-{n}", "reactions": {"+1": 0}}
              for n in range(1, count + 1)]
    records = {issue["number"]: {
        "result": {"issue_kind": "BUG", "confidence": "HIGH", "impact": "BROKEN_FUNCTIONALITY"},
        "evidence": {"context_truncated": False}, "add_labels": [],
    } for issue in issues}
    return issues, records


def controlled(number, control):
    return {"id": f"item-{number}", "isArchived": False,
            "content": {"number": number, "repository": {"nameWithOwner": "dotnet/wpf"}},
            "values": {"Selection control": control}}


class RankingTests(unittest.TestCase):
    def test_capacity_stable_tie_pins_and_excludes(self):
        issues, records = ranking_fixture()
        selected = shortlist(issues, records, [controlled(12, "Pin"), controlled(1, "Exclude")])
        self.assertEqual([item["number"] for item in selected], [12, 2, 3, 4, 5, 6, 7, 8, 9, 10])
        self.assertEqual([item["rank"] for item in selected], list(range(1, 11)))

    def test_priority_impact_compatibility_and_reactions(self):
        issues, records = ranking_fixture(6)
        issues[5]["labels"] = [{"name": "Priority:0"}]
        records[5]["add_labels"] = ["Priority:1"]
        records[4]["result"]["impact"] = "CRASH"
        issues[2]["labels"] = [{"name": "tenet-compatibility"}]
        issues[1]["reactions"]["+1"] = 20
        self.assertEqual([item["number"] for item in shortlist(issues, records, [])], [6, 5, 4, 3, 2, 1])

    def test_pin_cannot_enroll_bots_closed_nonbugs_or_conflicts(self):
        issues, records = ranking_fixture(5)
        issues[0]["user"] = {**issues[0]["user"], "type": "Bot"}
        issues[1]["state"] = "closed"
        records[3]["result"]["issue_kind"] = "FEATURE"
        issues[3]["labels"] = [{"name": "Question"}, {"name": "Bug"}]
        issues[4]["labels"] = [{"name": "Priority:0"}, {"name": "Priority:1"}]
        self.assertEqual(shortlist(issues, records, [controlled(n, "Pin") for n in range(1, 6)]), [])

    def test_missing_coverage_excess_pins_and_duplicate_membership_stop(self):
        issues, records = ranking_fixture()
        for items, assessments in [
            ([], {}), ([controlled(n, "Pin") for n in range(1, 12)], records),
            ([controlled(1, "Pin"), controlled(1, "Exclude")], records),
        ]:
            with self.subTest(items=items), self.assertRaises(TriageError):
                shortlist(issues, assessments, items)
