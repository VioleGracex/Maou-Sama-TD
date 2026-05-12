import os
import re

base_dir = r"d:\OuikiDev\Maou-Sama-TD"

# 1. Rename and modify markdown documentation profile
old_md_path = os.path.join(base_dir, "Assets", "_Game", "docs~", "characters", "SSR", "11_shade_the_phantom_stalker.md")
new_md_path = os.path.join(base_dir, "Assets", "_Game", "docs~", "characters", "SSR", "11_vaelin_the_phantom_stalker.md")

if os.path.exists(old_md_path):
    with open(old_md_path, "r", encoding="utf-8") as f:
        md_content = f.read()
    
    # Replace content
    md_content = md_content.replace("Shade, The Phantom Stalker", "Vaelin, The Phantom Stalker")
    md_content = md_content.replace("Shade, The Phantom", "Vaelin, The Phantom")
    md_content = md_content.replace("Vassal: Shade", "Vassal: Vaelin")
    md_content = md_content.replace("Shade was once", "Vaelin was once")
    md_content = md_content.replace("Shade, The Phantom Stalker", "Vaelin, The Phantom Stalker")
    md_content = md_content.replace("Shade_The_Phantom_Stalker", "Vaelin_The_Phantom_Stalker")
    md_content = md_content.replace("Shade", "Vaelin")
    md_content = md_content.replace("shade", "vaelin")
    
    with open(new_md_path, "w", encoding="utf-8") as f:
        f.write(md_content)
    
    os.remove(old_md_path)
    print(f"Renamed and updated markdown profile: {os.path.basename(new_md_path)}")

# 2. Rename and modify UnitData asset
old_asset_path = os.path.join(base_dir, "Assets", "_Game", "Data", "Units", "Vassals", "05_SSR", "Char_Shade_UnitData.asset")
new_asset_path = os.path.join(base_dir, "Assets", "_Game", "Data", "Units", "Vassals", "05_SSR", "Char_Vaelin_UnitData.asset")

if os.path.exists(old_asset_path):
    with open(old_asset_path, "r", encoding="utf-8") as f:
        asset_content = f.read()
        
    asset_content = asset_content.replace("m_Name: Char_Shade_UnitData", "m_Name: Char_Vaelin_UnitData")
    asset_content = asset_content.replace("UnitName: Shade", "UnitName: Vaelin")
    
    with open(new_asset_path, "w", encoding="utf-8") as f:
        f.write(asset_content)
        
    os.remove(old_asset_path)
    
    # Also rename meta file
    old_meta = old_asset_path + ".meta"
    new_meta = new_asset_path + ".meta"
    if os.path.exists(old_meta):
        os.rename(old_meta, new_meta)
        
    print("Renamed and updated UnitData asset and its meta.")

# 3. Rename Art directories and contents
old_art_dir = os.path.join(base_dir, "Assets", "_Game", "Art", "Characters", "05_SSR", "Shade")
new_art_dir = os.path.join(base_dir, "Assets", "_Game", "Art", "Characters", "05_SSR", "Vaelin")

if os.path.exists(old_art_dir):
    os.makedirs(new_art_dir, exist_ok=True)
    
    # Rename files inside Shade directory to Vaelin
    for filename in os.listdir(old_art_dir):
        old_file_path = os.path.join(old_art_dir, filename)
        new_filename = filename.replace("Shade", "Vaelin")
        new_file_path = os.path.join(new_art_dir, new_filename)
        
        # Rename physical file
        os.rename(old_file_path, new_file_path)
        print(f"  Renamed art file: {filename} -> {new_filename}")
        
    os.rmdir(old_art_dir)
    
    # Also rename the directory meta file
    old_dir_meta = old_art_dir + ".meta"
    new_dir_meta = new_art_dir + ".meta"
    if os.path.exists(old_dir_meta):
        os.rename(old_dir_meta, new_dir_meta)
        
    print("Renamed Art folder and all contents.")

# 4. Update Balancing_PowerGrid.csv
csv_path = os.path.join(base_dir, "Assets", "_Game", "docs~", "Math_and_Balance", "Balancing_PowerGrid.csv")
if os.path.exists(csv_path):
    with open(csv_path, "r", encoding="utf-8") as f:
        csv_content = f.read()
        
    csv_content = csv_content.replace("11_shade_the_phantom_stalker.md", "11_vaelin_the_phantom_stalker.md")
    csv_content = csv_content.replace('"Shade, The Phantom Stalker"', '"Vaelin, The Phantom Stalker"')
    
    with open(csv_path, "w", encoding="utf-8") as f:
        f.write(csv_content)
        
    print("Updated Balancing_PowerGrid.csv row for Vaelin.")

# 5. Update Batch SSR text files
batch_files = [
    os.path.join(base_dir, "Assets", "_Game", "docs~", "Math_and_Balance", "Mythic_Batches", "SSR", "waist_up", "Batch_SSR_3.txt"),
    os.path.join(base_dir, "Assets", "_Game", "docs~", "Math_and_Balance", "Mythic_Batches", "SSR", "general", "Batch_SSR_3.txt"),
    os.path.join(base_dir, "Assets", "_Game", "docs~", "Math_and_Balance", "Mythic_Batches", "SSR", "chibi", "Batch_SSR_3.txt")
]

for batch_file in batch_files:
    if os.path.exists(batch_file):
        with open(batch_file, "r", encoding="utf-8") as f:
            b_content = f.read()
            
        b_content = b_content.replace("Shade_The_Phantom_Stalker", "Vaelin_The_Phantom_Stalker")
        b_content = b_content.replace("Shade, The Phantom Stalker", "Vaelin, The Phantom Stalker")
        b_content = b_content.replace("Shade, The Phantom", "Vaelin, The Phantom")
        
        with open(batch_file, "w", encoding="utf-8") as f:
            f.write(b_content)
            
        print(f"Updated batch file: {os.path.basename(batch_file)}")

print("\nAll Shade -> Vaelin renaming and content updates completed!")
