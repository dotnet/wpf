import unittest

from test_evidence import FakeGitHub
from test_priority import impact_result, priority_evidence
from triage import TriageError, label_plan, prepare


def assessment_fixture():
    client = FakeGitHub()
    client.issue["body"] = "TextBox crashes on 11.0.0 when saving a document."
    evidence = prepare(client, 123, priority_evidence()["releases"])
    result = {
        **impact_result(), "input_sha256": evidence["input_sha256"], "area": "TextBox",
        "area_confidence": "HIGH", "area_quote": "TextBox crashes",
        "evidence_quote": client.issue["body"],
    }
    return client, evidence, result


class OutputTests(unittest.TestCase):
    def test_only_exact_area_and_high_priority_are_planned(self):
        _, evidence, result = assessment_fixture()
        plan = label_plan(result, evidence)
        self.assertEqual(plan["add_labels"], ["TextBox", "Priority:0"])
        self.assertFalse(plan["published"])
        self.assertNotIn("comment", plan)

    def test_unknown_fields_labels_and_stale_hash_fail(self):
        _, evidence, result = assessment_fixture()
        for change in ({"shell": "run"}, {"area": "new-area"}, {"input_sha256": "stale"},
                       {"confidence": []}, {"evidence_quote": "Invented evidence"}):
            with self.subTest(change=change), self.assertRaises(TriageError):
                label_plan({**result, **change}, evidence)

    def test_existing_labels_and_human_removals_protect_both_axes(self):
        _, evidence, result = assessment_fixture()
        for field in ("existing_labels", "human_removed_labels"):
            with self.subTest(field=field):
                current = {**evidence, field: ["RichTextBox", "Priority:3"]}
                self.assertEqual(label_plan(result, current)["add_labels"], [])

    def test_feature_or_external_classification_preserves_priority(self):
        _, evidence, result = assessment_fixture()
        for name in ("Feature Request", "External Non-WPF Issue", "Question"):
            current = {**evidence, "existing_labels": [name]}
            self.assertEqual(label_plan(result, current)["add_labels"], ["TextBox"])

    def test_unknown_and_truncated_assessments_add_no_labels(self):
        _, evidence, result = assessment_fixture()
        self.assertEqual(label_plan(result, {**evidence, "context_truncated": True})["add_labels"], [])
        result.update(area="None", area_confidence="NONE", area_quote="None", confidence="LOW")
        self.assertEqual(label_plan(result, evidence)["add_labels"], [])

    def test_version_cannot_be_invented_or_matched_as_a_prefix(self):
        _, evidence, result = assessment_fixture()
        for version in ("10.0.1", "11.0", "latest"):
            with self.subTest(version=version), self.assertRaises(TriageError):
                label_plan({**result, "affected_version": version}, evidence)
