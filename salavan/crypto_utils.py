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

