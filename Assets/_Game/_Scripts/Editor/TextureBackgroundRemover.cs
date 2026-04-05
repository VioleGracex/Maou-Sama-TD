using UnityEngine;
using UnityEditor;
using System.IO;

namespace MaouSamaTD.EditorUtils
{
    public class TextureBackgroundRemover
    {
        [MenuItem("Assets/Art Utilities/Remove White Background")]
        public static void RemoveWhiteBackground()
        {
            int processedCount = 0;
            foreach (var obj in Selection.objects)
            {
                Texture2D tex = obj as Texture2D;
                if (tex == null) continue;

                string path = AssetDatabase.GetAssetPath(tex);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                Debug.Log($"[BackgroundRemover] Processing {path}...");

                // Ensure readability and uncompressed format for processing
                bool wasReadable = importer.isReadable;
                TextureImporterCompression origComp = importer.textureCompression;
                TextureImporterType origType = importer.textureType;
                
                if (!wasReadable || origComp != TextureImporterCompression.Uncompressed)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                // Load fresh copy to avoid compression artifacts in memory
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D rawTex = new Texture2D(2, 2);
                rawTex.LoadImage(fileData);

                Color32[] pixels = rawTex.GetPixels32();
                int alphaCount = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    // High-threshold white detection (values > 240)
                    if (pixels[i].r > 240 && pixels[i].g > 240 && pixels[i].b > 240)
                    {
                        pixels[i].a = 0;
                        alphaCount++;
                    }
                }

                rawTex.SetPixels32(pixels);
                rawTex.Apply();

                byte[] pngData = rawTex.EncodeToPNG();
                File.WriteAllBytes(path, pngData);

                Object.DestroyImmediate(rawTex);

                // Re-import as Sprite with transparency enabled
                importer.isReadable = wasReadable;
                importer.textureCompression = origComp;
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                
                Debug.Log($"[BackgroundRemover] Success: {path} (Modified {alphaCount} pixels)");
                processedCount++;
            }

            Debug.Log($"[BackgroundRemover] Finished. Processed {processedCount} textures.");
        }

        [MenuItem("Assets/Art Utilities/Remove White Background", true)]
        public static bool ValidateRemoveWhiteBackground()
        {
            return Selection.activeObject is Texture2D;
        }
    }
}
