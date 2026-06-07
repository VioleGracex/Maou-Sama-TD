import pyautogui
import time
from PIL import ImageGrab

print("Moving mouse to (850, 630)")
pyautogui.moveTo(850, 630, duration=0.5)
print("Clicking...")
pyautogui.click()
time.sleep(1.0)

# Take new screenshot
screenshot = ImageGrab.grab()
artifact_path = r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\9a441518-3530-4154-9d6d-cfcdef45956f\game_screenshot_after_click.png"
screenshot.save(artifact_path)
print(f"New screenshot saved to {artifact_path}")
