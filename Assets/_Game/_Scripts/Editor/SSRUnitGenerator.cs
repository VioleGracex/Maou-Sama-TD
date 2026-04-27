using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.IO;
using MaouSamaTD.Units;

namespace MaouSamaTD.Editor
{
    public static class SSRUnitGenerator
    {
        [MenuItem("Maou-TD/Tasks/Generate New SSR Units")]
        public static void GenerateNewSSRUnits()
        {
            string[] newUnits = new string[] 
            {
                "Zephyria",
                "Kaelia",
                "Vespera",
                "Tina"
            };

            string folderPath = "Assets/_Game/Data/Units/Vassals/05_SSR";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup("UnitData");
            if (group == null)
            {
                Debug.LogError("Could not find Addressables group 'UnitData'. Make sure it exists.");
                return;
            }

            foreach(var unitName in newUnits)
            {
                string assetName = $"Char_{unitName}_UnitData";
                string assetPath = $"{folderPath}/{assetName}.asset";
                
                UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(assetPath);
                if (unit == null)
                {
                    unit = ScriptableObject.CreateInstance<UnitData>();
                    unit.UnitName = unitName;
                    
                    AssetDatabase.CreateAsset(unit, assetPath);
                    EditorUtility.SetDirty(unit);
                    
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var entry = settings.CreateOrMoveEntry(guid, group);
                    entry.SetAddress(unitName);
                    // Add label if not present
                    settings.AddLabel("UnitData");
                    entry.SetLabel("UnitData", true, true, false);
                }
            }

            // Old units to delete
            string[] oldToDelete = new string[] 
            {
                "Nyx_Phantom_Beastkin",
                "Valerius_Crimson_Defector",
                "Victor_Fallen_Paladin",
                "Fenris_Alpha_Of_The_North",
                "Malina_Infernal_Countess",
                "Eidon_Archlich_Supreme"
            };

            foreach(var old in oldToDelete)
            {
                string path = $"{folderPath}/Char_{old}_UnitData.asset";
                if (AssetDatabase.LoadAssetAtPath<UnitData>(path) != null)
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    if (settings != null)
                    {
                        settings.RemoveAssetEntry(guid);
                    }
                    AssetDatabase.DeleteAsset(path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully created new SSR units and safely removed old ones (Nyx, Valerius, etc). Ignis, Aquila, Lilith, Shade were left TOUCHED ONLY by skipping them here.");
        }
    }
}
