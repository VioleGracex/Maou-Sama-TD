import os
from pathlib import Path
import math

docs_dir = Path("d:/OuikiDev/Maou-Sama-TD/Assets/_Game/docs~/characters/Common")
chibi_dir = Path("d:/OuikiDev/Maou-Sama-TD/Assets/_Game/docs~/Math_and_Balance/Mythic_Batches/Common/chibi")
waist_dir = Path("d:/OuikiDev/Maou-Sama-TD/Assets/_Game/docs~/Math_and_Balance/Mythic_Batches/Common/waist_up")

os.makedirs(chibi_dir, exist_ok=True)
os.makedirs(waist_dir, exist_ok=True)

prompts = []

for md_file in docs_dir.glob("*.md"):
    name_str = md_file.name.replace(".md", "")
    parts = name_str.split("_")
    # e.g. "01_drakmora_infantry" -> "Drakmora_Infantry"
    char_name = "_".join(parts[1:]).title()

    chibi_line = ""
    with open(md_file, "r", encoding="utf-8") as f:
        for line in f:
            if "Chibi" in line and "**" in line:
                # example line: - **Chibi (1:1 & 3:4)**: chibi style, white background...
                # We extract the content after the colon
                parts_colon = line.split("**: ")
                if len(parts_colon) > 1:
                    chibi_line = parts_colon[1].strip()
                break
    
    if chibi_line:
        prompts.append(f"{char_name} | 1:1 | {chibi_line}")

# Batch them in groups of 20
batch_size = 20
for i in range(math.ceil(len(prompts) / batch_size)):
    batch_prompts = prompts[i * batch_size : (i + 1) * batch_size]
    
    # Write chibi
    chibi_file = chibi_dir / f"Batch_Common_{i+1}.txt"
    with open(chibi_file, "w", encoding="utf-8") as f:
        f.write("\n".join(batch_prompts) + "\n")

    # Generate waist up prompts
    prefix_to_replace = (
        "chibi style, white background, standalone character design, "
        "cute big head small body chibi proportions, looking left, no shadow, "
        "full entire body fully in frame uncropped."
    )
    new_prefix = "white background, waist up, half body, standalone character design."
    
    waist_prompts = []
    for line in batch_prompts:
        line = line.replace("| 1:1 |", "| 3:4 |")
        line = line.replace(prefix_to_replace, new_prefix)
        # Ensure we add "fate ufotable anime style," if needed, or replace "anime style,"
        line = line.replace("anime style,", "fate ufotable anime style,")
        waist_prompts.append(line)
        
    waist_file = waist_dir / f"Batch_Common_{i+1}.txt"
    with open(waist_file, "w", encoding="utf-8") as f:
        f.write("\n".join(waist_prompts) + "\n")

print(f"Generated batches for {len(prompts)} Common characters.")
