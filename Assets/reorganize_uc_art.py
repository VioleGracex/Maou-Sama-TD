import os
import shutil

art_root = r"Assets\_Game\Art\Characters\02_UC"
names = ["Viona", "Callum", "Ulf", "Tarkus", "Xylia", "Fyr", "Tika", "Elowen", "Korr", "Skyra"]

for name in names:
    char_folder = os.path.join(art_root, name)
    if not os.path.exists(char_folder):
        os.makedirs(char_folder)
        
    # File patterns: {name}_Chibi.png, {name}_FullBody.png
    # Target patterns: Art_{name}_Chibi.png, Art_{name}_FullBody.png
    
    files_to_move = [
        (f"{name}_Chibi.png", f"Art_{name}_Chibi.png"),
        (f"{name}_FullBody.png", f"Art_{name}_FullBody.png")
    ]
    
    for old_name, new_name in files_to_move:
        old_path = os.path.join(art_root, old_name)
        new_path = os.path.join(char_folder, new_name)
        
        if os.path.exists(old_path):
            shutil.move(old_path, new_path)
            print(f"Moved and renamed {old_name} to {name}/{new_name}")

print("Reorganization complete.")
