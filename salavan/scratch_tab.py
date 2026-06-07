import pyautogui
import time
from PIL import ImageGrab

print("Pressing TAB...")
pyautogui.press('tab')
time.sleep(0.5)

# Take screenshot after tab to see focus
screenshot = ImageGrab.grab()
artifact_path = r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\9a441518-3530-4154-9d6d-cfcdef45956f\game_screenshot_after_tab.png"
screenshot.save(artifact_path)
print("Saved screenshot after Tab.")
