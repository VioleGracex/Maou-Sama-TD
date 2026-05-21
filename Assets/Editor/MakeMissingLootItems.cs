using UnityEngine;
using UnityEditor;
using MaouSamaTD.Data;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class MakeMissingLootItems
{
    [MenuItem("Tools/Create Missing Loot Items")]
    public static void Run()
    {
        // 1. Gold Coins
        var gold = ScriptableObject.CreateInstance<ItemConfigSO>();
        gold.ItemID = "gold_coins";
        gold.ItemName = "Gold Coins";
        gold.Description = "The standard currency of the demon realm.";
        gold.BackgroundColor = new Color(0.3f, 0.25f, 0.1f, 0.9f);
        gold.TextColor = new Color(1f, 0.9f, 0.5f, 1f);
        
        string goldIconPath = "Assets/_Game/Art/UI/Icons/Gacha/icon_gold_pile.png";
        gold.ItemIcon = AssetDatabase.LoadAssetAtPath<Sprite>(goldIconPath);
        
        AssetDatabase.CreateAsset(gold, "Assets/_Game/Data/Items/gold_coins.asset");
        
        // 2. Blood Crests
        var blood = ScriptableObject.CreateInstance<ItemConfigSO>();
        blood.ItemID = "blood_crests";
        blood.ItemName = "Blood Crests";
        blood.Description = "Rare crests used for summoning and premium upgrades.";
        blood.BackgroundColor = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        blood.TextColor = new Color(1f, 0.5f, 0.5f, 1f);
        
        string bloodIconPath = "Assets/_Game/Art/UI/Icons/Gacha/icon_blood_crest.png";
        blood.ItemIcon = AssetDatabase.LoadAssetAtPath<Sprite>(bloodIconPath);
        
        AssetDatabase.CreateAsset(blood, "Assets/_Game/Data/Items/blood_crests.asset");
        
        AssetDatabase.SaveAssets();
        
        // Add to Addressables
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            AddressableAssetGroup group = settings.DefaultGroup;
            
            var goldEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID("Assets/_Game/Data/Items/gold_coins.asset"), group);
            goldEntry.SetAddress("gold_coins");
            
            var bloodEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID("Assets/_Game/Data/Items/blood_crests.asset"), group);
            bloodEntry.SetAddress("blood_crests");
            
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, goldEntry, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, bloodEntry, true);
        }
        
        AssetDatabase.SaveAssets();
        
        Debug.Log("Created gold_coins and blood_crests ItemConfigs and added to Addressables!");
    }
}
