from pathlib import Path
import unittest


class AnalysisWorkflowTests(unittest.TestCase):
    def test_analysis_and_framework_reporting_cannot_write_issues(self):
        workflows = Path(__file__).resolve().parents[3] / "workflows"
        compiled = (workflows / "issue-triage.lock.yml").read_text(encoding="utf-8")
        self.assertNotIn("issues: write", compiled)
        self.assertNotIn("pull-requests: write", compiled)
        self.assertNotIn("WPF_TRIAGE_PROJECT_TOKEN", compiled)
