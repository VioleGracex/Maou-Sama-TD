import cv2
import numpy as np
import os

chars_to_check = ["Eidon", "Kaldor", "Seraphine", "Karrow", "Vesper"]
root_dir = r"c:\UnityProjects\Maou-Sama-TD\Assets\_Game\Art\Characters"

for char_name in chars_to_check:
    for dirpath, dirnames, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename == f"Art_{char_name}_FullBody.png":
                path = os.path.join(dirpath, filename)
                img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
                if img is not None and img.shape[2] == 4:
                    h, w = img.shape[:2]
                    a = img[:, :, 3]
                    coords = cv2.findNonZero((a > 0).astype(np.uint8))
                    if coords is not None:
                        x, y, bw, bh = cv2.boundingRect(coords)
                        print(f"[{char_name}] Size: {w}x{h}, Alpha bounds: x={x}, y={y}, w={bw}, h={bh}")
                        
                        # Test center strip
                        strip_w = int(w * 0.4)
                        x_start = w // 2 - strip_w // 2
                        x_end = w // 2 + strip_w // 2
                        strip = a[:, x_start:x_end]
                        strip_coords = cv2.findNonZero((strip > 0).astype(np.uint8))
                        if strip_coords is not None:
                            sx, sy, sw, sh = cv2.boundingRect(strip_coords)
                            cx = x_start + sx + sw // 2
                            print(f"  Strip bounds: x={x_start+sx}, y={sy}, w={sw}, h={sh}")
                            print(f"  Estimated cx={cx}, cy={sy+85}")
                        else:
                            print("  Strip has no alpha pixels!")
