import os
import re

base_path = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters"

classes = {}
files_found = 0

for root, dirs, files in os.walk(base_path):
    if "Archive" in root:
        continue
    for file in files:
        if file.endswith(".md") and file != "CHARACTERS_MASTER_TIER_LIST.md" and file != "CHARACTERS_MASTER_TIER_LIST_archive.md" and file != "RACE_VISUAL_GUIDE.md" and file != "Tina_Profile.md":
            files_found += 1
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
                
            # Find Name and Class
            name_match = re.search(r"# Vassal:\s*(.*)", content)
            class_match = re.search(r"\*\*Class\*\*:\s*(.*)", content)
            
            name = name_match.group(1).strip() if name_match else file
            cls = class_match.group(1).strip() if class_match else "Unknown"
            
            classes[cls] = classes.get(cls, []) + [f"{name} ({file})"]

print(f"Total markdown files checked: {files_found}")
print("\nClass Distribution:")
for cls, units in sorted(classes.items()):
    print(f"\n{cls} ({len(units)} units):")
    for u in sorted(units):
        print(f"  - {u}")
