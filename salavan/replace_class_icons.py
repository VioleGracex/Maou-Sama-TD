import os
import glob
import shutil

source_dir = r"C:\Users\Ouikio\Downloads\icons1"
target_dir = r"D:\OuikiDev\Maou-Sama-TD\Assets\_Game\Art\ClassIcons"

print("Starting to copy and replace class icons...")

if not os.path.exists(source_dir):
    print(f"Error: Source directory {source_dir} not found.")
    exit(1)

# Find all pngs in source
pngs = glob.glob(os.path.join(source_dir, "*.png"))

for src_path in pngs:
    filename = os.path.basename(src_path)
    
    # 1. Replace _ITEM with _Class (case insensitive)
    # The user has files like Vanguard_ITEM.png or Enemyflanged_ITEM.png
    name_base = filename.replace("_ITEM.png", "").replace("_Item.png", "").replace("_item.png", "")
    
    # Fix the typo in EnemyRanged if it's there
    if name_base.lower() == "enemyflanged":
        name_base = "EnemyRanged"
        
    new_filename = f"{name_base}_Class.png"
    dest_path = os.path.join(target_dir, new_filename)
    
    print(f"\nProcessing {filename} -> {new_filename}...")
    
    # Check if there is an existing Icon to inherit the .meta file from
    # Existing files might be Name_Icon.png, name_icon.png, etc.
    existing_icon_path = None
    existing_meta_path = None
    
    # Try different casing for the existing icon
    possible_old_names = [
        f"{name_base}_Icon.png",
        f"{name_base.lower()}_icon.png",
        f"{name_base.capitalize()}_Icon.png",
        f"{name_base} _Icon.png" # like "Architect _Icon.png"
    ]
    
    for old_name in possible_old_names:
        old_path = os.path.join(target_dir, old_name)
        if os.path.exists(old_path):
            existing_icon_path = old_path
            existing_meta_path = old_path + ".meta"
            break
            
    # Copy the new image to target
    shutil.copy2(src_path, dest_path)
    print(f"Copied {new_filename}")
    
    if existing_icon_path and os.path.exists(existing_meta_path):
        new_meta_path = dest_path + ".meta"
        print(f"Found existing icon {os.path.basename(existing_icon_path)}. Inheriting its .meta file...")
        shutil.move(existing_meta_path, new_meta_path)
        os.remove(existing_icon_path)
        print(f"Replaced {os.path.basename(existing_icon_path)} and preserved Unity GUID.")
    else:
        print("No exact matching existing _Icon.png found. Added as new asset.")

print("\nDone! Unity will automatically import any new assets and update the renamed meta files.")
