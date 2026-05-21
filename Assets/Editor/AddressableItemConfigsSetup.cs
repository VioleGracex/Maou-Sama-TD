using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using MaouSamaTD.Data;

public class AddressableItemConfigsSetup
{
    [MenuItem("Tools/Antigravity/Setup Addressable Item Configs")]
    public static void SetupItemConfigs()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found. Please initialize Addressables first.");
            return;
        }

        string groupName = "ItemConfigs";
        AddressableAssetGroup group = settings.FindGroup(groupName);

        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            Debug.Log($"Created new Addressables Group: {groupName}");
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemConfigSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemConfigSO config = AssetDatabase.LoadAssetAtPath<ItemConfigSO>(path);
            if (config != null)
            {
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                // Set the addressable name to be the item's unique ID for easy lookup
                entry.address = config.ItemID;
                Debug.Log($"Added {config.name} to Addressables with key: {config.ItemID}");
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
        AssetDatabase.SaveAssets();
        Debug.Log("Addressable Item Configs Setup Complete!");
    }
}
