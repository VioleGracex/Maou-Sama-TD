import os
import shutil

source_root = r"Assets\_Game\docs~\Math_and_Balance\Mythic_Batches\UC\general\Batch_UC_1"
target_dir = r"Assets\_Game\Art\Characters\02_UC"

if not os.path.exists(target_dir):
    os.makedirs(target_dir)

# Mapping of character folders to personal names
# The folders are named like "Viona_Shadow_Stalker"
folders = [f for f in os.listdir(source_root) if os.path.isdir(os.path.join(source_root, f))]

for folder in folders:
    # Part 1: Extract Name
    # Folder is "Viona_Shadow_Stalker"
    name = folder.split('_')[0]
    
    sub_path = os.path.join(source_root, folder)
    files = os.listdir(sub_path)
    
    for filename in files:
        if filename.endswith(".png"):
            # batch_Viona_Shadow_Stalker_CHIBI.png
            # batch_Viona_Shadow_Stalker_FULL_BODY.png
            
            target_name = ""
            if "CHIBI" in filename:
                target_name = f"{name}_Chibi.png"
            elif "FULL_BODY" in filename:
                target_name = f"{name}_FullBody.png"
            else:
                continue
                
            source_file = os.path.join(sub_path, filename)
            target_file = os.path.join(target_dir, target_name)
            
            shutil.copy2(source_file, target_file)
            print(f"Copied {filename} to {target_name}")

print("Art migration complete.")
