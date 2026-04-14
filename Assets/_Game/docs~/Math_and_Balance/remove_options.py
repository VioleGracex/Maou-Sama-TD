import os
import glob
from pathlib import Path

base_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Characters")

for file_path in base_dir.rglob("*.md"):
    if "Archive" in file_path.parts:
        continue
        
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    if "waist-up & full body options, " in content:
        content = content.replace("waist-up & full body options, ", "")
        
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(content)

print("Removed 'waist-up & full body options' from all files.")
