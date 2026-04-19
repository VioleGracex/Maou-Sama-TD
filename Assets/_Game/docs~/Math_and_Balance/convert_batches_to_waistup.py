import os
import re
import shutil

base_dir = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Math_and_Balance\Mythic_Batches"
tiers = ["R", "SR", "SSR", "UR"]

prefix_to_replace = (
    r"chibi style, white background, standalone character design, "
    r"cute big head small body chibi proportions, looking left, no shadow, "
    r"full entire body fully in frame uncropped\."
)
new_prefix = "white background, waist up, half body, standalone character design."

for tier in tiers:
    tier_dir = os.path.join(base_dir, tier)
    if not os.path.exists(tier_dir):
        continue
    
    chibi_dir = os.path.join(tier_dir, "chibi")
    waist_up_dir = os.path.join(tier_dir, "waist_up")
    
    os.makedirs(chibi_dir, exist_ok=True)
    os.makedirs(waist_up_dir, exist_ok=True)
    
    for filename in os.listdir(tier_dir):
        if not filename.endswith(".txt") or not filename.startswith("Batch_"):
            continue
            
        file_path = os.path.join(tier_dir, filename)
        chibi_path = os.path.join(chibi_dir, filename)
        waist_up_path = os.path.join(waist_up_dir, filename)
        
        # 1. Move to chibi folder
        shutil.move(file_path, chibi_path)
        
        # 2. Read chibi path to generate waist up
        with open(chibi_path, "r", encoding="utf-8") as f:
            lines = f.readlines()
            
        new_lines = []
        for line in lines:
            line = line.strip()
            if not line:
                new_lines.append("")
                continue
                
            # Replace aspect ratio
            line = line.replace("| 1:1 |", "| 3:4 |")
            
            # Replace prefix
            line = re.sub(prefix_to_replace, new_prefix, line)
            
            # Change "anime style" to "fate ufotable anime style" 
            # if we didn't specify it in the prefix or just replace it at the end
            line = line.replace("anime style,", "fate ufotable anime style,")
            
            new_lines.append(line)
            
        # 3. Save to waist_up folder
        with open(waist_up_path, "w", encoding="utf-8") as f:
            f.write("\n".join(new_lines) + "\n")
        
        print(f"Processed {filename} in {tier}")

print("Done generating waist_up batches.")
