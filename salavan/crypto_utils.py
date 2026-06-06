import base64
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.backends import default_backend

def decrypt_state(encrypted_base64, key_base64):
    """
    Decrypts AES-256-CBC encrypted game state.
    The first 16 bytes of decoded content are the IV.
    """
    if not encrypted_base64 or not key_base64:
        return None
    try:
        key = base64.b64decode(key_base64)
        data = base64.b64decode(encrypted_base64)
        if len(data) < 16:
            return None
        
        iv = data[:16]
        ciphertext = data[16:]
        
        cipher = Cipher(algorithms.AES(key), modes.CBC(iv), backend=default_backend())
        decryptor = cipher.decryptor()
        decrypted_padded = decryptor.update(ciphertext) + decryptor.finalize()
        
        # Remove PKCS7 padding
        unpadder = padding.PKCS7(128).unpadder()
        decrypted = unpadder.update(decrypted_padded) + unpadder.finalize()
        
        return decrypted.decode('utf-8-sig')
    except Exception as e:
        print(f"Decryption error: {e}")
        return None

def find_element_in_state(element_id, state):
    if not state:
        return None
    elements = state.get("elements", {})
    if not elements:
        elements = state.get("buttons", {})
    if not elements:
        return None
    
    # 0. Wildcard matching (if '*' is in element_id)
    if '*' in element_id:
        import fnmatch
        for path, data in elements.items():
            name = path.split('/')[-1]
            if fnmatch.fnmatchcase(path.lower(), element_id.lower()) or fnmatch.fnmatchcase(name.lower(), element_id.lower()):
                return data
                
    clean_id = element_id.lower().replace("_", "").replace("-", "").replace(" ", "").replace(".png", "").strip()
    
    # 1. Match by exact full path
    for path, data in elements.items():
        if path.lower() == element_id.lower():
            return data
            
    # 2. Match by exact GameObject name
    for path, data in elements.items():
        name = path.split('/')[-1]
        if name.lower() == element_id.lower():
            return data
            
    # 3. Match by partial name or path
    for path, data in elements.items():
        name = path.split('/')[-1]
        clean_name = name.lower().replace("_", "").replace("-", "").replace(" ", "").strip()
        if clean_name == clean_id or clean_id in clean_name or clean_name in clean_id:
            return data
            
    # 4. Match by text content
    for path, data in elements.items():
        text_val = data.get("text", "")
        if text_val:
            clean_text = text_val.lower().replace("_", "").replace("-", "").replace(" ", "").strip()
            if clean_text == clean_id or clean_id in clean_text or clean_text in clean_id:
                return data
                
    return None

def merge_map_tiles_into_state(state, directory, automation_key):
    import os
    import json
    try:
        map_path = os.path.join(directory, "map_state.json")
        if os.path.exists(map_path):
            with open(map_path, "r", encoding="utf-8") as f:
                content = f.read().strip()
            if content:
                m_state = None
                if content.startswith("{"):
                    m_state = json.loads(content)
                else:
                    decrypted = decrypt_state(content, automation_key)
                    if decrypted:
                        m_state = json.loads(decrypted)
                
                if m_state and "tiles" in m_state:
                    if "elements" not in state:
                        state["elements"] = {}
                    res_w = state.get("resolution", {}).get("width", 1280)
                    res_h = state.get("resolution", {}).get("height", 720)
                    for tile in m_state["tiles"]:
                        tile_id = tile.get("id")
                        if tile_id:
                            sx = tile.get("screenX", 0.0)
                            sy = tile.get("screenY", 0.0)
                            x = sx * (1280.0 / res_w) if res_w > 0 else sx
                            y = sy * (720.0 / res_h) if res_h > 0 else sy
                            state["elements"][tile_id] = {
                                "id": tile_id,
                                "path": tile_id,
                                "type": "Tile",
                                "x": tile.get("x", x),
                                "y": tile.get("y", y),
                                "fx": tile.get("fx", x),
                                "fy": tile.get("fy", y),
                                "visible": True,
                                "interactable": True,
                                "text": ""
                            }
    except Exception as e:
        print(f"Error merging map tiles: {e}")

