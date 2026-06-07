import pyautogui
import time
from PIL import ImageGrab

print("Pressing Space...")
pyautogui.press('space')
time.sleep(1.5)

# Take screenshot
screenshot = ImageGrab.grab()
artifact_path = r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\9a441518-3530-4154-9d6d-cfcdef45956f\game_screenshot_after_space.png"
screenshot.save(artifact_path)
print("Saved screenshot after pressing Space.")
