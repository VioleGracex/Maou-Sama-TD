using UnityEditor;
using UnityEngine;
public class TexturePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        // Only apply to textures inside the _Game folder to avoid affecting third-party packages
        if (!assetPath.Contains("Assets/_Game/")) return;

        TextureImporter textureImporter = (TextureImporter)assetImporter;

        // Check if this is a new asset (not yet imported as a sprite)
        // or if we want to force all textures to be sprites.
        // Using assetImporter.importSettingsMissing is a good way to target only new imports.
        if (textureImporter.textureType == TextureImporterType.Default)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false; // Usually disabled for UI/2D sprites for sharpness
            
            Debug.Log($"[Automator] Automatically configured texture as Sprite: {assetPath}");
        }
    }
}

