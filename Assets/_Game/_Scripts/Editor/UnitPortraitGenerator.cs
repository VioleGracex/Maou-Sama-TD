#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MaouSamaTD.Units;
using System.IO;
using System.Collections.Generic;

namespace MaouSamaTD.Editor
{
    public class UnitPortraitGenerator : EditorWindow
    {
        public enum PortraitType { WaistUp, Avatar }

        [MenuItem("Maou-TD/Tools/Unit Portrait Generator")]
        public static void Open()
        {
            GetWindow<UnitPortraitGenerator>("Portrait Gen");
        }

        private PortraitType _mode = PortraitType.WaistUp;
        private float _cropPercent = 0.5f; // Defaults: 0.5 for WaistUp, 0.25 for Avatar
        private float _aspectRatio = 1.0f; 
        private bool _useSmartCrop = true;

        private void OnGUI()
        {
            GUILayout.Label("Unit Portrait Generation Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _mode = (PortraitType)EditorGUILayout.EnumPopup("Generation Mode", _mode);
            if (EditorGUI.EndChangeCheck())
            {
                // Auto-adjust crop percent based on mode
                _cropPercent = _mode == PortraitType.Avatar ? 0.25f : 0.5f;
            }

            _cropPercent = EditorGUILayout.Slider("Crop Height %", _cropPercent, 0.1f, 1.0f);
            _aspectRatio = EditorGUILayout.Slider("Aspect Ratio (W/H)", _aspectRatio, 0.5f, 2.0f);
            _useSmartCrop = EditorGUILayout.Toggle("Use Smart Crop (Bounds)", _useSmartCrop);

            EditorGUILayout.Space();

            if (GUILayout.Button($"Generate {_mode} for Selected"))
            {
                GenerateForSelection();
            }

            if (GUILayout.Button($"Generate {_mode} for ALL (93+)"))
            {
                if (EditorUtility.DisplayDialog("Bulk Operation", $"Are you sure you want to generate {_mode} portraits for all units?", "Yes", "Cancel"))
                {
                    GenerateAll();
                }
            }
        }

        private void GenerateForSelection()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is UnitData unit) ProcessUnit(unit);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void GenerateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:UnitData");
            int i = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(path);
                if (unit != null)
                {
                    EditorUtility.DisplayProgressBar("Generating Portraits", $"Processing {unit.UnitName}...", (float)i / guids.Length);
                    ProcessUnit(unit);
                }
                i++;
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void ProcessUnit(UnitData unit)
        {
            Sprite fullBody = unit.BaseSkin.FullBodyCutout;
            if (fullBody == null) return;

            string fullBodyPath = AssetDatabase.GetAssetPath(fullBody);
            Texture2D sourceTex = LoadReadableTexture(fullBodyPath);
            if (sourceTex == null) return;

            // Determine Crop Rect
            RectInt cropRect = CalculateCropRect(sourceTex);
            
            // Create New Texture
            Texture2D newTex = new Texture2D(cropRect.width, cropRect.height, TextureFormat.RGBA32, false);
            newTex.SetPixels(sourceTex.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height));
            newTex.Apply();

            // Save Asset
            string dir = Path.GetDirectoryName(fullBodyPath);
            string fileSuffix = _mode == PortraitType.Avatar ? "Avatar" : "WaistUp";
            string newPath = Path.Combine(dir, $"Art_{unit.UnitName}_{fileSuffix}.png");
            
            byte[] bytes = newTex.EncodeToPNG();
            File.WriteAllBytes(newPath, bytes);
            AssetDatabase.ImportAsset(newPath);

            // Configure Sprite Settings
            TextureImporter importer = AssetImporter.GetAtPath(newPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            // Assign to SO
            Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(newPath);
            if (_mode == PortraitType.Avatar)
                unit.BaseSkin.Avatar = newSprite;
            else
                unit.BaseSkin.WaistUp = newSprite;
                
            EditorUtility.SetDirty(unit);
            Debug.Log($"[UnitPortraitGenerator] Generated {_mode}: {newPath}");
        }

        private RectInt CalculateCropRect(Texture2D tex)
        {
            int texWidth = tex.width;
            int texHeight = tex.height;

            if (!_useSmartCrop)
            {
                int cropH = (int)(texHeight * _cropPercent);
                int cropW = (int)(cropH * _aspectRatio);
                cropW = Mathf.Min(cropW, texWidth);
                return new RectInt((texWidth - cropW) / 2, texHeight - cropH, cropW, cropH);
            }

            // Smart Crop: Find non-transparent bounds
            Color32[] pixels = tex.GetPixels32();
            int minX = texWidth, maxX = 0, minY = texHeight, maxY = 0;
            bool found = false;

            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    if (pixels[y * texWidth + x].a > 10)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        found = true;
                    }
                }
            }

            if (!found) return new RectInt(0, 0, texWidth, texHeight);

            int subjectHeight = maxY - minY;
            int subjectWidth = maxX - minX;

            int headTop = FindVisualHeadTop(pixels, texWidth, texHeight, minX, maxX, minY, maxY);
            // Add a small 5% headroom buffer above detected head top
            int visualTop = Mathf.Min(maxY, headTop + (int)(subjectHeight * 0.05f));

            int cropHeight = (int)(subjectHeight * _cropPercent);
            int cropWidth = (int)(cropHeight * _aspectRatio);
            
            // Allow crop to be wider than the character bounds to preserve AR
            int centerX = minX + (subjectWidth / 2);
            int startX = centerX - (cropWidth / 2);
            int startY = visualTop - cropHeight;

            // Clamp but don't shrink W/H unless they exceed original texture
            startX = Mathf.Max(0, startX);
            startY = Mathf.Max(0, startY);
            
            if (startX + cropWidth > texWidth) startX = texWidth - cropWidth;
            if (startY + cropHeight > texHeight) startY = texHeight - cropHeight;
            
            // Final safety clamp to texture size
            startX = Mathf.Clamp(startX, 0, texWidth - 1);
            startY = Mathf.Clamp(startY, 0, texHeight - 1);
            cropWidth = Mathf.Min(cropWidth, texWidth - startX);
            cropHeight = Mathf.Min(cropHeight, texHeight - startY);

            return new RectInt(startX, startY, cropWidth, cropHeight);
        }

        private int FindVisualHeadTop(Color32[] pixels, int texWidth, int texHeight, int minX, int maxX, int minY, int maxY)
        {
            int subjectWidth = maxX - minX;
            // A row is considered "part of the head" if its width is > 15% of the subject width
            float densityThreshold = subjectWidth * 0.15f;

            for (int y = maxY; y > minY; y--)
            {
                int rowWidth = 0;
                for (int x = minX; x <= maxX; x++)
                {
                    if (pixels[y * texWidth + x].a > 20) rowWidth++;
                }

                if (rowWidth >= densityThreshold)
                {
                    // Check a few rows below to ensure consistency (avoid single-line horizontal noise)
                    int solidCount = 0;
                    int checkDepth = Mathf.Min(10, y - minY);
                    for (int sy = y - 1; sy > y - checkDepth; sy--)
                    {
                        int sRowWidth = 0;
                        for (int sx = minX; sx <= maxX; sx++)
                            if (pixels[sy * texWidth + sx].a > 20) sRowWidth++;
                        if (sRowWidth >= densityThreshold) solidCount++;
                    }

                    if (solidCount >= checkDepth / 2) return y;
                }
            }
            return maxY;
        }

        private Texture2D LoadReadableTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif
