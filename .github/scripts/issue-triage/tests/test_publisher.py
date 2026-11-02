import copy
import unittest
from unittest.mock import patch

from publisher import publish_record
from test_evidence import FakeGitHub
from test_output import assessment_fixture
from test_priority import priority_evidence
from triage import TriageError, label_plan


class PublisherTests(unittest.TestCase):
    def setUp(self):
        releases = patch("priority.fetch_releases", return_value=priority_evidence()["releases"])
        self.addCleanup(releases.stop)
        releases.start()
        self.client, evidence, result = assessment_fixture()
        self.record = {**label_plan(result, evidence), "evidence": evidence}

    def test_preview_and_disabled_axes_never_mutate(self):
        for publish, area, priority in ((False, True, False), (True, False, False)):
            with self.subTest(publish=publish), patch.object(self.client, "add_labels", create=True) as write:
                publish_record(self.client, self.record, publish, area, priority)
                write.assert_not_called()

    def test_area_publication_is_additive_and_refingerprinted(self):
        def add(number, labels):
            self.client.issue["labels"].extend({"name": name} for name in labels)
        with patch.object(self.client, "add_labels", side_effect=add, create=True):
            result = publish_record(self.client, self.record, True, True, False)
        self.assertTrue(result["published"])
        self.assertEqual(result["published_labels"], ["TextBox"])
        self.assertEqual(result["add_labels"], ["Priority:0"])

    def test_stale_input_and_missing_priority_approval_stop_writes(self):
        with patch.object(self.client, "add_labels", create=True) as write:
            self.client.issue["body"] = "Changed"
            with self.assertRaises(TriageError):
                publish_record(self.client, self.record, True, True, False)
            write.assert_not_called()

    def test_ambiguous_mutation_is_not_retried(self):
        with patch.object(self.client, "add_labels", side_effect=TriageError("Ambiguous"), create=True) as write:
            with self.assertRaises(TriageError):
                publish_record(self.client, self.record, True, True, False)
            self.assertEqual(write.call_count, 1)

    def test_priority_requires_approval_before_any_axis_is_written(self):
        with patch("publisher.load_approval", side_effect=TriageError("No approval")), \
                patch.object(self.client, "add_labels", create=True) as write:
            with self.assertRaises(TriageError):
                publish_record(self.client, self.record, True, True, True)
            write.assert_not_called()
