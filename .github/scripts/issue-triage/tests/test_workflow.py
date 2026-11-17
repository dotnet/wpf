from pathlib import Path
import unittest


class AnalysisWorkflowTests(unittest.TestCase):
    def test_analysis_and_framework_reporting_cannot_write_issues(self):
        workflows = Path(__file__).resolve().parents[3] / "workflows"
        compiled = (workflows / "issue-triage.lock.yml").read_text(encoding="utf-8")
        self.assertNotIn("issues: write", compiled)
        self.assertNotIn("pull-requests: write", compiled)
        self.assertNotIn("WPF_TRIAGE_PROJECT_TOKEN", compiled)

    def test_inference_depends_directly_on_the_live_eligibility_gate(self):
        workflows = Path(__file__).resolve().parents[3] / "workflows"
        compiled = (workflows / "issue-triage.lock.yml").read_text(encoding="utf-8")
        agent = compiled.split("  agent:\n", 1)[1].split("    runs-on:", 1)[0]
        self.assertIn("- pre_activation", agent)
        self.assertIn("needs.pre_activation.outputs.eligibility_result == 'success'", agent)
