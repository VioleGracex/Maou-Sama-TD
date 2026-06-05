import time
import threading
import pyautogui
from pynput import mouse

pyautogui.FAILSAFE = False

def click_task():
    time.sleep(2)
    print("PyAutoGUI clicking at (500, 500)...")
    pyautogui.click(500, 500)
    print("Clicked!")

l = mouse.Listener(suppress=True)
l.start()

t = threading.Thread(target=click_task)
t.start()
t.join()
l.stop()
