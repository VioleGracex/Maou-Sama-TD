import os
import glob
from pathlib import Path

docs_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Characters")
art_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\art\Characters")
data_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\data\Units\Vassals")

# 1. Build the list of valid "Title_Case" character names from docs
valid_chars = set()
for filepath in docs_dir.rglob("*.md"):
    if "Archive" in filepath.parts:
        continue
    # example: 01_abyssal_grunt.md -> Abyssal_Grunt
    filename = filepath.stem
    # strip numbers prefix
    name_part = filename.split("_", 1)[-1] if len(filename.split("_", 1)) > 1 and filename.split("_", 1)[0].isdigit() else filename
    # Title Case conversion
    words = name_part.split("_")
    title_case = "_".join([w.title() for w in words])
    valid_chars.add(title_case)

print(f"Loaded {len(valid_chars)} valid characters from docs.")

# 2. Scan Art Directories
orphaned_art = []
for tier_dir in art_dir.iterdir():
    if not tier_dir.is_dir() or tier_dir.name in ["Archive", "Shade"]:
        continue
    
    for char_dir in tier_dir.iterdir():
        if not char_dir.is_dir():
            continue
            
        if char_dir.name not in valid_chars:
            orphaned_art.append(str(char_dir))

# 3. Scan Data Directories
orphaned_data = []
for tier_dir in data_dir.iterdir():
    if not tier_dir.is_dir() or tier_dir.name in ["Archive", "Shade"]:
        continue
        
    for asset_file in tier_dir.rglob("*.asset"):
        # Format usually: Char_Name_UnitData.asset
        base_name = asset_file.stem
        # Try to extract the name
        core_name = base_name.replace("Char_", "").replace("_UnitData", "")
        if core_name not in valid_chars:
            orphaned_data.append(str(asset_file))

print(f"\nOrphaned Art Folders Found: {len(orphaned_art)}")
for p in orphaned_art:
    print(p)

print(f"\nOrphaned Data Assets Found: {len(orphaned_data)}")
for p in orphaned_data:
    print(p)
