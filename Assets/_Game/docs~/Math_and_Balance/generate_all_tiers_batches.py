import os
import math
from pathlib import Path

docs_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters")
mythic_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Math_and_Balance\Mythic_Batches")

tiers = ["Common", "R", "SR", "SSR", "UR"]

def clean_prompt_line(line):
    parts = line.split("**: ")
    if len(parts) > 1:
        return parts[1].strip()
    return ""

for tier in tiers:
    tier_docs = docs_dir / tier
    if not tier_docs.exists():
        continue
        
    chibi_out = mythic_dir / tier / "chibi"
    waist_out = mythic_dir / tier / "waist_up"
    general_out = mythic_dir / tier / "general"
    
    os.makedirs(chibi_out, exist_ok=True)
    os.makedirs(waist_out, exist_ok=True)
    os.makedirs(general_out, exist_ok=True)
    
    tier_data = []

    for md_file in tier_docs.glob("*.md"):
        name_str = md_file.name.replace(".md", "")
        parts = name_str.split("_")
        char_name = "_".join(parts[1:]).title()
        
        chibi_prompt = ""
        sprite_prompt = ""
        general_prompt = ""
        
        with open(md_file, "r", encoding="utf-8") as f:
            for line in f:
                if "**Chibi" in line:
                    chibi_prompt = clean_prompt_line(line)
                elif "**Sprite" in line:
                    sprite_prompt = clean_prompt_line(line)
                elif "**General / Normal" in line:
                    general_prompt = clean_prompt_line(line)
                    
        if chibi_prompt or sprite_prompt or general_prompt:
            tier_data.append({
                "name": char_name,
                "chibi": chibi_prompt,
                "sprite": sprite_prompt,
                "general": general_prompt
            })
            
    # Batch them in groups of 5
    batch_size = 5
    pages = math.ceil(len(tier_data) / batch_size)
    for i in range(pages):
        batch = tier_data[i * batch_size : (i + 1) * batch_size]
        
        chibi_lines = []
        sprite_lines = []
        general_lines = []
        
        for item in batch:
            chibi_lines.append(f"{item['name']} | 1:1 | {item['chibi']}")
            sprite_lines.append(f"{item['name']} | 3:4 | {item['sprite']}")
            general_lines.append(f"{item['name']} | {item['general']}")
            
        with open(chibi_out / f"Batch_{tier}_{i+1}.txt", "w", encoding="utf-8") as f:
            f.write("\n".join(chibi_lines) + "\n")
            
        with open(waist_out / f"Batch_{tier}_{i+1}.txt", "w", encoding="utf-8") as f:
            f.write("\n".join(sprite_lines) + "\n")
            
        with open(general_out / f"Batch_{tier}_{i+1}.txt", "w", encoding="utf-8") as f:
            f.write("\n".join(general_lines) + "\n")

print(f"Successfully generated all Chibi, Waist Up (Sprite), and General mythic batches across {len(tiers)} tiers!")
