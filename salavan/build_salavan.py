import os
import re
import sys
import shutil
import subprocess

def main():
    # 1. Run pyinstaller
    print("Running PyInstaller...")
    result = subprocess.run([sys.executable, "-m", "PyInstaller", "salavan.spec"], check=False)
    if result.returncode != 0:
        print("PyInstaller build failed!")
        sys.exit(result.returncode)
        
    # 2. Parse version from main_window.py
    version = "Unknown"
    main_window_path = os.path.join("gui_pyside", "ui", "main_window.py")
    if os.path.exists(main_window_path):
        with open(main_window_path, "r", encoding="utf-8") as f:
            content = f.read()
            match = re.search(r"SALAVAN-HUD GAME SALAVAN PANEL v(\d+\.\d+\.\d+)", content)
            if match:
                version = match.group(1)
                
    print(f"Detected Salavan version: {version}")
    
    # 3. Create destination directory
    dest_dir = f"D:/OuikiDev/Builds/Salavan_v{version}"
    print(f"Creating build directory: {dest_dir}")
    os.makedirs(dest_dir, exist_ok=True)
    
    # 4. Copy the executable
    src_exe = os.path.join("dist", "salavan.exe")
    dest_exe = os.path.join(dest_dir, "salavan.exe")
    if os.path.exists(src_exe):
        print("Copying executable...")
        shutil.copy2(src_exe, dest_exe)
    else:
        print(f"Error: Executable not found at {src_exe}")
        
    # 5. Copy related data folders
    folders_to_copy = ["scenarios", "lua_api"]
    for folder in folders_to_copy:
        if os.path.exists(folder):
            dest_folder = os.path.join(dest_dir, folder)
            if os.path.exists(dest_folder):
                shutil.rmtree(dest_folder)
            print(f"Copying folder {folder}...")
            shutil.copytree(folder, dest_folder)
            
    # 6. Copy config file
    if os.path.exists("config.json"):
        print("Copying config.json...")
        shutil.copy2("config.json", os.path.join(dest_dir, "config.json"))
        
    print(f"Build complete and deployed to {dest_dir}")

if __name__ == "__main__":
    main()
