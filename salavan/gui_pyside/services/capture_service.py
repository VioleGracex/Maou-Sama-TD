import time
import cv2
import numpy as np
from PIL import ImageGrab, Image
from PySide6.QtCore import QThread, Signal, QMutex, QMutexLocker
from PySide6.QtGui import QImage, QGuiApplication

class CaptureService(QThread):
    # Emits the downscaled preview image as a QImage for the UI
    frame_ready = Signal(QImage)
    # Emits any recorder level errors or interruptions
    recorder_error = Signal(str)

    def __init__(self, fps=10, parent=None):
        super().__init__(parent)
        self.fps = fps
        self.running = True
        self.recording_path = None
        self.writer = None
        self.writer_mutex = QMutex()
        self.game_rect = None # Tuple: (x, y, width, height)
        self.game_hwnd = 0
        self.hud_mode = "ANALYZE" # Updated by the controller

    def set_game_window(self, x, y, w, h, hwnd):
        """Update the bounding box and handle of the target game window."""
        self.game_rect = (x, y, w, h)
        self.game_hwnd = hwnd

    def set_hud_mode(self, mode):
        self.hud_mode = mode

    def start_recording(self, output_path, codec_str='MJPG', fps=None):
        with QMutexLocker(self.writer_mutex):
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
        with QMutexLocker(self.writer_mutex):
            if self.writer:
                self.writer.release()
                self.writer = None
            self.recording_path = None

    def stop(self):
        self.running = False
        self.wait()

    def run(self):
        frame_duration = 1.0 / self.fps
        placeholder_set = False
        
        while self.running:
            start_time = time.time()
            
            if self.game_hwnd:
                placeholder_set = False
                try:
                    # Capture exact game window natively using its HWND
                    screen = QGuiApplication.primaryScreen()
                    qpixmap = screen.grabWindow(self.game_hwnd)
                    
                    if not qpixmap.isNull():
                        qimage = qpixmap.toImage().convertToFormat(QImage.Format_RGBA8888)
                        arr = np.array(qimage.constBits()).reshape(qimage.height(), qimage.width(), 4)
                        screenshot = Image.fromarray(arr, 'RGBA').convert('RGB')
                        
                        with QMutexLocker(self.writer_mutex):
                            if self.writer:
                                frame = cv2.cvtColor(np.array(screenshot), cv2.COLOR_RGB2BGR)
                                if frame.shape[1] != 1280 or frame.shape[0] != 720:
                                    frame = cv2.resize(frame, (1280, 720))
                                self.writer.write(frame)
                        
                        # Feed preview to the GUI if in ANALYZE mode
                        if self.hud_mode == "ANALYZE":
                            preview_img = screenshot.resize((320, 180), Image.Resampling.BILINEAR)
                            data = preview_img.convert("RGBA").tobytes("raw", "RGBA")
                            p_qimage = QImage(data, preview_img.width, preview_img.height, QImage.Format_RGBA8888)
                            self.frame_ready.emit(p_qimage)

                except Exception:
                    pass
            else:
                if not placeholder_set:
                    # Emit a null/empty QImage to signal the UI to show the placeholder
                    self.frame_ready.emit(QImage())
                    placeholder_set = True
                
                with QMutexLocker(self.writer_mutex):
                    if self.writer:
                        self.writer.release()
                        self.writer = None
                        self.recorder_error.emit("Recording interrupted: Game window closed.")
            
            elapsed = time.time() - start_time
            sleep_time = max(0.01, frame_duration - elapsed)
            time.sleep(sleep_time)
            
        with QMutexLocker(self.writer_mutex):
            if self.writer:
                self.writer.release()
