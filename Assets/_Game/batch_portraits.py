import os
from PIL import Image
import glob

def process_portraits():
    root_dir = r"D:\OuikiDev\Maou-Sama-TD\Assets\_Game\Art\Characters"
    pattern = os.path.join(root_dir, "**", "Art_*_FullBody.png")
    
    # Excluded names
    excluded = ["ignis", "lilith", "aquila", "shade"]
    
    count = 0
    for fb_path in glob.iglob(pattern, recursive=True):
        basename = os.path.basename(fb_path)
        character_name = basename.replace("Art_", "").replace("_FullBody.png", "")
        
        if any(ex in character_name.lower() for ex in excluded):
            print(f"Skipping original: {character_name}")
            continue
            
        try:
            with Image.open(fb_path) as img:
                img = img.convert("RGBA")
                width, height = img.size
                
                # Find content bounding box
                bbox = img.getbbox()
                if not bbox:
                    continue
                
                min_x, min_y, max_x, max_y = bbox
                subject_w = max_x - min_x
                subject_h = max_y - min_y
                
                # Target: 0.75 ratio (3:4)
                # Crop height = 65% of subject height to get waist-up
                crop_h = int(subject_h * 0.65)
                crop_w = int(crop_h * 0.75)
                
                # Safety clamping
                if crop_w > width:
                    crop_w = width
                    crop_h = int(crop_w / 0.75)
                if crop_h > height:
                    crop_h = height
                    crop_w = int(crop_h * 0.75)
                
                center_x = min_x + (subject_w // 2)
                start_x = max(0, min(center_x - (crop_w // 2), width - crop_w))
                start_y = max(0, min(min_y, height - crop_h)) # Align to top of subject content
                
                cropped = img.crop((start_x, start_y, start_x + crop_w, start_y + crop_h))
                
                target_name = f"Art_{character_name}_WaistUp.png"
                target_path = os.path.join(os.path.dirname(fb_path), target_name)
                
                cropped.save(target_path, "PNG")
                print(f"Generated: {target_name} ({crop_w}x{crop_h})")
                count += 1
                
        except Exception as e:
            print(f"Error processing {fb_path}: {e}")
            
    print(f"DONE. Processed {count} portraits.")

if __name__ == "__main__":
    process_portraits()
