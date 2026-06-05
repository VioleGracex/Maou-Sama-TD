import unittest
import base64
import json
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.backends import default_backend

from crypto_utils import decrypt_state, find_element_in_state

class TestEncryptionDecryption(unittest.TestCase):
    def setUp(self):
        # Sample key (32 bytes base64 encoded)
        self.key_base64 = "MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI="  # 32 characters base64 decoded is 32 bytes
        self.key = base64.b64decode(self.key_base64)
        self.test_state = {
            "current_scene": "HomeScene",
            "is_dialogue_active": False,
            "elements": {
                "Canvas/MainMenu/PlayButton": {
                    "type": "Button",
                    "x": 450.0,
                    "y": 200.0,
                    "w": 120.0,
                    "h": 40.0,
                    "text": "Play",
                    "visible": True,
                    "interactable": True
                }
            }
        }
        self.plain_text = json.dumps(self.test_state)

    def encrypt_mock_csharp(self, plain_text):
        """Simulates C# AES-256-CBC encryption (writes IV first, then ciphertext)."""
        import os
        iv = os.urandom(16)
        
        # Pad using PKCS7
        padder = padding.PKCS7(128).padder()
        padded_data = padder.update(plain_text.encode('utf-8')) + padder.finalize()
        
        cipher = Cipher(algorithms.AES(self.key), modes.CBC(iv), backend=default_backend())
        encryptor = cipher.encryptor()
        ciphertext = encryptor.update(padded_data) + encryptor.finalize()
        
        # Combined IV + ciphertext
        combined = iv + ciphertext
        return base64.b64encode(combined).decode('utf-8')

    def test_encryption_compatibility(self):
        encrypted = self.encrypt_mock_csharp(self.plain_text)
        decrypted = decrypt_state(encrypted, self.key_base64)
        
        self.assertIsNotNone(decrypted)
        decrypted_state = json.loads(decrypted)
        self.assertEqual(decrypted_state["current_scene"], "HomeScene")
        self.assertFalse(decrypted_state["is_dialogue_active"])
        self.assertIn("elements", decrypted_state)

    def test_find_element_matching(self):
        # Match by full path
        elem = find_element_in_state("Canvas/MainMenu/PlayButton", self.test_state)
        self.assertIsNotNone(elem)
        self.assertEqual(elem["type"], "Button")

        # Match by GameObject name
        elem_by_name = find_element_in_state("PlayButton", self.test_state)
        self.assertIsNotNone(elem_by_name)
        self.assertEqual(elem_by_name["text"], "Play")

        # Match by text
        elem_by_text = find_element_in_state("Play", self.test_state)
        self.assertIsNotNone(elem_by_text)
        self.assertEqual(elem_by_text["x"], 450.0)

if __name__ == "__main__":
    unittest.main()
