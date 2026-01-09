// 09/01/2026 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEditor;
using UnityEngine;
using TMPro;

public class TMProFontAssetCreator : MonoBehaviour
{
    [MenuItem("Tools/Create TMPro Font Assets")]
    public static void CreateTMProFontAssets()
    {
        // Define the source and target folders
        string sourceFolder = "Assets/_Game/Fonts";
        string targetFolder = "Assets/TMPro_Fonts";

        // Ensure the target folder exists
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "TMPro_Fonts");
        }

        // Get all font files in the source folder
        string[] fontPaths = AssetDatabase.FindAssets("t:Font", new[] { sourceFolder });

        foreach (string fontPath in fontPaths)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(fontPath);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);

            if (font != null)
            {
                // Create TMPro font asset
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);

                // Save the font asset in the target folder
                string fontAssetPath = $"{targetFolder}/{font.name}_TMP.asset";
                AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

                Debug.Log($"Created TMPro Font Asset: {fontAssetPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to load font at path: {assetPath}");
            }
        }

        // Refresh the AssetDatabase
        AssetDatabase.Refresh();
        Debug.Log("TMPro Font Assets creation completed!");
    }
}