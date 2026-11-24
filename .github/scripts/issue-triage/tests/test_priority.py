import unittest

from priority import candidate_priority, release_summary
from triage import TriageError


def priority_evidence():
    return {
        "issue_kind": "BUG", "context_truncated": False,
        "releases": {"supported_stable": ["10.0.1", "11.0.0"], "latest_stable": "11.0.0"},
    }


def impact_result():
    return {
        "issue_kind": "BUG", "confidence": "HIGH", "scope": "CORE",
        "workaround": "NONE_REPORTED", "affected_version": "11.0.0", "impact": "CRASH",
    }


class PriorityTests(unittest.TestCase):
    def test_latest_stable_crash_or_regression_can_be_p0(self):
        for impact in ("CRASH", "REGRESSION"):
            result = {**impact_result(), "impact": impact}
            self.assertEqual(candidate_priority(result, priority_evidence()), "Priority:0")

    def test_other_supported_critical_failure_can_be_p1(self):
        result = {**impact_result(), "affected_version": "10.0.1"}
        self.assertEqual(candidate_priority(result, priority_evidence()), "Priority:1")
        result.update(impact="BROKEN_FUNCTIONALITY", affected_version="11.0.0")
        self.assertEqual(candidate_priority(result, priority_evidence()), "Priority:1")

    def test_uncertainty_or_lower_impact_never_creates_p2_or_p3(self):
        for change in (
            {"scope": "NARROW"}, {"scope": "UNKNOWN"}, {"confidence": "LOW"},
            {"workaround": "VIABLE"}, {"workaround": "UNKNOWN"}, {"impact": "OTHER"},
            {"issue_kind": "FEATURE"}, {"affected_version": "latest"},
            {"affected_version": "11.0.0-preview.1"}, {"affected_version": "9.0.0"},
        ):
            with self.subTest(change=change):
                self.assertIsNone(candidate_priority({**impact_result(), **change}, priority_evidence()))

    def test_truncation_and_feature_intake_defer(self):
        for change in ({"context_truncated": True}, {"issue_kind": "FEATURE"}):
            with self.subTest(change=change):
                self.assertIsNone(candidate_priority(impact_result(), {**priority_evidence(), **change}))

    def test_release_order_is_numeric_and_excludes_preview_and_eol(self):
        metadata = {"releases-index": [
            {"latest-release": "9.0.9", "support-phase": "active"},
            {"latest-release": "10.0.1", "support-phase": "active"},
            {"latest-release": "11.0.0-preview.1", "support-phase": "active"},
            {"latest-release": "8.0.99", "support-phase": "eol"},
        ]}
        self.assertEqual(release_summary(metadata)["latest_stable"], "10.0.1")
        self.assertEqual(len(release_summary(metadata)["supported_stable"]), 2)

    def test_missing_stable_metadata_is_an_error_not_an_assumption(self):
        with self.assertRaises(TriageError):
            release_summary({})
