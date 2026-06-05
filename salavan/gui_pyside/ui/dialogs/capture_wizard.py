import os
import io
from PIL import ImageGrab, Image
from PySide6.QtWidgets import QDialog, QLabel, QRubberBand, QMessageBox
from PySide6.QtCore import Qt, QPoint, QSize, QRect
from PySide6.QtGui import QPixmap, QCursor

class CaptureWizard(QDialog):
    def __init__(self, name, app_controller, parent=None):
        super().__init__(parent)
        self.target_name = name
        self.app_controller = app_controller
        self.crop_result = False
        
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Dialog)
        self.setModal(True)
        self.setCursor(Qt.CrossCursor)
        
        self._setup_capture()

    def _setup_capture(self):
        # Retrieve game window coordinates
        rect = self.app_controller.game_hooks.game_rect
        if not rect or rect[2] <= 0 or rect[3] <= 0:
            QMessageBox.warning(self, "Capture", "Game window is not running or active.")
            self.reject()
            return
            
        gx, gy, gw_w, gw_h = rect
        self.setGeometry(gx, gy, 1280, 720) # Resize wizard to fit 1280x720 matching game reference
        
        # Grab screenshot
        try:
            self.screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gy + gw_h)) # Wait, bottom-right should be gx + gw_w, gy + gw_h
        except Exception as e:
            # Fallback grab of the rect area
            self.screenshot = ImageGrab.grab(bbox=(gx, gy, gx + gw_w, gy + gw_h))
            
        self.screenshot = self.screenshot.resize((1280, 720), Image.Resampling.LANCZOS)
        
        # Convert to QPixmap
        byte_arr = io.BytesIO()
        self.screenshot.save(byte_arr, format='PNG')
        self.pixmap = QPixmap()
        self.pixmap.loadFromData(byte_arr.getvalue())
        
        # Display background screenshot
        self.bg_label = QLabel(self)
        self.bg_label.setPixmap(self.pixmap)
        self.bg_label.setGeometry(0, 0, 1280, 720)
        
        # Rubber band selection variables
        self.rubber_band = QRubberBand(QRubberBand.Rectangle, self)
        self.origin = QPoint()

    def mousePressEvent(self, event):
        if event.button() == Qt.LeftButton:
            self.origin = event.position().toPoint()
            self.rubber_band.setGeometry(QRect(self.origin, QSize()))
            self.rubber_band.show()

    def mouseMoveEvent(self, event):
        if not self.origin.isNull():
            self.rubber_band.setGeometry(QRect(self.origin, event.position().toPoint()).normalized())

    def mouseReleaseEvent(self, event):
        if event.button() == Qt.LeftButton:
            self.rubber_band.hide()
            rect = self.rubber_band.geometry()
            
            x1 = max(0, rect.left())
            y1 = max(0, rect.top())
            x2 = min(1280, rect.right())
            y2 = min(720, rect.bottom())
            
            if (x2 - x1) > 5 and (y2 - y1) > 5:
                cropped = self.screenshot.crop((x1, y1, x2, y2))
                
                # Build templates path
                from core.paths import get_base_dir
                active_game = self.app_controller.config.get_active_game()
                game_id = active_game.get("id", "maou_sama_td")
                
                dest_dir = os.path.join(get_base_dir(), "templates", game_id)
                os.makedirs(dest_dir, exist_ok=True)
                
                filename = self.target_name
                if not filename.endswith(".png"):
                    filename += ".png"
                    
                dest_path = os.path.join(dest_dir, filename)
                cropped.save(dest_path)
                
                self.app_controller.log_message("CROP UI", "INFO", f"Pattern '{self.target_name}' mapped successfully to {filename}.")
                self.crop_result = True
                self.accept()
            else:
                self.reject()
