import os
import re

base_dir = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Math_and_Balance\Mythic_Batches"
tiers = ["Common", "R", "SR", "SSR", "UR"]

prefix_to_replace = (
    r"chibi style, white background, standalone character design, "
    r"cute big head small body chibi proportions, looking left, no shadow, "
    r"full entire body fully in frame uncropped\."
)
new_prefix = "standalone character design, looking left, clear details, no shadow, no glow."

for tier in tiers:
    tier_dir = os.path.join(base_dir, tier)
    if not os.path.exists(tier_dir):
        continue
    
    chibi_dir = os.path.join(tier_dir, "chibi")
    general_dir = os.path.join(tier_dir, "general")
    
    if not os.path.exists(chibi_dir):
        continue
        
    os.makedirs(general_dir, exist_ok=True)
    
    for filename in os.listdir(chibi_dir):
        if not filename.endswith(".txt") or not filename.startswith("Batch_"):
            continue
            
        chibi_path = os.path.join(chibi_dir, filename)
        general_path = os.path.join(general_dir, filename)
        
        with open(chibi_path, "r", encoding="utf-8") as f:
            lines = f.readlines()
            
        new_lines = []
        for line in lines:
            line = line.strip()
            if not line:
                new_lines.append("")
                continue
                
            # Remove aspect ratio entirely to get "Name | description"
            line = line.replace(" | 1:1 | ", " | ")
            line = line.replace(" | 3:4 | ", " | ")
            
            # Replace prefix
            line = re.sub(prefix_to_replace, new_prefix, line)
            
            # Change "anime style" to "fate ufotable anime style" 
            if "fate ufotable anime style," not in line:
                line = line.replace("anime style,", "fate ufotable anime style,")
            
            new_lines.append(line)
            
        with open(general_path, "w", encoding="utf-8") as f:
            f.write("\n".join(new_lines) + "\n")
        
        print(f"Processed {filename} in {tier}/general")

print("Done generating general batches.")
