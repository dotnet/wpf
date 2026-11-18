import unittest

from publisher import publication_controls
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
