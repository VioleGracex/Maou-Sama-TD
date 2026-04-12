using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace MaouSamaTD.Editor
{
    public static class AddressablesEditorTools
    {
        [MenuItem("Maou-TD/Addressables/Simplify Unit Addresses")]
        public static void SimplifyUnitAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AddressablesTools] AddressableAssetSettings not found!");
                return;
            }

            var group = settings.FindGroup("UnitData");
            if (group == null)
            {
                Debug.LogError("[AddressablesTools] Group 'UnitData' not found!");
                return;
            }

            int count = 0;
            foreach (var entry in group.entries)
            {
                string fileName = Path.GetFileNameWithoutExtension(entry.AssetPath);
                string address = fileName;

                // Strip Char_ and _UnitData to get just the character name
                if (address.StartsWith("Char_")) address = address.Substring(5);
                if (address.EndsWith("_UnitData")) address = address.Substring(0, address.Length - 9);

                if (entry.address != address)
                {
                    entry.address = address;
                    count++;
                }
            }

            Debug.Log($"[AddressablesTools] Simplified {count} unit addresses to character names.");
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Maou-TD/Addressables/Simplify All Shop Addresses")]
        public static void SimplifyShopAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var group = settings.FindGroup("Shop");
            if (group == null) return;

            int count = 0;
            foreach (var entry in group.entries)
            {
                string fileName = Path.GetFileNameWithoutExtension(entry.AssetPath);
                if (entry.address != fileName)
                {
                    entry.address = fileName;
                    count++;
                }
            }

            Debug.Log($"[AddressablesTools] Simplified {count} shop addresses.");
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
            AssetDatabase.SaveAssets();
        }
    }
}
