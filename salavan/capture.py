import time
import threading
from PIL import ImageGrab, Image
import cv2
import numpy as np

class LiveCaptureThread(threading.Thread):
    def __init__(self, app, fps=10):
        super().__init__(daemon=True)
        self.app = app
        self.fps = fps
        self.running = True
        self.recording_path = None
        self.writer = None
        self.writer_lock = threading.Lock()
        
    def start_recording(self, output_path, codec_str='MJPG', fps=None):
        with self.writer_lock:
            self.recording_path = output_path
            if fps is not None:
                self.fps = fps
            
            c_str = codec_str.upper()
            if c_str in ('IVF', 'VP8', 'VP80'):
                fourcc = cv2.VideoWriter_fourcc(*'VP80')
            elif c_str in ('MP4', 'MP4V'):
                fourcc = cv2.VideoWriter_fourcc(*'mp4v')
            else:
                fourcc = cv2.VideoWriter_fourcc(*'MJPG')
                
            self.writer = cv2.VideoWriter(output_path, fourcc, self.fps, (1280, 720))
            
    def stop_recording(self):
        with self.writer_lock:
            if self.writer:
                self.writer.release()
                self.writer = None
            self.recording_path = None
            
    def run(self):
        frame_duration = 1.0 / self.fps
        placeholder_set = False
        
        while self.running:
            start_time = time.time()
            
            rect = self.app.get_game_rect()
            if rect:
                placeholder_set = False
                gx, gy, gw_w, gw_h = rect
                try:
                    # Capture exact game window region
                    screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
                    frame = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
                    
                    # Ensure frame is exactly 1280x720
                    if frame.shape[1] != 1280 or frame.shape[0] != 720:
                        frame = cv2.resize(frame, (1280, 720))
                    
                    with self.writer_lock:
                        if self.writer:
                            self.writer.write(frame)
                    
                    # Feed preview to the GUI
                    if getattr(self.app, "hud_mode", "ANALYZE") == "ANALYZE":
                        preview_img = screenshot.resize((320, 180), Image.Resampling.BILINEAR)
                        self.app.update_preview_image(preview_img)
                except Exception:
                    pass
            else:
                if not placeholder_set:
                    self.app.reset_preview_placeholder()
                    placeholder_set = True
                
                with self.writer_lock:
                    if self.writer:
                        self.writer.release()
                        self.writer = None
                        self.app.log_message("Recorder", "FAIL", "Recording interrupted: Game window closed.")
            
            elapsed = time.time() - start_time
            sleep_time = max(0.01, frame_duration - elapsed)
            time.sleep(sleep_time)
            
        with self.writer_lock:
            if self.writer:
                self.writer.release()
