import os
import json
from PySide6.QtWidgets import QDialog, QInputDialog, QMessageBox
from PySide6.QtCore import Qt, QRect, QPoint
from PySide6.QtGui import QPainter, QColor, QPen, QGuiApplication

class CaptureWizardDialog(QDialog):
    def __init__(self, app_controller, parent=None):
        super().__init__(parent)
        self.app_controller = app_controller
        
        self.setWindowFlags(Qt.Window | Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool)
        self.setAttribute(Qt.WA_TranslucentBackground)
        
        self.start_pos = QPoint()
        self.end_pos = QPoint()
        self.is_drawing = False
        self.selection_rect = QRect()
        
        self.setup_overlay_geometry()

    def setup_overlay_geometry(self):
        rect = self.app_controller.game_hooks.game_rect
        if rect:
            # rect is (x, y, w, h)
            self.setGeometry(rect[0], rect[1], rect[2], rect[3])
            self.game_w = rect[2]
            self.game_h = rect[3]
        else:
            # Fallback to current screen
            screen = QGuiApplication.primaryScreen().geometry()
            self.setGeometry(screen)
            self.game_w = screen.width()
            self.game_h = screen.height()

    def paintEvent(self, event):
        painter = QPainter(self)
        # Dim background
        painter.fillRect(self.rect(), QColor(0, 0, 0, 100))
        
        # Draw selection box
        if not self.selection_rect.isNull():
            painter.setCompositionMode(QPainter.CompositionMode_Clear)
            painter.fillRect(self.selection_rect, Qt.transparent)
            
            painter.setCompositionMode(QPainter.CompositionMode_SourceOver)
            pen = QPen(QColor(255, 0, 0))
            pen.setWidth(2)
            painter.setPen(pen)
            painter.drawRect(self.selection_rect)
            
            # Draw coords info
            painter.setPen(QColor(255, 255, 255))
            txt = f"{self.selection_rect.width()} x {self.selection_rect.height()}"
            painter.drawText(self.selection_rect.bottomLeft() + QPoint(5, 15), txt)

    def mousePressEvent(self, event):
        if event.button() == Qt.LeftButton:
            self.start_pos = event.pos()
            self.is_drawing = True
            self.selection_rect = QRect(self.start_pos, self.start_pos)
            self.update()
        elif event.button() == Qt.RightButton:
            self.reject() # Cancel on right click

    def mouseMoveEvent(self, event):
        if self.is_drawing:
            self.end_pos = event.pos()
            self.selection_rect = QRect(self.start_pos, self.end_pos).normalized()
            self.update()

    def mouseReleaseEvent(self, event):
        if event.button() == Qt.LeftButton and self.is_drawing:
            self.is_drawing = False
            self.process_capture()

    def process_capture(self):
        if self.selection_rect.width() < 5 or self.selection_rect.height() < 5:
            self.selection_rect = QRect()
            self.update()
            return

        cx = self.selection_rect.center().x()
        cy = self.selection_rect.center().y()
        
        # Temporarily hide overlay so user can see input dialog clearly
        self.hide()
        
        name, ok = QInputDialog.getText(
            self, 
            "Save Coordinate Mapping", 
            "Enter the Object Path / Key Name for this Button:"
        )
        
        if ok and name.strip():
            self.save_mapping(name.strip(), cx, cy)
            self.accept()
        else:
            self.show() # Return to capture mode if cancelled
            self.selection_rect = QRect()
            self.update()

    def save_mapping(self, name, cx, cy):
        active_game = self.app_controller.config.get_active_game()
        if not active_game: return
        
        path = active_game.get("ui_mapping_path", "")
        if not path or not os.path.exists(path):
            from core.paths import get_base_dir
            path = os.path.join(get_base_dir(), "assets", "UIConfig_Custom.json")
            
        data = {"entries": []}
        has_wrapper = True
        if os.path.exists(path):
            try:
                with open(path, "r", encoding="utf-8") as f:
                    content = json.load(f)
                    if isinstance(content, dict) and "entries" in content:
                        data = content
                    else:
                        data["entries"] = content
                        has_wrapper = False
            except:
                pass
                
        # Normalize relative to 1280x720 matching existing mapping specs
        rel_x = (cx / self.game_w) * 1280.0
        rel_y = (cy / self.game_h) * 720.0
        
        new_entry = {
            "Path": name,
            "Type": "Button",
            "Coordinates": {
                "x": round(rel_x, 1),
                "y": round(rel_y, 1)
            }
        }
        
        data["entries"].append(new_entry)
        
        try:
            with open(path, "w", encoding="utf-8") as f:
                if has_wrapper:
                    json.dump(data, f, indent=4)
                else:
                    json.dump(data["entries"], f, indent=4)
            self.app_controller.log_message("SYSTEM", "INFO", f"Saved new UI Mapping: {name}")
        except Exception as e:
            self.app_controller.log_message("SYSTEM", "FAIL", f"Failed to save UI Mapping: {e}")