def get_system_specs():
    import platform
    import subprocess
    import os
    specs = {
        "os": f"{platform.system()} {platform.release()}",
        "cpu": "Unknown Processor",
        "ram": "Unknown Memory",
        "gpu": "Unknown GPU"
    }
    # CPU info
    try:
        if platform.system() == "Windows":
            import winreg
            key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"HARDWARE\DESCRIPTION\System\CentralProcessor\0")
            cpu_name, _ = winreg.QueryValueEx(key, "ProcessorNameString")
            if cpu_name:
                specs["cpu"] = cpu_name.strip()
        else:
            specs["cpu"] = platform.processor() or "Unknown CPU"
    except Exception:
        specs["cpu"] = platform.processor() or "Unknown CPU"

    # RAM info (using GlobalMemoryStatusEx on Windows)
    try:
        if platform.system() == "Windows":
            import ctypes
            from ctypes import wintypes

            class MEMORYSTATUSEX(ctypes.Structure):
                _fields_ = [
                    ('dwLength', wintypes.DWORD),
                    ('dwMemoryLoad', wintypes.DWORD),
                    ('ullTotalPhys', ctypes.c_uint64),
                    ('ullAvailPhys', ctypes.c_uint64),
                    ('ullTotalPageFile', ctypes.c_uint64),
                    ('ullAvailPageFile', ctypes.c_uint64),
                    ('ullTotalVirtual', ctypes.c_uint64),
                    ('ullAvailVirtual', ctypes.c_uint64),
                    ('ullAvailExtendedVirtual', ctypes.c_uint64),
                ]

            stat = MEMORYSTATUSEX()
            stat.dwLength = ctypes.sizeof(stat)
            if ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(stat)):
                ram_bytes = stat.ullTotalPhys
                specs["ram"] = f"{ram_bytes / (1024**3):.1f} GB"
            else:
                raise RuntimeError("GlobalMemoryStatusEx failed")
        else:
            # Fallback for non-Windows (or if GlobalMemoryStatusEx failed)
            import psutil
            ram_bytes = psutil.virtual_memory().total
            specs["ram"] = f"{ram_bytes / (1024**3):.1f} GB"
    except Exception:
        # Fallback to systeminfo or powershell
        try:
            if platform.system() == "Windows":
                out = subprocess.check_output('powershell -Command "(Get-CimInstance Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum"', shell=True, stderr=subprocess.DEVNULL).decode()
                val = out.strip()
                if val.isdigit():
                    ram_bytes = int(val)
                    specs["ram"] = f"{ram_bytes / (1024**3):.1f} GB"
        except Exception:
            pass

    # GPU info (using registry, fallback to PowerShell)
    try:
        if platform.system() == "Windows":
            import winreg
            gpu_names = []
            try:
                path = r"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
                key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, path)
                for i in range(winreg.QueryInfoKey(key)[0]):
                    subkey_name = winreg.EnumKey(key, i)
                    if subkey_name.isdigit():
                        subkey = winreg.OpenKey(key, subkey_name)
                        try:
                            gpu_name, _ = winreg.QueryValueEx(subkey, "DriverDesc")
                            if gpu_name:
                                gpu_names.append(gpu_name.strip())
                        except Exception:
                            pass
            except Exception:
                pass
            
            if gpu_names:
                specs["gpu"] = ", ".join(set(gpu_names))
            else:
                # PowerShell fallback
                out = subprocess.check_output('powershell -Command "Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name"', shell=True, stderr=subprocess.DEVNULL).decode()
                lines = [line.strip() for line in out.splitlines() if line.strip()]
                if lines:
                    specs["gpu"] = ", ".join(lines)
    except Exception:
        pass
    
    return specs



