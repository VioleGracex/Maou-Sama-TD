import time
import xml.etree.ElementTree as ET
import os
from PySide6.QtCore import QObject

class ReportLogger(QObject):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.report_log_file = None
        self.test_cases = {}
        self.scenario_name = ""
        self.start_time = 0

    def initialize(self, log_path, scenario_name):
        self.report_log_file = log_path
        self.scenario_name = scenario_name
        self.start_time = time.time()
        self.test_cases = {}
        try:
            os.makedirs(os.path.dirname(self.report_log_file), exist_ok=True)
            with open(self.report_log_file, "w", encoding="utf-8") as f:
                f.write(f"=== Sylvan-HUD Test Report: {scenario_name} ===\n")
                f.write(f"Started At: {time.strftime('%Y-%m-%d %H:%M:%S')}\n\n")
        except Exception:
            pass

    def log(self, step, result, message):
        if self.report_log_file:
            try:
                with open(self.report_log_file, "a", encoding="utf-8") as f:
                    f.write(f"[{time.strftime('%Y-%m-%d %H:%M:%S')}] [{step}] [{result}] {message}\n")
            except Exception:
                pass
        
        now = time.time()
        if step not in self.test_cases:
            self.test_cases[step] = {
                "name": step,
                "status": result,
                "messages": [message],
                "start_time": now,
                "end_time": now
            }
        else:
            self.test_cases[step]["end_time"] = now
            self.test_cases[step]["messages"].append(message)
            if result == "FAIL":
                self.test_cases[step]["status"] = "FAIL"
            elif result == "PASS" and self.test_cases[step]["status"] == "STARTING":
                self.test_cases[step]["status"] = "PASS"

    def write_xml_report(self, xml_path):
        try:
            total_time = time.time() - self.start_time
            testsuites = ET.Element("testsuites", name="Sylvan Game Salavan Suite", time=f"{total_time:.3f}")
            
            failures = sum(1 for tc in self.test_cases.values() if tc["status"] == "FAIL")
            testsuite = ET.SubElement(
                testsuites, "testsuite",
                name=self.scenario_name,
                tests=str(len(self.test_cases)),
                failures=str(failures),
                time=f"{total_time:.3f}"
            )
            
            for tc_name, tc in self.test_cases.items():
                tc_duration = tc["end_time"] - tc["start_time"]
                testcase = ET.SubElement(
                    testsuite, "testcase",
                    classname=self.scenario_name,
                    name=tc_name,
                    time=f"{tc_duration:.3f}"
                )
                
                log_content = "\n".join(tc["messages"])
                system_out = ET.SubElement(testcase, "system-out")
                system_out.text = log_content
                
                if tc["status"] == "FAIL":
                    failure = ET.SubElement(testcase, "failure", message=f"Step {tc_name} failed.")
                    failure.text = log_content
                    
            tree = ET.ElementTree(testsuites)
            if hasattr(ET, "indent"):
                ET.indent(tree, space="  ")
            tree.write(xml_path, encoding="utf-8", xml_declaration=True)
        except Exception:
            pass
