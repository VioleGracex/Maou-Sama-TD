import os
import glob
import re

common_dir = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Characters\Common"

for filepath in glob.glob(os.path.join(common_dir, "*.md")):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    # Check if race is Skeleton
    if "**Race**: Skeleton" in content or "**Race**: Undead" in content:
        # We want to remove the specific hair strings from the lines
        # Like "Short bob Blonde hair, "
        
        # We will also remove the Hair line entirely
        content = re.sub(r"- \*\*Hair\*\*:.*?\n", "", content)
        
        # We will remove hair strings from the prompts
        content = re.sub(r"[a-zA-Z\s]+ hair, ", "", content)
        
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(content)

print("Fixed skeleton hair in Common tier.")
