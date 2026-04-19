import os
import shutil
import hashlib
import re
from pathlib import Path
import json

# Relative import of logic from assign_chibis
# Or just copy the core functions to be standalone
def generate_deterministic_guid(input_string):
    m = hashlib.md5()
    m.update(input_string.encode('utf-8'))
    return m.hexdigest()

def create_sprite_meta(png_path, guid):
    meta_content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 12
  mipmaps:
    enableMipMap: 0
  textureSettings:
    filterMode: 1
    wrapU: 1
    wrapV: 1
  spriteMode: 1
  spritePixelsToUnits: 100
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
"""
    # Note: Using a simplified meta format for brevity, Unity will auto-fill remainders correctly
    with open(str(png_path) + '.meta', 'w', encoding='utf-8') as f:
        f.write(meta_content)

def assign_chibi_to_asset(asset_path, guid):
    if not os.path.exists(asset_path):
        print(f"Skipping: {asset_path} not found.")
        return
    with open(asset_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Force single-line {fileID: ...} format for stability in batch scripts
    pattern = r'Chibi:\s*\{fileID:\s*\d+,\s*guid:\s*[a-zA-Z0-9]+,\s*type:\s*\d+\}'
    replacement = f"Chibi: {{fileID: 21300000, guid: {guid}, type: 3}}"
    new_content = re.sub(pattern, replacement, content)
    
    # Also handle the multi-line format if it exists
    pattern_multi = r'(Chibi:\s+m_FileID:\s*)\d+(\s+m_PathID:\s*)\w+'
    replacement_multi = f"\\g<1>21300000\\g<2>{guid}"
    new_content = re.sub(pattern_multi, replacement_multi, new_content)

    with open(asset_path, 'w', encoding='utf-8') as f:
        f.write(new_content)

def import_batch(source_folder, tier="03_R"):
    art_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\art\Characters")
    data_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\data\Units\Vassals")
    
    source_path = Path(source_folder)
    if not source_path.exists():
        print(f"Error: Source folder {source_folder} does not exist.")
        return

    # Scan for Chibi_*.png
    files = [f for f in source_path.iterdir() if f.suffix == ".png" and f.name.startswith("Chibi_")]
    
    if not files:
        print("No match found for Chibi_*.png in source folder.")
        return

    print(f"Found {len(files)} sprites to import into tier {tier}...")

    for f in files:
        # Extract character name from Chibi_[Name].png
        name = f.stem.replace("Chibi_", "")
        
        target_art_folder = art_dir / tier / name
        os.makedirs(target_art_folder, exist_ok=True)
        
        target_png = target_art_folder / "Sprite_Chibi.png"
        asset_file = data_dir / tier / f"Char_{name}_UnitData.asset"
        
        # Move
        shutil.copy(str(f), str(target_png))
        print(f"Copied {f.name} -> {target_png}")
        
        # GUID & Meta
        guid = generate_deterministic_guid(name + "_chibi")
        create_sprite_meta(target_png, guid)
        
        # Asset linkage
        assign_chibi_to_asset(str(asset_file), guid)
        print(f"Linked {name} to {asset_file.name}")

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Batch import Chibi sprites from MythicFrame ZIP contents.")
    parser.add_argument("source", help="Path to the folder containing unzipped Chibi_*.png files")
    parser.add_argument("--tier", default="03_R", help="Target rarity tier folder (default: 03_R)")
    args = parser.parse_args()
    
    import_batch(args.source, args.tier)
