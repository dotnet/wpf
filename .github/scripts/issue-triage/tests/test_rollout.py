import unittest

from publisher import publication_controls
from project import board_publication
from triage import TriageError


class RolloutTests(unittest.TestCase):
    def test_automatic_area_requires_enable_and_pilot_acceptance(self):
        for enabled, approved in ((False, False), (True, False), (False, True)):
            self.assertEqual(publication_controls("workflow_run", False, enabled, True, approved), (False, False, False))
        self.assertEqual(publication_controls("workflow_run", False, True, True, True), (True, True, False))

    def test_manual_pilot_is_explicit_and_never_implicit(self):
        self.assertEqual(publication_controls("workflow_dispatch", False, True, False), (False, True, False))
        self.assertEqual(publication_controls("workflow_dispatch", True, True, False), (True, True, False))
        with self.assertRaises(TriageError):
            publication_controls("pull_request", True, True, True, True)

    def test_priority_and_area_have_independent_automatic_controls(self):
        self.assertEqual(publication_controls("workflow_run", False, False, True, True, False), (False, False, False))
        self.assertEqual(publication_controls("workflow_run", False, False, True, True, True), (True, False, True))
        self.assertEqual(publication_controls("workflow_run", False, True, True, True, True), (True, True, True))

    def test_scheduled_board_requires_both_controls_and_a_write_request(self):
        for requested, enabled, approved in ((False, True, True), (True, False, True), (True, True, False)):
            self.assertFalse(board_publication("schedule", requested, enabled, approved))
        self.assertTrue(board_publication("schedule", True, True, True))
        self.assertTrue(board_publication("workflow_dispatch", True, True, False))
        self.assertFalse(board_publication("workflow_dispatch", False, True, True))
        with self.assertRaises(TriageError):
            board_publication("pull_request", True, True, True)
