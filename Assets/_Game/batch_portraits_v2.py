import os
from PIL import Image
import glob
import shutil

def process_complex_portraits():
    root_dir = r"D:\OuikiDev\Maou-Sama-TD\Assets\_Game\Art\Characters"
    excluded = ["ignis", "lilith"] # Only skip these two
    
    # We will search for all character folders
    for rarity_dir in os.listdir(root_dir):
        rarity_path = os.path.join(root_dir, rarity_dir)
        if not os.path.isdir(rarity_path):
            continue
            
        for char_dir in os.listdir(rarity_path):
            char_path = os.path.join(rarity_path, char_dir)
            if not os.path.isdir(char_path):
                continue
            
            char_name = char_dir
            if any(ex in char_name.lower() for ex in excluded):
                print(f"Skipping: {char_name}")
                continue
                
            # Preferred source order
            fb_path = os.path.join(char_path, f"Art_{char_name}_FullBody.png")
            splash_path = os.path.join(char_path, f"Art_{char_name}_SplashArt.png")
            
            source_file = None
            if os.path.exists(fb_path):
                source_file = fb_path
            elif os.path.exists(splash_path):
                source_file = splash_path
                # Copy Splash to FullBody as requested
                shutil.copy2(splash_path, fb_path)
                print(f"Copied Splash to FullBody for {char_name}")
            
            if not source_file:
                print(f"No valid source for {char_name}")
                continue
                
            try:
                with Image.open(source_file) as img:
                    img = img.convert("RGBA")
                    width, height = img.size
                    
                    bbox = img.getbbox()
                    if not bbox:
                        # Fallback for empty images or issue
                        continue
                    
                    min_x, min_y, max_x, max_y = bbox
                    subject_w = max_x - min_x
                    subject_h = max_y - min_y
                    
                    # 3:4 logic
                    crop_h = int(subject_h * 0.65)
                    crop_w = int(crop_h * 0.75)
                    
                    if crop_w > width:
                        crop_w = width
                        crop_h = int(crop_w / 0.75)
                    if crop_h > height:
                        crop_h = height
                        crop_w = int(crop_h * 0.75)
                    
                    center_x = min_x + (subject_w // 2)
                    start_x = max(0, min(center_x - (crop_w // 2), width - crop_w))
                    start_y = max(0, min(min_y, height - crop_h))
                    
                    cropped = img.crop((start_x, start_y, start_x + crop_w, start_y + crop_h))
                    
                    target_path = os.path.join(char_path, f"Art_{char_name}_WaistUp.png")
                    cropped.save(target_path, "PNG")
                    print(f"Generated 3:4 WaistUp for {char_name} from {os.path.basename(source_file)}")
                    
            except Exception as e:
                print(f"Error {char_name}: {e}")

if __name__ == "__main__":
    process_complex_portraits()
