using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace AiImageGenerator.Editor
{
    public static class AiAssignSprite
    {
        [MenuItem("Tools/Antigravity/Assign Red Button Sprite")]
        public static void AssignSprite()
        {
            string path = "Assets/_Game/Art/Generated/red_button_gacha.png";
            GameObject target = GameObject.Find("Image");
            if (target == null) { Debug.LogError("Could not find Image GameObject"); return; }
            
            Image img = target.GetComponent<Image>();
            if (img == null) { Debug.LogError("Could not find Image component"); return; }
            
            // Force reimport as sprite
            // Force reimport as sprite
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite sprite = assets.OfType<Sprite>().FirstOrDefault();
            
            if (sprite != null)
            {
                img.sprite = sprite;
                EditorUtility.SetDirty(img);
                Debug.Log($"Successfully assigned sprite: {sprite.name} to {target.name}");
            }
            else
            {
                Debug.LogError($"Could not find Sprite sub-asset at {path}. Found {assets.Length} entries.");
            }        }
    }
}