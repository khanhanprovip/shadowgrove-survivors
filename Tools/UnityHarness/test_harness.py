import tempfile
from pathlib import Path
import sys
import unittest
from harness import assess, invoke


class HarnessGates(unittest.TestCase):
    def result(self, report='PASS | works\nRuntime errors: 0\n', log='', code=0, timeout=False):
        return assess(report, log, {'exitCode': code, 'timedOut': timeout})['passed']

    def test_healthy_report(self): self.assertTrue(self.result())
    def test_fail_assertion(self): self.assertFalse(self.result('PASS | init\nFAIL | wrong\nRuntime errors: 0\n'))
    def test_runtime_failure_despite_passes(self): self.assertFalse(self.result('PASS | works\nRuntime errors: 650\n'))
    def test_nonzero_exit_despite_passes(self): self.assertFalse(self.result(code=1))
    def test_missing_report(self): self.assertFalse(self.result(''))
    def test_truncated_report(self): self.assertFalse(self.result('PASS | started'))
    def test_ambiguous_report(self): self.assertFalse(self.result('PASS | x\nRuntime errors: 0\nRuntime errors: 1\n'))
    def test_raw_exception_before_collector(self): self.assertFalse(self.result(log='NullReferenceException: bootstrap failed\n'))
    def test_timeout_gate(self): self.assertFalse(self.result(timeout=True))
    def test_legacy_summary(self):
        self.assertTrue(assess('PASS | ready\n== SO LOI/EXCEPTION: 0 ==', '',
                               {'exitCode': 0, 'timedOut': False}, legacy=True)['passed'])
    def test_real_process_timeout(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = invoke([sys.executable, '-c', 'import time; time.sleep(30)'], Path(tmp) / 'child.log', .2)
            self.assertTrue(result['timedOut'])
            self.assertEqual(result['exitCode'], 124)


if __name__ == '__main__': unittest.main()
