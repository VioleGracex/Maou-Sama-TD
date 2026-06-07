from PIL import ImageGrab
import pyautogui
import os

w, h = pyautogui.size()
print(f"PyAutoGUI screen size: {w}x{h}")

# Take screenshot
screenshot = ImageGrab.grab()
print(f"ImageGrab screen size: {screenshot.size[0]}x{screenshot.size[1]}")

artifact_path = r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\9a441518-3530-4154-9d6d-cfcdef45956f\game_screenshot.png"
screenshot.save(artifact_path)
print(f"Screenshot saved to {artifact_path}")
