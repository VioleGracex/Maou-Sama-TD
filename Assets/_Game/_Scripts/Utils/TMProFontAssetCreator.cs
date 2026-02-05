#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;

public class TMProFontAssetCreator : EditorWindow
{
    private DefaultAsset sourceFolder;
    private DefaultAsset targetFolder;

    [MenuItem("Tools/Create TMPro Font Assets")]
    public static void ShowWindow()
    {
        GetWindow<TMProFontAssetCreator>("TMPro Font Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("TMPro Font Asset Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Source Folder Selection
        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Source Folder", sourceFolder, typeof(DefaultAsset), false);

        // Target Folder Selection
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Target Folder", targetFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Font Assets"))
        {
            if (sourceFolder == null || targetFolder == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select both Source and Target folders.", "OK");
                return;
            }

            CreateTMProFontAssets();
        }
    }

    private void CreateTMProFontAssets()
    {
        string sourcePath = AssetDatabase.GetAssetPath(sourceFolder);
        string targetPath = AssetDatabase.GetAssetPath(targetFolder);

        // Verify that the selected objects are actually folders
        if (!AssetDatabase.IsValidFolder(sourcePath) || !AssetDatabase.IsValidFolder(targetPath))
        {
            EditorUtility.DisplayDialog("Error", "Selected assets must be folders.", "OK");
            return;
        }

        // Get all font files in the source folder
        string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { sourcePath });

        if (fontGuids.Length == 0)
        {
            Debug.LogWarning("No fonts found in the selected source folder.");
            return;
        }

        foreach (string guid in fontGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);

            if (font != null)
            {
                CreateFontAsset(font, targetPath);
            }
        }

        // Refresh the AssetDatabase
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "TMPro Font Assets creation completed!", "OK");
    }

    private void CreateFontAsset(Font font, string targetFolderPath)
    {
        // 1. Create the TMP_FontAsset
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
        
        // 2. IMPORTANT: Set it to Dynamic so it can generate characters at runtime
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // 3. Construct the path
        string fontAssetPath = $"{targetFolderPath}/{font.name}_TMP.asset";

        // 4. Create the asset
        AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

        // 5. Ensure internal materials/textures are persistent
        // For Dynamic fonts, we need to ensure the default material is saved as a sub-asset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = $"{font.name} Atlas Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        // If there's a texture (though often null/empty for purely dynamic initially), save it too if needed
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = $"{font.name} Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        // Save and Refresh
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Created TMPro Font Asset: {fontAssetPath}");
    }
}
#endif