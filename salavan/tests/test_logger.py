import os
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

# Ensure salavan package is in path for imports
current_dir = os.path.dirname(os.path.abspath(__file__))
parent_dir = os.path.dirname(current_dir)
if parent_dir not in sys.path:
    sys.path.insert(0, parent_dir)

from logger import ReportLogger

class TestReportLogger(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.log_path = os.path.join(self.temp_dir.name, "test_log.txt")
        self.xml_path = os.path.join(self.temp_dir.name, "junit_report.xml")

    def tearDown(self):
        self.temp_dir.cleanup()

    def test_logger_flow(self):
        logger = ReportLogger()
        logger.initialize(self.log_path, "TestScenario")
        
        logger.log("Step1", "STARTING", "Begin test sequence")
        logger.log("Step1", "PASS", "Step 1 passed successfully")
        logger.log("Step2", "FAIL", "Assertion failed at Step 2")
        
        # Verify plaintext log exists
        self.assertTrue(os.path.exists(self.log_path))
        with open(self.log_path, "r", encoding="utf-8") as f:
            content = f.read()
            self.assertIn("=== Sylvan-HUD Test Report: TestScenario ===", content)
            self.assertIn("[Step1] [STARTING] Begin test sequence", content)
            self.assertIn("[Step2] [FAIL] Assertion failed at Step 2", content)
            
        # Write JUnit XML report
        logger.write_xml_report(self.xml_path)
        self.assertTrue(os.path.exists(self.xml_path))
        
        # Parse XML report and verify tags
        tree = ET.parse(self.xml_path)
        root = tree.getroot()
        self.assertEqual(root.tag, "testsuites")
        
        suite = root.find("testsuite")
        self.assertIsNotNone(suite)
        self.assertEqual(suite.get("name"), "TestScenario")
        self.assertEqual(suite.get("tests"), "2")
        self.assertEqual(suite.get("failures"), "1")
        
        cases = suite.findall("testcase")
        self.assertEqual(len(cases), 2)
        
        step1_case = next(c for c in cases if c.get("name") == "Step1")
        self.assertIn("Begin test sequence", step1_case.find("system-out").text)
        
        step2_case = next(c for c in cases if c.get("name") == "Step2")
        self.assertEqual(step2_case.find("failure").get("message"), "Step Step2 failed.")

if __name__ == "__main__":
    unittest.main()
