import cv2
import numpy as np

# Load screenshot
img = cv2.imread(r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\9a441518-3530-4154-9d6d-cfcdef45956f\game_screenshot.png")
h, w, c = img.shape
print(f"Loaded image size: {w}x{h}")

# Let's crop the center region: x from 700 to 1220, y from 500 to 700
crop = img[500:700, 700:1220]
cv2.imwrite(r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\9a441518-3530-4154-9d6d-cfcdef45956f\crop.png", crop)
print("Saved cropped image of the buttons area.")
